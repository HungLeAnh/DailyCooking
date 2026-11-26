# Multiplayer Session Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clients never get stranded when the host leaves; client totals can't drift; a dropped player comes back to the same avatar and food within 60 s; the host's ads are watched by a client instead of freezing the room.

**Architecture:** Every decision lives in a pure, EditMode-tested class (`SessionStateMachine`, `SeatRegistry`, `StalenessTracker`, `SnapshotAssembler`, `AdDelegationRules`); thin NetworkBehaviours (`MultiplayerManager`, `SessionHeartbeat`, `AdDelegationService`, `PlayerStateMachine`) feed them events and apply results. Flows are verified in Multiplayer Play Mode (host + 2 virtual players).

**Tech Stack:** Unity 6000.6, Netcode for GameObjects 2.13, Unity Transport, Relay, LevelPlay 9.5.1, NUnit EditMode (`Assets/Tests/Tests.asmdef`), Unity MCP (`run_tests`, `execute_code`, `manage_editor`), MPPM.

**Spec:** `docs/superpowers/specs/2026-11-26-multiplayer-session-design.md`

## Global Constraints

- Paths below are relative to `DailyCooking/Assets/` unless they start with `docs/`.
- Host-authoritative: clients send intents by `[Rpc(SendTo.Server)]`; the server checks the sender (`rpcParams.Receive.SenderClientId`).
- `MAX_PLAYER_AMOUNT` = 4 (live players + held seats).
- Seat hold **60 s**; client retry every **3 s** for **60 s**; staleness **5 s**; snapshot timeout **15 s**; `UnityTransport.m_DisconnectTimeoutMS` **60000** in `Scenes/MainMenuScene.unity`.
- Ads: consent timeout **15 s**; ask cooldown **60 s**; request expiry **2 min**; forced notice **3 s**; interstitial safe-moment wait **60 s**; min gap between interstitials on one phone **90 s**; `ForceWatchWhenAllDecline` default **true**; one open rewarded request per host.
- English UI copy, exactly: "The restaurant has closed", "Connection lost", "Connection lost — your restaurant is saved", "Room full", "Could not join the restaurant", "Reconnecting…", "Away", "Who watches?", "Me — the restaurant pauses", "Force a random player", "Cancel", "Waiting for {name}…", "The host asks you to watch an ad for +{amount}", "Nobody watched — ad starts in 3 s", "{name} watched — +{amount}", "{name} can't watch right now", "{name} didn't finish the ad", "No free rewards left today.", "The host is watching an ad — the restaurant is paused".
- Every network decision logs one line with prefix `[Net]`.
- EditMode suite baseline **239**; stays green after every task. Tests: UnityMCP `run_tests` EditMode.
- MPPM: Window → Multiplayer → Multiplayer Play Mode, host in the main Editor, 2 virtual players; `Tools/Profiling/Enter Game (new host)` starts the host.
- Commit with the trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

1. **A client in a cooking close-up or holding food when the session ends** → lands in the main menu with a normal camera/HUD, no exceptions → MPPM check in Task 2, Step 6.
2. **Chunks of an old snapshot arriving after a reconnect** → ignored, never mixed into the new snapshot → `SnapshotAssemblerTests.NewSnapshotId_DiscardsOldChunks` (Task 1).
3. **A player dropping mid-gesture at a cooking tool** → the tool keeps its food and state (half-cut stays on the board), the away avatar can't be driven by the host → MPPM check in Task 6, Step 7.
4. **The host quitting with an ad request open** → no reward, no stuck "Waiting for…" → `AdDelegationRulesTests.HostLeaves_CancelsOpenRequest` (Task 8).
5. **A total that jumps two levels at once** → the client's level-up popup fires once per level → `RestaurantDataTotalsTests.SetProgress_TwoLevels_FiresLevelUpTwice` (Task 3).

---

### Task 1: Session state machine and snapshot assembler

**Files:**
- Create: `Scripts/Network/Session/SessionStateMachine.cs`, `Scripts/Network/Session/SnapshotAssembler.cs`
- Test: `Tests/SessionStateMachineTests.cs`, `Tests/SnapshotAssemblerTests.cs`

