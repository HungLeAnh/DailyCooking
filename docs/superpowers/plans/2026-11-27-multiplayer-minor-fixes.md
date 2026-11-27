# Multiplayer Minor Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the seven minor findings deferred by the multiplayer-session final review.

**Architecture:** Each fix moves its decision into the existing pure classes (`SeatRegistry`, `SessionStateMachine`, `AdDelegationRules`) or a new small pure helper, tested in EditMode, then wires it into `MultiplayerManager` / `GameManager` / `PlayerStateMachine`. Two-machine behaviour is checked once at the end in Multiplayer Play Mode.

**Tech Stack:** Unity 6000.6, Netcode for GameObjects 2.13, NUnit EditMode (`Assets/Tests/Tests.asmdef`), Unity MCP, MPPM.

**Spec:** `docs/superpowers/specs/2026-11-26-multiplayer-session-design.md` (the review findings M1–M8 below argue from its goals and anti-goals: "a held seat or kept avatar leaking past 60 s", "two avatars for one player", one open ad request, 4-player limit).

## Global Constraints

- Branch `fix/multiplayer-minors` from `feat/gesture-cooking`. Paths are relative to `DailyCooking/Assets/` unless they start with `docs/`.
- `MAX_PLAYER_AMOUNT` = 4; seat hold 60 s; consent timeout 15 s (client); request expiry 120 s.
- New values: server-side answer deadline **20 s** (15 s consent + 5 s slack); pending-approval timeout **30 s**.
- New copy: "You joined from another device" (session end, replaced connection). Existing copy unchanged.
- EditMode baseline **314**, green after every task. Commit trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- NGO RPC parameters must be NGO-serializable (no `string[]`); after every compile read the full error console (ILPP errors don't contain "error CS").

## Review Focus

1. **A client approved but dropping before it finishes connecting** → its pending seat frees (timeout or disconnect), never blocks the room → `SeatRegistryTests.PendingExpiresAfter30s`, `PendingClearedOnDropBeforeConnect` (Task 2).
2. **A client that accepted the ad and is watching a long one** → not timed out at 20 s → `AdDelegationRulesTests.Accepted_NotOverdueAt20s` (Task 4).
3. **Host cancels while waiting, then the late answer arrives** → no reward, no error → `AdDelegationRulesTests.CancelOpen_LateReportRejected` (Task 4).
4. **The player who replaced itself on device B quits on device A's message** → A ends with "You joined from another device", never retries → `SessionStateMachineTests.Replaced_InGame_EndsReplaced` (Task 3).
5. **A rejoin stuck connected without a snapshot** → dropped and retried within the window, blocking panel stays full-screen → `ReconnectStepTests.ListeningWhileReconnecting_DropsStaleConnection` (Task 5).

---

### Task 1: Kept avatars and expired food (M1, M2)

**Files:** Create `Scripts/Player/AvatarRules.cs`; Modify `Scripts/Managers/GameManager.cs` (`ExecutePlayerSpawn`), `Scripts/Managers/GameManager.Session.cs` (`NearestFreeCounter`); Test `Tests/AvatarRulesTests.cs`

**Interfaces — Produces:** `public static class AvatarRules { bool KeepWhenOwnerLeaves(string playerId); bool AcceptsExpiredFood(ClearCounterController counter); }`

- [ ] **Step 1: Failing tests:** `KeepWhenOwnerLeaves_WithId_True`; `KeepWhenOwnerLeaves_EmptyOrNull_False`; `AcceptsExpiredFood_ClearCounter_True` (a `GameObject` with `ClearCounterController`); `AcceptsExpiredFood_CookingToolCounter_False` (with `CookingToolCounterController`, a subclass); `AcceptsExpiredFood_Null_False`. Destroy the test objects in `finally`.
- [ ] **Step 2: Run EditMode** → compile error (AvatarRules missing).
- [ ] **Step 3: Implement**; `ExecutePlayerSpawn` sets `DontDestroyWithOwner = AvatarRules.KeepWhenOwnerLeaves(playerId)`; `NearestFreeCounter` skips counters where `!AvatarRules.AcceptsExpiredFood(counter)`.
- [ ] **Step 4: Run EditMode** → 314 + 5 pass. **Commit** `fix(net): keep only avatars with a player id; expired food never goes under a cooking tool`

### Task 2: Pending approvals count as seats (M7)

**Files:** Modify `Scripts/Network/Session/SeatRegistry.cs`, `Scripts/Network/MultiplayerManager.cs` (approval, connect/disconnect, seat expiry tick); Test `Tests/SeatRegistryTests.cs`

**Interfaces — Produces:** `ApprovalResult Approve(string playerId, ulong clientId, int max, float now)` (replaces `Approve(string, int)`; every approved result records a pending seat for `clientId`); `const float PendingSeconds = 30f`; `TakenSeats` = live + held + pending; `Connected(playerId, clientId)` and `Disconnected(playerId, clientId, now)` clear the pending entry of `clientId`; `void DroppedBeforeConnect(ulong clientId)`; `Expire(now)` also drops pending older than 30 s (not returned as expired players).

- [ ] **Step 1: Update existing tests** to the new signature (`Approve(id, clientId, Max, 0f)`), then add failing: `TwoPendingJoins_SecondRefusedWhenOneSeatLeft` (3 live, approve A as client 7 → Approved, approve B as client 8 → RoomFull); `PendingClearedOnConnect` (approve then Connected → TakenSeats unchanged = live count); `PendingClearedOnDropBeforeConnect` (approve, `DroppedBeforeConnect(7)` → seat free); `PendingExpiresAfter30s` (approve at 0, `Expire(30.1f)` → seat free, returned list empty); `ReclaimHeld_DoesNotDoubleCount` (held P reapproved → TakenSeats stays).
- [ ] **Step 2: Run** → compile errors / failures.
- [ ] **Step 3: Implement**; `MultiplayerManager` passes `clientId` and `Time.unscaledTime` to `Approve`, calls `Seats.DroppedBeforeConnect(clientId)` in the server disconnect callback when the client never reached `OnClientConnectedCallback` (it has no `playerIdByClient` entry); `Expire` already runs every second.
- [ ] **Step 4: Run EditMode** → pass. **Commit** `fix(net): approved-but-not-connected joins hold a seat`

### Task 3: Replaced connection ends instead of retrying (M8)

**Files:** Modify `Scripts/Network/Session/SessionStateMachine.cs`, `Scripts/Network/Session/SessionEndText.cs`, `Scripts/Network/MultiplayerManager.cs`; Test `Tests/SessionStateMachineTests.cs`, `Tests/SessionEndTextTests.cs`

**Interfaces — Produces:** `SessionEndReason.Replaced`; `SessionStateMachine.Kicked(SessionEndReason reason)` (any client state except Idle/Ended/Hosting → `Ended(reason)`); `SessionEndText.Message(Replaced)` = "You joined from another device"; constant `MultiplayerManager.REPLACED_REASON = "Replaced by another device"`.

- [ ] **Step 1: Failing tests:** `Replaced_InGame_EndsReplaced` (InGame, `Kicked(Replaced)` → Ended/Replaced); `Kicked_Reconnecting_EndsWithReason`; `Kicked_Hosting_Ignored`; in `SessionEndTextTests.Messages` add the Replaced line.
- [ ] **Step 2: Run** → compile errors. **Step 3: Implement**; `ReplaceLiveConnection` calls `nm.DisconnectClient(oldClientId, REPLACED_REASON)`; the client disconnect callback maps `DisconnectReason == REPLACED_REASON` → `Session.Kicked(SessionEndReason.Replaced)` (before the room-full/transport branches).
- [ ] **Step 4: Run EditMode** → pass. **Commit** `fix(net): a connection replaced by the same player on another device ends instead of retrying`

### Task 4: Server-side answer deadline and cancellable request (M4, M5)

**Files:** Modify `Scripts/Ads/Delegation/AdDelegationRules.cs`, `Scripts/Managers/GameManager.AdDelegation.cs`; Create `Scripts/Ads/Delegation/AdCandidates.cs`; Test `Tests/AdDelegationRulesTests.cs`, `Tests/AdCandidatesTests.cs`

**Interfaces — Produces:** `AdDelegationRules.AnswerDeadlineSeconds = 20f`; `void MarkAccepted(int requestId, ulong sender)`; `bool TryGetOverdue(float now, out int requestId, out ulong client)` (open, not forced, not accepted, older than 20 s); `void CancelOpen()` (closes the open request, no cooldown, chain ends); `public static class AdCandidates { bool CanWatch(bool hasAd, bool isAway); }` used for both the popup's enabled state and the `hasAd` predicate passed to `CanForce`/`TryForce`; new client RPC `AdAcceptedServerRpc(int requestId)`.

- [ ] **Step 1: Failing tests:** `Overdue_NotAnsweredIn20s` (open at 0 → `TryGetOverdue(20.1f)` true with that id/client; at 19.9 false); `Accepted_NotOverdueAt20s` (MarkAccepted at 5 → `TryGetOverdue(60f)` false); `Forced_NeverOverdue`; `CancelOpen_LateReportRejected` (cancel → Completed report → Rejected, `CanAsk` true, a new `TryOpen` succeeds); `MarkAccepted_WrongSender_Ignored`; `AdCandidatesTests.CanWatch` (true only for hasAd && !away).
- [ ] **Step 2: Run** → compile errors. **Step 3: Implement**:
  - Client: Yes → `AdAcceptedServerRpc(id)` before showing the ad.
  - Server `Update` (`TickAdDelegation`, server branch): `TryGetOverdue` → `HandleAdReport(id, client, AdOutcome.Timeout)` (cooldown + reopen, as a decline).
  - Popup Cancel while waiting → `adRules.CancelOpen()`, clear the open request, alert "Request cancelled".
  - Popup rows and `CanForce`/`TryForce` use `AdCandidates.CanWatch(ClientHasRewarded(c), IsAwayClient(c))`; `IsAwayClient(c)` = `PlayerStateMachine.FindForClient(c) == null`.
- [ ] **Step 4: Run EditMode** → pass. **Commit** `fix(ads): server-side answer deadline, cancellable request, away clients never forced`

### Task 5: Reconnect drops a stale connection and keeps the blocking panel (M6)

**Files:** Create `Scripts/Network/Session/ReconnectStep.cs`; Modify `Scripts/Network/MultiplayerManager.cs` (`BeginReconnect`, `ReconnectLoop`); Test `Tests/ReconnectStepTests.cs`

**Interfaces — Produces:** `public enum ReconnectAction { Wait, StartAttempt, DropStaleConnection }`; `public static class ReconnectStep { ReconnectAction Next(SessionState state, bool listening, bool shutdownInProgress, bool attemptInFlight); }`

- [ ] **Step 1: Failing tests:** `Reconnecting_NotListening_StartsAttempt`; `ListeningWhileReconnecting_DropsStaleConnection` (state Reconnecting, listening true, no attempt in flight → DropStaleConnection); `AttemptInFlight_Waits`; `ShutdownInProgress_Waits`; `Syncing_Waits`.
- [ ] **Step 2: Run** → compile errors. **Step 3: Implement**; the loop calls `ReconnectStep.Next` each tick: `StartAttempt` → `TryRejoin`; `DropStaleConnection` → `NetworkManager.Singleton.Shutdown()`; re-entering `Reconnecting` while the loop runs calls `ReconnectingOverlay.ShowBlocking()` (not the banner).
- [ ] **Step 4: Run EditMode** → pass. **Commit** `fix(net): a rejoin stuck without its snapshot is dropped and retried`

### Task 6: Multiplayer Play Mode checks

**Files:** none (verification); docs line in `docs/dataflow.md` §8 only if behaviour text changed.

- [ ] **Step 1:** MPPM, host `Tools/Profiling/Enter Game (local host, no Relay)` + `Tools/Profiling/Skip tutorial`, two virtual players join `LOCAL`.
- [ ] **Step 2 (M4):** Host asks P2; on P2 stop the consent coroutine (`StopCoroutine` on `GameManager.adConsentRoutine` via reflection) so it never answers → after 20 s the host reopens "Who watches?" with P2 marked declined. Host asks P3 and presses Cancel while waiting → "Request cancelled"; P3 then presses Yes → no reward, no error.
- [ ] **Step 3 (M5):** P3 in cooldown, P2 marked away on the host (`ServerSetAway(true)`) → P2's row disabled "away", no Force button.
- [ ] **Step 4 (M8):** Host `DisconnectClient(p2ClientId, MultiplayerManager.REPLACED_REASON)` → P2 ends straight away with "You joined from another device" in the main menu, never enters Reconnecting.
- [ ] **Step 5 (M1/M2/M6/M7):** covered by EditMode tests (no reliable MPPM trigger); spot-check the console stays clean through steps 2–4. Run EditMode → pass. Record results.
