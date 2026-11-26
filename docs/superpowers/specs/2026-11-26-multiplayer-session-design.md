# Multiplayer session: host leaving, state totals, reconnect, ad handover — design

Status: draft for approval (2026-11-26). Branch `feat/multiplayer-session` (from `feat/gesture-cooking`).

## Why

DailyCooking is 1–4 player co-op on Unity Relay with the host's phone as the server and the only
save. Three things break real mobile sessions today:

1. When the host quits or drops, clients are stranded in a dead `GameScene`: nothing handles the
   loss (`MultiplayerManager.OnFailedToJoinGame` is raised and has no subscriber; nobody listens to
   `OnClientStopped` / `OnTransportFailure`).
2. A phone that shows a full-screen ad stops running Unity. If the silence outlasts the transport
   timeout (`UnityTransport.m_DisconnectTimeoutMS = 30000`) that player is dropped, with no way back
   except typing the join code again.
3. When the **host** shows an ad the whole restaurant freezes for everyone (customers, cooking, every
   action), and past the timeout every client drops and the session ends. The host already shows a
   timed interstitial (`InGameState`, `"Break_Time"`) and a `"Level_Up"` one.

Also, clients mirror coins/gems/EXP through "add X" RPCs (`UpdateRestaurantCoinClientRpc(int delta)`,
etc.); it is correct today but nothing corrects a drift if a change is ever lost or doubled.

What success looks like: a player who steps into an ad or loses signal briefly comes back to the
same avatar holding the same food; clients see a clear message when the host leaves; the host's ads
never freeze the room unless every other player refuses.

## Goals

- G1 **Session end:** clients leave a session through one path with a reason popup ("The restaurant
  has closed", "Connection lost", "Room full", …) and land in the main menu.
- G2 **Totals:** clients receive coin/gem/EXP **totals**, not deltas.
- G3 **Reconnect:** a dropped client retries for **60 s**; the host **holds its seat** and keeps its
  **avatar in the restaurant, still holding its food**, and gives that avatar back on rejoin.
- G4 **Ad handover:** with other players in the room, the host's ads are watched by a client:
  - Rewarded: the host **picks a client each time**; the client must **Accept**; refusals mark that
    client and the host picks again; if **every** client refuses or has no ad, the host gets a
    **[Force a random player]** button, and pressing it makes the server force a random client that
    has an ad loaded to watch (switchable, see below).
  - Interstitials: the host's `Break_Time` **rotates through the clients** (starting from the one the
    host last picked), on top of each client's own `Break_Time`; the host's `Level_Up` interstitial
    is skipped (every client already shows its own).
- G5 **Host connection lost:** the host saves immediately and returns to the main menu with
  "Connection lost — your restaurant is saved".

## Non-goals

- Host migration, dedicated servers, distributed authority, cloud save.
- Changing the join snapshot (measured cheap: 449–549 bytes gzipped, 1 chunk, 53–62 ms).
- Lobby / room lists / friend invites.
- Rewards for clients outside the host's save (unchanged: the restaurant owns coins/EXP; clients keep
  their stats/upgrades/cosmetics, stored in the host's save by `playerId`).
- Real ad behaviour on devices (builds are blocked); the Editor uses a fake ad provider.

## Anti-goals

- A client stuck in a dead scene, ever.
- The room pausing for an **interstitial**.
- A held seat or kept avatar leaking past 60 s.
- Two avatars for one player after a rejoin.

## Constraints

- Unity 6000.6, Netcode for GameObjects 2.13 (host-authoritative), Unity Transport, Relay (DTLS),
  Multiplayer Services 2.3.1, LevelPlay 9.5.1, Android target.
- Existing rules: clients send intents by ServerRpc; the server checks sender/ownership/range; feedback
  goes to one client through `UIManager` (`RpcTarget.Single`).
- EditMode suite (239) stays green. Testing: EditMode for the pure logic, Multiplayer Play Mode
  (MPPM) for the flows.

## Approaches considered

- **A. Targeted fixes on the current model (chosen).** Session state machine + single exit, totals in
  RPCs, reconnect with held seat and kept avatar, ad handover service. Each piece testable in the
  Editor; no change to Relay, the snapshot or authority.