**Interfaces — Produces:**
- `public enum SessionState { Idle, Joining, Syncing, InGame, Reconnecting, Hosting, Ended }`
- `public enum SessionEndReason { None, Quit, HostClosed, ConnectionLost, HostConnectionLost, RoomFull, JoinFailed }`
- `public sealed class SessionStateMachine`: `SessionState State`, `SessionEndReason EndReason`, `event Action<SessionState> OnChanged`; inputs `StartHosting()`, `StartJoining()`, `Connected()`, `SnapshotApplied()`, `HostClosing()` (marks the next loss as `HostClosed`), `TransportLost()`, `Refused(SessionEndReason reason)`, `RejoinSucceeded()`, `RetryWindowOver()`, `SnapshotTimedOut()`, `UserQuit()`, `Reset()`.
- `public sealed class SnapshotAssembler`: `bool Add(int snapshotId, int index, int count, byte[] chunk, out byte[][] complete)`, `void Reset()`.

- [ ] **Step 1: Failing tests.** `SessionStateMachineTests`: `Join_Connect_Snapshot_InGame`; `TransportLost_InGame_Reconnecting`; `HostClosing_ThenLost_EndsHostClosed`; `Reconnecting_Rejoin_Syncing` (then `SnapshotApplied` → InGame); `Reconnecting_WindowOver_EndsConnectionLost`; `Joining_Refused_EndsWithReason` (RoomFull); `Syncing_SnapshotTimeout_EndsJoinFailed`; `Hosting_TransportLost_EndsHostConnectionLost`; `UserQuit_AnyState_EndsQuit`; `TransportLost_WhileJoining_EndsJoinFailed`; `Ended_IgnoresFurtherInputs`; `OnChanged_FiresOncePerTransition`. `SnapshotAssemblerTests`: `SingleChunk_Completes`; `OutOfOrder_CompletesWhenAllArrive`; `NewSnapshotId_DiscardsOldChunks` (2 chunks of id 1, then 1 of id 2 with count 1 → completes with id 2's bytes only; a later id-1 chunk → not complete); `BadIndex_Ignored`; `Reset_DropsPartial`.
- [ ] **Step 2: Run EditMode** → compile errors (types missing).
- [ ] **Step 3: Implement** both classes (no Unity types). `SnapshotAssembler` holds the logic now inside `MultiplayerManager.ReceiveGameDataChunkClientRpc` (`MultiplayerManager.cs:130-148`).
- [ ] **Step 4: Run EditMode** → all pass (239 + new).
- [ ] **Step 5: Commit** `feat(net): session state machine and snapshot assembler`

### Task 2: Single exit, host closing, host connection lost

**Files:**
- Modify: `Scripts/Network/MultiplayerManager.cs`, `Scripts/UI/UIPopup/Popups/OptionsPopup/UISettingsPopup.cs` (quit path), `Scripts/Managers/GameManager.Save.cs` (no change expected; `SaveGameImmediate` reused)
- Create: `Scripts/Network/Session/SessionEndText.cs`
- Test: `Tests/SessionEndTextTests.cs`

**Interfaces:**
- Consumes: Task 1 types.
- Produces: `MultiplayerManager.Session` (`SessionStateMachine`), `MultiplayerManager.LeaveSession()` (user quit: host sends closing first), `MultiplayerManager.CurrentJoinCode` (`string`), `[Rpc(SendTo.NotServer)] SessionEndingRpc()`; `public static class SessionEndText { string Message(SessionEndReason r); }` (null for `Quit`/`None`).

- [ ] **Step 1: Failing test** `SessionEndTextTests.Messages`: HostClosed → "The restaurant has closed"; ConnectionLost → "Connection lost"; HostConnectionLost → "Connection lost — your restaurant is saved"; RoomFull → "Room full"; JoinFailed → "Could not join the restaurant"; Quit → null.
- [ ] **Step 2: Run** → compile error. **Step 3: Implement** `SessionEndText`. **Step 4: Run** → pass.
- [ ] **Step 5: Wire `MultiplayerManager`:**
  - Feed the machine: `StartHostSessionAsync` → `StartHosting`; `StartClientSession` → `StartJoining`; own client connected → `Connected`; snapshot loaded (`LoadGameDataFromHost` success) → `SnapshotApplied`; `NetworkManager.OnClientStopped` / `OnTransportFailure` / own-id disconnect → `TransportLost` unless the machine is already `Ended`; a non-empty `NetworkManager.DisconnectReason` equal to "Room full" → `Refused(RoomFull)`; a 15 s timer from `Connected` without `SnapshotApplied` → `SnapshotTimedOut`.
  - Chunk receive uses `SnapshotAssembler`.
  - `OnChanged(Ended)` runs the single exit once: if host and reason is `HostConnectionLost` → `GameManager.Instance.SaveGameImmediate()`; `ShutdownAndReset()`; `Loader.Load(Loader.Scene.MainMenuScene)`; `GameManager.Instance.SwitchState(new MainMenuState(GameManager.Instance))`; then if `SessionEndText.Message` is non-null show `UIPopupType.UIGameNotiPopup` with `Title = "Session ended"` and that message.
  - `LeaveSession()`: host → `SessionEndingRpc()`, wait 0.2 s (coroutine), then `UserQuit`; client → `UserQuit`. Client `SessionEndingRpc` → `HostClosing()`.
  - `UISettingsPopup` quit button calls `MultiplayerManager.Instance.LeaveSession()` instead of its inline shutdown/load.
  - Store `CurrentJoinCode` on host start and client join; clear it on `Ended`.
  - `[Net]` logs for every transition with the reason.
- [ ] **Step 6: MPPM verify:** (a) host Quit → both clients show "The restaurant has closed", main menu; (b) close a virtual player's window/stop its play mode → host keeps running, that player is gone after the transport timeout (30 s until Task 5 raises it); (c) host: `NetworkManager.Singleton.NetworkConfig.NetworkTransport.Shutdown()` via `execute_code` → host saves, main menu, "Connection lost — your restaurant is saved"; (d) a client in a cooking close-up holding food when the host quits → main menu, walk camera/HUD normal, console clean (Review Focus 1).
- [ ] **Step 7: Run EditMode** → pass. **Commit** `feat(net): one exit for every session end, host closing and host connection lost`

### Task 3: Totals instead of deltas

**Files:**
- Modify: `Scripts/Managers/RestaurantData.cs`, `Scripts/UI/UIPopup/Popups/InventoryPopup/InventoryData.cs`, `Scripts/Managers/PostBoxData.cs`, `Scripts/Managers/GameManager.Economy.cs`, `Scripts/Managers/GameManager.Data.cs`, the daily-free count holder (`GameData.ShopData`), `PlayerStats` (upgrade values)
- Test: `Tests/RestaurantDataTotalsTests.cs`, `Tests/InventoryDataTests.cs` (add cases)

**Interfaces — Produces:** `RestaurantData.SetCoins(int total)`, `SetGems(int total)`, `SetProgress(int level, int exp)` (fires `OnLevelUp(level)` once for each level above the current one, then `OnLevelChange`/`OnExpChange`); `InventoryData.SetCount(string id, int count)` (0 removes the stack); `PostBoxData.SetPackages(IList<string> guids)`; `ShopData` set-count for a reward type; `PlayerStats` set-value per `UpgradeTarget`.

- [ ] **Step 1: Failing tests:** `SetCoins_SetsAndNotifies`; `SetGems_SetsAndNotifies`; `SetProgress_SameLevel_NoLevelUp`; `SetProgress_OneLevel_FiresLevelUpOnce`; `SetProgress_TwoLevels_FiresLevelUpTwice` (levels 2 and 3, in order); `SetProgress_Lower_NoLevelUp`; `SetCount_Sets`; `SetCount_Zero_RemovesStack`; `SetCount_Twice_Idempotent`.
- [ ] **Step 2: Run** → compile errors. **Step 3: Implement** the setters.
- [ ] **Step 4: Switch the mirror RPCs** to totals: `UpdateRestaurantCoinClientRpc(int total)`, `UpdateRestaurantGemsClientRpc(int total)`, `UpdateRestaurantExpClientRpc(int level, int exp)`; inventory add/remove → `SetInventoryCountClientRpc(string guid, int count)`; post-box remove → `SetPostBoxPackagesClientRpc(string[] guids)`; daily-free increase → set count; upgrade stat → set the resulting value. Menu add/remove, dish unlock, cosmetic unlock and upgrade purchase stay (already set operations).
- [ ] **Step 5: Run EditMode** → pass. **MPPM:** one busy minute (serve, buy, place, level up) → coins/gems/level/exp/inventory equal on host and both clients (`execute_code` on each).
- [ ] **Step 6: Commit** `fix(net): clients get totals, not deltas`

### Task 4: Seat registry and staleness tracker

**Files:**
- Create: `Scripts/Network/Session/SeatRegistry.cs`, `Scripts/Network/Session/StalenessTracker.cs`
- Test: `Tests/SeatRegistryTests.cs`, `Tests/StalenessTrackerTests.cs`

**Interfaces — Produces:**
- `public sealed class SeatRegistry` (time passed in, seconds): `const float HoldSeconds = 60f`; `void Connected(string playerId, ulong clientId)`; `void Disconnected(string playerId, float now)`; `bool IsHeld(string playerId)`; `int TakenSeats` (live + held); `ApprovalResult Approve(string playerId, int max)` → `enum ApprovalResult { Approved, ApprovedReplaceLive, ApprovedReclaimHeld, RoomFull }`; `IReadOnlyList<string> Expire(float now)` (returns player ids whose hold ended); `ulong? LiveClient(string playerId)`.
- `public sealed class StalenessTracker`: `const float StaleSeconds = 5f`; `void Heard(float now)`; `bool IsStale(float now)`; `void Reset(float now)`.

- [ ] **Step 1: Failing tests:** `NewPlayer_FreeSeat_Approved`; `Full_NewPlayer_RoomFull` (4 live); `HeldSeat_CountsAsTaken` (3 live + 1 held → new player RoomFull); `KnownLive_ApprovedReplaceLive` (same id, live connection); `KnownHeld_ApprovedReclaimHeld` (even when 4 taken); `Expire_After60s_ReleasesSeat`; `Expire_Before60s_Keeps`; `Reconnect_ClearsHold`; `Staleness_After5s_Stale`; `Staleness_HeardResets`.
- [ ] **Step 2: Run** → compile errors. **Step 3: Implement.** **Step 4: Run** → pass.
- [ ] **Step 5: Commit** `feat(net): seat registry and staleness tracker`

### Task 5: Approval by player id, replace live connection, heartbeat

**Files:**
- Modify: `Scripts/Network/MultiplayerManager.cs`, `Scripts/Network/SessionManager.cs` (only if the payload needs the id from it)
- Create: `Scripts/Network/Session/SessionHeartbeat.cs` (NetworkBehaviour on the `MultiplayerManager` GameObject in `Scenes/MainMenuScene.unity`)

**Interfaces:**
- Consumes: `SeatRegistry`, `StalenessTracker`.
- Produces: `MultiplayerManager.Seats` (`SeatRegistry`, server); `string MultiplayerManager.PlayerIdFor(ulong clientId)` (server, from the approval payload); `event Action<string> OnSeatExpired` (server); `MultiplayerManager.OnPlayerAway` / `OnPlayerBack` (`event Action<string playerId>`, server); `SessionHeartbeat.IsServerStale` (client), `SessionHeartbeat.IsClientQuiet(ulong clientId)` (server).

- [ ] **Step 1: Payload:** the client sets `NetworkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(playerId)` before `StartClientWithRelay`; `ConnectionApprovalCallback` reads it (≤128 chars; empty → treat as new player) and calls `Seats.Approve(playerId, MAX_PLAYER_AMOUNT)`: `RoomFull` → refuse with `Reason = "Room full"`; `ApprovedReplaceLive` → approve and `DisconnectClient(oldClientId)` the old connection (log `[Net] replaced live connection`); otherwise approve.
- [ ] **Step 2: Seat bookkeeping:** server connected → `Seats.Connected` (id from the approval payload, kept in a pending map by client id); server disconnect → `Seats.Disconnected(playerId, Time.unscaledTime)` + `OnPlayerAway(playerId)`; each second `Seats.Expire(now)` → for each id raise a new `OnSeatExpired(playerId)` event. `SetplayerIdServerRpc` keeps its duplicate guard but no longer blocks a reclaimed id (the old entry is gone).
- [ ] **Step 3: Heartbeat:** server sends `[Rpc(SendTo.NotServer, Delivery = RpcDelivery.Unreliable)] BeatClientRpc()` every 1 s; client sends `[Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)] BeatServerRpc()` every 1 s; trackers per side. Client: stale → show the "Reconnecting…" overlay (a `UIGameNotiPopup`-style panel on the persistent canvas, non-blocking), hide when heard again. Server: quiet client → `OnPlayerAway` visual only (Task 6 label), back → `OnPlayerBack`.
- [ ] **Step 4: Transport:** set `m_DisconnectTimeoutMS: 60000` in `Scenes/MainMenuScene.unity` via the Inspector/serialized object (verify the YAML line).
- [ ] **Step 5: Run EditMode** → pass. **MPPM:** 4 players (host + 3) → a 5th is refused "Room full"; a client re-joins with the same id while its old connection is live → old one dropped, one `PlayerData` entry, `[Net] replaced live connection`; freeze the host (Editor pause in the main window) for 8 s → both clients show "Reconnecting…" after ~5 s and hide it within ~1 s of the host resuming; stop a virtual player's frames for 8 s → the host logs `[Net] client quiet` and its avatar shows "Away" once Task 6 is in.
- [ ] **Step 6: Commit** `feat(net): approval by player id, live-connection replace, heartbeat and 60 s transport timeout`

### Task 6: Kept avatar, hand-back and expiry

**Files:**
- Modify: `Scripts/Player/PlayerStateMachine.cs`, `Scripts/Managers/GameManager.cs` (`ExecutePlayerSpawn`), the player prefab (`Prefab/Player/*.prefab` that holds `PlayerStateMachine`: `DontDestroyWithOwner: 1`), `Scripts/Network/MultiplayerManager.cs` (expiry hook)
- Create: `Scripts/Player/AwayLabel.cs` (world-space "Away" label shown while `IsAway`)

**Interfaces:**
- Consumes: `OnPlayerAway`, `OnPlayerBack`, `OnSeatExpired`, `Seats`.
- Produces: `PlayerStateMachine.AvatarPlayerId` (`string`, from `NetworkVariable<FixedString64Bytes>`), `PlayerStateMachine.IsAway` (`bool`, `NetworkVariable<bool>`), `PlayerStateMachine.IsLocalAvatar` (`IsOwner && !IsAway`), `static PlayerStateMachine FindByPlayerId(string playerId)`; `FindForClient(ulong)` returns the avatar whose `OwnerClientId == clientId && !IsAway`.

- [ ] **Step 1: Identity:** `ExecutePlayerSpawn(clientId)` sets `avatarPlayerId` from `MultiplayerManager` (the approval payload id) before/at spawn; `GetOwnerPlayerId()` returns `AvatarPlayerId` when set.
- [ ] **Step 2: Gates:** every owner-driven path uses `IsLocalAvatar` instead of `IsOwner`: `Update` (line 127), `PressInteract` (178), the drag/gesture senders (506, 514), `LocalInstance` assignment (382) plus new `OnGainedOwnership`/`OnLostOwnership` overrides that set/clear `LocalInstance` only for `IsLocalAvatar`. Server: `InteractServerRpc`/`ReportGestureServerRpc` refuse when the avatar `IsAway`; an away avatar's held food cannot be taken (the take path checks the holder's `IsAway`).
- [ ] **Step 3: Away:** `OnPlayerAway(id)` → avatar `IsAway = true` (ownership already returned to the server by `DontDestroyWithOwner`); `OnPlayerBack` (heartbeat) → `IsAway = false` only while its owner is still connected.
- [ ] **Step 4: Hand-back:** `ExecutePlayerSpawn(clientId)` with an id whose avatar exists (`FindByPlayerId`) → `ChangeOwnership(clientId)`, `IsAway = false`, no new avatar; the duplicate-spawn guard checks `FindByPlayerId` instead of `client.PlayerObject`.
- [ ] **Step 5: Expiry:** `OnSeatExpired(id)` → held food goes to the nearest counter slot that accepts it (reuse the counter's own accept check; search placed counters by distance), else `DestroySelf`; avatar `Despawn(true)`; log `[Net] seat expired`.
- [ ] **Step 6: Run EditMode** → pass.
- [ ] **Step 7: MPPM:** client holding food drops (stop its play mode) → host sees "Away" avatar holding the food, host joystick/USE don't move or use it, nobody can take its food; client rejoins within 60 s → same avatar, same food, own stats/customization; drop for > 60 s → food on the nearest counter (or destroyed), avatar gone, seat free; a client dropped mid-gesture at a cutting board → the board keeps the half-cut food (Review Focus 3).
- [ ] **Step 8: Commit** `feat(net): keep the avatar and its food for 60 s and hand it back on rejoin`

### Task 7: Client reconnect loop

**Files:**
- Modify: `Scripts/Network/MultiplayerManager.cs`, `Scripts/Managers/GameManager.Save.cs` (`ClearActiveSave` reused)

**Interfaces:** Consumes `SessionStateMachine` (`Reconnecting`, `RejoinSucceeded`, `RetryWindowOver`), `CurrentJoinCode`, `SnapshotAssembler.Reset`.

- [ ] **Step 1: Implement:** on `Reconnecting`: show the "Reconnecting…" overlay; `ShutdownAndReset()` without leaving the state; `GameManager.Instance.ClearActiveSave()`; `SnapshotAssembler.Reset()`; show `UILoadingPopup`; every 3 s call `StartClientSession(CurrentJoinCode)` until connected (→ `RejoinSucceeded`, normal snapshot + spawn = kept avatar) or 60 s since entering `Reconnecting` (→ `RetryWindowOver` → `Ended(ConnectionLost)`). A refusal "Room full" during retries ends with `RoomFull`.
- [ ] **Step 2: Run EditMode** → pass.
- [ ] **Step 3: MPPM:** cut a client (host `DisconnectClient(id)` via `execute_code`) → client shows Reconnecting, loading, back in the restaurant on its avatar within a few seconds; host refuses rejoin for 60 s (temporarily block approval for that id) → client ends with "Connection lost" at ~60 s; host quits during the client's retries → client ends with "Connection lost" at 60 s (old code can't succeed).
- [ ] **Step 4: Commit** `feat(net): clients reconnect for 60 s through the normal join path`

### Task 8: Ad delegation rules

**Files:**
- Create: `Scripts/Ads/Delegation/AdDelegationRules.cs`, `Scripts/Ads/Delegation/AdDelegationConfigSO.cs` (`bool ForceWatchWhenAllDecline = true`; asset `SO/Ads/AdDelegationConfig.asset`)
- Test: `Tests/AdDelegationRulesTests.cs`

**Interfaces — Produces:**
- `public enum AdOutcome { Completed, Declined, NoAd, Timeout, FailedOrClosedEarly, Left }`
- `public sealed class AdDelegationRules` (time in seconds, client ids as `ulong`): constants `ConsentSeconds = 15f`, `AskCooldownSeconds = 60f`, `RequestExpirySeconds = 120f`, `InterstitialGapSeconds = 90f`;
  - `bool TryOpen(ulong client, int rewardType, float now, out int requestId)` (fails if a request is open or the client is in cooldown);
  - `bool CanAsk(ulong client, float now)`;
  - `AdResult Report(int requestId, ulong sender, AdOutcome outcome, float now)` → `enum AdResult { Grant, Reopen, Ended, Rejected }` (wrong sender/expired/used → `Rejected`; Declined/NoAd/Timeout → cooldown + `Reopen`; FailedOrClosedEarly → `Reopen`; Left → `Reopen`; forced request non-Completed → `Ended`);
  - `bool CanForce(IReadOnlyCollection<ulong> clients, Func<ulong,bool> hasAd, float now, bool forceEnabled)` (true only when no client can be asked and some client has an ad);
  - `bool TryForce(IReadOnlyCollection<ulong> clients, Func<ulong,bool> hasAd, int rewardType, Random rng, out ulong client, out int requestId)` (ignores cooldown, once per request chain);
  - `void CancelAll()` (host leaving);
  - interstitial: `ulong? NextInterstitialTarget(IReadOnlyList<ulong> clients, ulong? lastPicked, Func<ulong,float> lastInterstitialAt, float now)` (rotation starting after `lastPicked`, skips clients with < 90 s since their last interstitial).