- B. Move coins/gems/EXP to `NetworkVariable`s on a new network object. Cleaner sync, but touches UI
  and save loading; G2 gets the same safety from totals in the existing RPCs.
- C. Take the server off the host's phone (dedicated server / distributed authority) so anyone's ad
  only pauses them. Solves the ad problem fully, but rewrites authority, adds hosting cost and needs
  cloud save. Out of scope for 4-player co-op now.
- Ad options rejected by the owner: turning host interstitials off, pausing the room, everyone
  watching together, host-only-when-closed.

## Decisions (owner's)

| Topic | Decision |
|---|---|
| Host leaves | Clients get a reason popup and go to the main menu |
| Client totals | Totals instead of deltas |
| Reconnect | 60 s, seat held, avatar kept with its food |
| Host rewarded ad | Host picks a client each time; client must Accept (15 s, no answer = decline) |
| All refuse | Host presses **[Force a random player]**; the server forces a random client with an ad loaded — **owner accepts the ad-policy risk** (rewarded ads are normally opt-in; LevelPlay/AdMob policy to be checked) |
| Re-asking | A client who declined cannot be asked again for 60 s |
| Host interstitial | `Break_Time` → rotates through the clients, in addition to their own; `Level_Up` → skipped on the host |
| Host's own connection fails | Save immediately, main menu, "Connection lost — your restaurant is saved"; the host opens a new room (new code) when it wants |
| Client rewards | Unchanged |

## Builder calls (from the builder check, settled without the owner)

- A known `playerId` connecting while its old connection is still alive: the host disconnects the old
  connection and hands the avatar over at once (one avatar per player). A known `playerId` is never
  refused as "Room full".
- A retry that lands after the 60 s seat hold expired joins as a new player if a seat is free (new
  avatar, the old food already placed/destroyed); otherwise it ends with `RoomFull`.
- A client shows "Reconnecting…" after **5 s without any server traffic** (its own staleness check,
  separate from the 60 s transport timeout); the host shows "Away" over a client's avatar after 5 s
  of silence (visual only). The 60 s seat hold starts when the host's transport reports the
  disconnect. A client in an ad shorter than 60 s therefore never disconnects.
- Reconnect goes through the normal join path: clear `GameData`, loading screen, scene sync,
  snapshot, spawn → kept avatar. Never an overlay over an emptied restaurant.
- A host killed by the OS (no `SessionEndingRpc`): clients show "Reconnecting…" for 60 s, then
  "Connection lost" (the old join code cannot succeed once the host's Relay allocation is gone).
- A handed-over interstitial is shown at least 90 s after that phone's previous interstitial (it waits,
  within its 60 s safe-moment window, else it goes to the next client in the rotation or is skipped).

## Design

### 1. Session state machine and single exit (G1)

Pure class `SessionStateMachine` (EditMode-tested), states:
`Idle → Joining → Syncing → InGame → Reconnecting → Ended(reason)`; host side `Hosting → Ended`.
Inputs: join started, connected, snapshot applied, transport lost, rejoin ok/failed, retry window
over, host closed, user quit, approval refused (`NetworkManager.DisconnectReason`).

`MultiplayerManager` feeds it from `NetworkManager.OnClientStopped`, `OnTransportFailure`,
`OnClientDisconnectCallback` (own id), the snapshot completion and the existing quit path.
`Ended(reason)` runs one exit: `ShutdownAndReset` → `Loader.Load(MainMenuScene)` →
`MainMenuState` → a popup with the reason text. Reasons: `HostClosed`, `ConnectionLost`,
`RoomFull`, `JoinFailed`, `Quit` (no popup).

Graceful host exit: before `Shutdown`, the host sends `SessionEndingRpc(reason)` to clients and
waits ~0.2 s; clients mark the coming loss as `HostClosed` instead of `ConnectionLost`.

Host side: `Hosting` takes `transport failure` / `server stopped` (not caused by its own quit) →
`Ended(HostConnectionLost)`: `SaveGameImmediate`, the single exit, popup "Connection lost — your
restaurant is saved".

A joining client that does not get the snapshot within 15 s ends with `JoinFailed`.

### 2. Totals instead of deltas (G2)