- [ ] **Step 1: Failing tests:** `Open_OneAtATime`; `Completed_Grants_Once` (second report → Rejected); `WrongSender_Rejected`; `Expired_Rejected` (121 s); `Declined_Cooldown60s_ThenAskable`; `NoAd_Cooldown_Reopen`; `Failed_Reopen_NoCooldown`; `AllInCooldown_CanForce`; `CanForce_FalseWhenNoAdAnywhere`; `CanForce_FalseWhenSwitchOff`; `Force_PicksOnlyClientsWithAd`; `Forced_Failed_Ends_NoSecondForce`; `HostLeaves_CancelsOpenRequest` (after `CancelAll`, a Completed report → Rejected); `Interstitial_RotatesAfterLastPicked`; `Interstitial_SkipsUnder90s`; `Interstitial_NoneEligible_Null`.
- [ ] **Step 2: Run** → compile errors. **Step 3: Implement** (pure, no Unity types except none; `System.Random` passed in). Create the config asset.
- [ ] **Step 4: Run EditMode** → pass. **Step 5: Commit** `feat(ads): ad delegation rules`

### Task 9: Ad provider seam

**Files:**
- Create: `Scripts/Ads/Delegation/IAdProvider.cs`, `Scripts/Ads/Delegation/LevelPlayAdProvider.cs`, `Scripts/Ads/Delegation/FakeAdProvider.cs`
- Modify: `Scripts/Ads/AdsManager.cs` (raise completion/failure/close outcomes for rewarded and interstitial)
- Test: `Tests/FakeAdProviderTests.cs`

**Interfaces — Produces:** `public interface IAdProvider { bool HasRewarded { get; } bool HasInterstitial { get; } void ShowRewarded(string placement, Action<AdOutcome> done); void ShowInterstitial(string placement, Action<AdOutcome> done); }`; `AdProviders.Current` (`IAdProvider`: `LevelPlayAdProvider` in players; `FakeAdProvider` in the Editor or when `AdsManager` has no ad unit ids); `FakeAdProvider` settable `NextOutcome`, `DelaySeconds` (default 3), `HasAd`.
- [ ] **Step 1: Failing tests:** `Fake_CompletesAfterDelay` (drive with a manual clock `Tick(float)`), `Fake_NoAd_ReportsNoAd`, `Fake_ConfiguredFailure`.
- [ ] **Step 2: Run** → compile errors. **Step 3: Implement**; `LevelPlayAdProvider` maps `OnAdRewarded`+`OnAdClosed` → `Completed`, `OnAdDisplayFailed` → `FailedOrClosedEarly`, closed without reward → `FailedOrClosedEarly`, not loaded → `NoAd`. Existing `ShowRewardedAds`/`ShowInterstitialAds` callers keep working.
- [ ] **Step 4: Run EditMode** → pass. **Step 5: Commit** `feat(ads): ad provider seam with a fake for the Editor`