`UpdateRestaurantCoinClientRpc`, `…GemsClientRpc`, `…ExpClientRpc` carry the new **total** (and EXP
the level with it); the client sets the value instead of adding. List-style mirrors (menu,
inventory, post box, unlocks) stay add/remove; they are idempotent already or keyed by GUID
(builder checks each and makes any non-idempotent one idempotent). The level-up popup still fires
once per level reached on clients.

### 3. Reconnect, held seat, kept avatar (G3)

Host:
- Player avatar prefab: `DontDestroyWithOwner = true`; on owner disconnect NGO returns ownership to
  the server instead of despawning.
- `PlayerStateMachine` gets a server-written `NetworkVariable<FixedString64Bytes> avatarPlayerId`
  (set at spawn) and `NetworkVariable<bool> isAway`. **`GetOwnerPlayerId` must read
  `avatarPlayerId`**, otherwise an away avatar (owned by the server) shows the host's stats and
  customization.
- An away avatar is owned by the host's client, so `IsOwner` is true for it on the host: every
  owner-driven path (`PlayerStateMachine.Update` movement, `PressInteract`, camera/local-player
  lookups, gesture reports) must skip it while `isAway`.
- `ChangeOwnership` does not make the avatar the new client's `PlayerObject` (NGO sets that only at
  spawn). Avatar lookups (`PlayerStateMachine.FindForClient`, the duplicate-spawn guard in
  `ExecutePlayerSpawn`) use `avatarPlayerId` / current owner instead of `client.PlayerObject`.
- The joining client's `playerId` travels in the connection payload so approval and seat checks can
  use it (today it only arrives later via `SetplayerIdServerRpc`).
- On a client disconnect: mark its avatar `isAway = true`, record `reservedSeats[playerId] = now +
  60 s`. An away avatar ignores interactions, cannot be pushed, and its held food cannot be taken.
  Show an "Away" label above it.
- `ConnectionApproval` counts live players + reserved seats against `MAX_PLAYER_AMOUNT` (4). A joiner
  whose `playerId` owns a reserved seat is always allowed (the id arrives after approval, so the
  approval payload carries it; see "Left to the builder").
- On spawn request from a client whose `playerId` has an away avatar: `ChangeOwnership(newClientId)`,
  `isAway = false`, drop the seat reservation, no new avatar. The `PlayerData` list entry is
  re-created for the new client id as today.
- When the 60 s pass: the held food goes to the nearest free counter (any `IKitchenObjectParent`
  counter slot that accepts it), else it is destroyed; the avatar despawns; the seat is freed.
- Host leaving ends everything (G1); no seats survive the session.

Client:
- On `transport lost` while `InGame` (not quit, not `HostClosed`): enter `Reconnecting`, show a
  "Reconnecting…" overlay, retry `StartClientWithRelay(joinCode)` every 3 s until 60 s pass, then
  `Ended(ConnectionLost)`. On success, the normal join (snapshot, spawn → gets the kept avatar).
- The client keeps `joinCode` for the session it is in.

Transport: raise `m_DisconnectTimeoutMS` 30000 → 60000 in `MainMenuScene` (both ends tolerate a
longer silent ad). Relay's own idle limit for a suspended phone is unknown and must be measured on a
device; reconnect covers the case where it is shorter.

### 4. Ad handover (G4)

New server-owned `AdDelegationService` (NetworkBehaviour beside `UIManager`); decisions in a pure,
EditMode-tested `AdDelegationRules`. An `IAdProvider` seam wraps `AdsManager` (rewarded/interstitial
show, loaded, completed/failed/closed callbacks); the Editor and tests use `FakeAdProvider`
(configurable delay and outcome).

Rewarded (host taps a daily-free item in `UIDailyFreeItem`):
- Room has no other players → host watches as today.
- Otherwise the host gets a "Who watches?" popup: each client (cooldown and away clients shown
  disabled with the reason), the last pick preselected, plus **[Me — the restaurant pauses]** and
  **[Cancel]**.
- Server creates one request (`requestId`, `rewardType`, chosen client, expiry 2 min). **One open
  request per host.** The host button shows "Waiting for <name>…".