### Task 10: Ad delegation service, popups and hooks

**Files:**
- Create: `Scripts/Ads/Delegation/AdDelegationService.cs` (server-owned NetworkBehaviour on the `UIManager` GameObject in `Scenes/MainMenuScene.unity`), `Scripts/UI/UIPopup/Popups/UIAdWatcherPopup.cs` + prefab, registered as `UIPopupType.UIAdWatcherPopup` (append to the enum) in `SO/PopupDatabase/PopupDatabase.asset`
- Modify: `Scripts/UI/UIPopup/Popups/ShopPopup/UIDailyFreeItem.cs`, `Scripts/Managers/States/InGameState.cs`, `Scripts/UI/UILevelUpPopup.cs`, `Scripts/Managers/GameManager.Economy.cs` (extract `ServerGrantDailyFree(ShopItemType, ulong notifyClient)` from `ClaimDailyFreeServerRpc`)

**Interfaces:**
- Consumes: Tasks 8–9, `MultiplayerManager` player list, `PlayerStateMachine.IsAway`, `CameraModeController.Instance.Mode`.
- Produces: `AdDelegationService.Instance`; host API `RequestRewardedFor(ShopItemType reward)` (opens `UIAdWatcherPopup` when others are in the room, else returns false so the caller shows the ad itself); `bool HostShouldDelegateInterstitials` (others in the room).