- Client sees "The host asks you to watch an ad for +X" [Accept] [Decline]; 15 s without an answer =
  decline. Accept → the client's own ad provider shows the rewarded ad. The room keeps running.
- Outcomes reported by `AdResultServerRpc(requestId, result)`:
  - `Completed` → server checks the request (exists, sender is the chosen client, not expired, not
    used) and grants through the same code as `ClaimDailyFreeServerRpc` (daily count, reward to the
    restaurant); host and watcher see "<name> watched — +X".
  - `Declined` / `NoAd` / `Timeout` → that client is marked for this request and gets a **60 s ask
    cooldown**; the host's popup reopens with them disabled.
  - `FailedOrClosedEarly` → no reward; host chooses again.
  - Chosen client leaves → as declined; the request is not resumed after a rejoin.
  - Daily limit reached meanwhile → refused at grant, both told "No free rewards left today".
- **All clients refused / no ad / in cooldown:** the host's popup shows **[Force a random player]**
  (only when `ForceWatchWhenAllDecline` is on, default, and at least one client reports an ad
  loaded), **[Me — the restaurant pauses]** and **[Cancel]**. Nothing is forced until the host
  presses Force. Then the server picks a random client with an ad loaded (ignores cooldown) and sends
  a forced request: the client shows "Nobody watched — ad starts in 3 s" and then the rewarded ad,
  no decline button. Completed → reward; failed/closed early → no reward and the request ends
  (never forced twice).
- [Me — the restaurant pauses]: clients see "The host is watching an ad — the restaurant is paused"
  until the host is back (no gameplay pause logic; the host is simply frozen).
- Host leaves mid-request → session ends; no reward.

Interstitials on the host, with other players in the room:
- `Break_Time` timer due → `AdDelegationService` sends it to the **next client in a rotation**
  (starting from the client the host last picked, else the first), no question. It is in addition to
  that client's own `Break_Time` (which keeps its timer). The client shows it at its next safe moment
  (no popup open, not in a cooking close-up, ≥ 90 s since that phone's previous interstitial),
  waiting up to 60 s; otherwise the next client in the rotation is tried once. No client can show it
  → skipped (nobody watches, the host neither).
- `Level_Up` → skipped on the host.
- Alone in the room → the host's interstitials behave as today.

Switch: `ForceWatchWhenAllDecline` lives in a config ScriptableObject (default true) so it can be
turned off without code; later it can come from remote config.

### 5. Logging

`[Net]` log lines: join phase timings (relay, scene, snapshot, spawn), state machine transitions with
reasons, `DisconnectReason`, reconnect attempts and result, seat hold/release, avatar handback, every
ad request and its outcome (including forced), and every interstitial hand-over.

## Verification

- EditMode: `SessionStateMachine` transitions; `AdDelegationRules` (pick, cooldown, all-refuse →
  force, one open request, expiry, grant checks); seat accounting; totals application.
- MPPM (host + 2 clients):
  - host quits → clients get "The restaurant has closed" and the main menu;
  - host transport fails → host saves, main menu, "Connection lost — your restaurant is saved";
  - a client rejoins while the host still holds its old connection → old connection dropped, same
    avatar handed over, never two avatars;
  - client network cut (MPPM disconnect / `Shutdown` on the client transport) → Reconnecting →
    back on the same avatar with the same food; past 60 s → food on a counter, avatar gone, seat free;
  - a 5th player is refused while a seat is held;
  - coins/EXP equal on all peers after a busy minute;
  - ad handover with `FakeAdProvider`: accept, decline → cooldown, all decline → forced, no ad →
    [Me]/[Cancel], `Break_Time` goes to the last pick, `Level_Up` skipped on the host.
- Device (blocked until builds work): Relay idle limit for a suspended phone; real LevelPlay
  callbacks; ad length vs the 60 s timeout.

## Left to the builder

- Which list mirrors need to become idempotent (G2).
- "Nearest free counter" search and which counters accept which food (reuse existing placement
  rules).
- Popup prefabs: reuse the existing popup style (`UIPopupManager`, `PopupDatabase`); English text.
- Whether the MPPM drop test uses the transport `DisconnectRemoteClient` from the host or a client
  `Shutdown`; both must end in `Reconnecting` on the client.