- [ ] **Step 1: Popup:** `UIAdWatcherPopup` titled "Who watches?": one row per client (name; disabled with "declined" / "no ad" / "away" / cooldown seconds), last pick preselected, buttons "Me — the restaurant pauses", "Cancel", and "Force a random player" shown only when `CanForce`. While a request is open the daily-free button text is "Waiting for {name}…".
- [ ] **Step 2: Rewarded flow:** host pick → `AskClientRpc(requestId, reward, amount, forced:false)` to that client → `UIGameConfirmPopup` "The host asks you to watch an ad for +{amount}" (Yes/No, auto-No after 15 s); Yes → `AdProviders.Current.ShowRewarded` → `ReportServerRpc(requestId, outcome)`; server `Report` → `Grant` → `ServerGrantDailyFree` + alert host and watcher "{name} watched — +{amount}"; `Reopen` → host alert ("{name} can't watch right now" / "{name} didn't finish the ad") and reopen the popup; daily limit reached → "No free rewards left today." to both. Force → `TryForce` → `AskClientRpc(..., forced:true)` → client shows "Nobody watched — ad starts in 3 s" then the ad. "Me" → host broadcasts `HostAdPauseClientRpc(true)` ("The host is watching an ad — the restaurant is paused" notice on clients) and shows the ad itself, then `(false)`. Client leaves → `Report(..., Left)`. Session end → `CancelAll`.
- [ ] **Step 3: Hooks:** `UIDailyFreeItem` host path → `RequestRewardedFor` first; clients unchanged. `InGameState` host with `HostShouldDelegateInterstitials` → `AdDelegationService.SendInterstitial()` instead of `ShowInterstitialAds("Break_Time")` (server picks `NextInterstitialTarget`; client shows at a safe moment: no popup open, `CameraModeController.Instance.Mode != CameraMode.CloseUp`, waits ≤ 60 s, else reports and the server tries the next client once; none → skip). Each phone records its last interstitial time. `UILevelUpPopup` on the host with others in the room → no `Level_Up` interstitial.
- [ ] **Step 4: Run EditMode** → pass.
- [ ] **Step 5: MPPM with `FakeAdProvider`:** accept → reward once, both alerts; decline → cooldown shown, host picks the other client; both decline → Force button → a client with an ad gets the 3 s notice and the ad; set `HasAd=false` on all → no Force button, only Me/Cancel; switch off in the config asset → no Force button; host's `Break_Time` (shorten the interval via `execute_code`) → goes to clients in rotation, never twice within 90 s on one phone; host level-up with others → no host `Level_Up` ad; host quits with a request open → client sees "The restaurant has closed", no reward, no error.
- [ ] **Step 6: Commit** `feat(ads): host ads are watched by a client of the host's choice`

### Task 11: Docs

**Files:** Modify `docs/dataflow.md` (§2 session/join: approval payload, seats, heartbeat, reconnect, single exit; new §2b ad delegation; §8 risk: forced rewarded ads, owner-accepted), `docs/setupjoinsession.md` (Leave/Reconnect rows, MPPM drop test), `docs/project_overview.md` (Multiplayer paragraph).

- [ ] **Step 1: Write** the doc changes. **Step 2: Run EditMode** → pass. **Step 3: Commit** `docs: multiplayer session, reconnect and ad delegation`
