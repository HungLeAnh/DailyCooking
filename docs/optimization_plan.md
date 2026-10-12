# DailyCooking Optimization Plan

Status of every performance / networking issue found in the codebase.

- **✅ Fixed** — done, verified with a clean console and a green EditMode run.
- **⏸ Deferred** — **the agent's proposal, not yet confirmed by the owner**; needs a design decision before touching (see §3).
- **➖ Stale / not applicable** — the original report no longer matches the code; do not re-do these.

---

## 1. ✅ Fixed

### Per-frame GC and CPU

| Issue | Location | Fix |
|---|---|---|
| **Enum boxing** | `StateManager.cs:27` | `nextStateKey.Equals(...)` → `EqualityComparer<T>.Default.Equals(...)`; boxes nothing on the hot path. |
| **Allocating capsule overlap** | `PlayerStateMachine.cs` | `Physics.OverlapCapsule` → `OverlapCapsuleNonAlloc` with a pre-sized `highlightHitBuffer`. |
| **`async void Update()`** | `InGameState.cs` | Converted to a coroutine; no async state machine per frame. |
| **Empty lifecycle method** | `KitchenGameManager.cs` | Empty `Update()` removed. |
| **Closure allocation per frame** | `GameData.GetPlayerStatsById` | `PlayersStats.Find(player => ...)` (allocates a closure every call, reached from `PlayerStateMachine.HandleMovement`) → index `for` loop. |
| **`Camera.main` on per-frame paths** | `UI/LookAtCamera.cs`, `Tutorial/Window_QuestPointer.cs`, `Camera/CameraController.cs` | Lazily cached reference (`Camera.main` re-searches by tag). `CameraController` now uses `_camera.nearClipPlane`, which is also the correct camera for its own `ScreenToWorldPoint`. |
| **Enum `ToString()` on every state change** | `BotCustomerController.PlayAnimation` | `Animator.Play(animationState.ToString())` → `Animator.Play(BotAnimation.StateHash(state))`, hashes memoized in `BotAnimation`. |

### Allocation on repeated calls

| Issue | Location | Fix |
|---|---|---|
| **List copies for iteration** | `BotManager` (`GameManager_OnStateChanged`, `StopSpawnBot`, `KickAllBots`) | `ToList()` / `new List<GameObject>(activeBots)` → backwards `for` over the live list. `ReturnBotToPool` removes at the current index, so no element is skipped. |
| **Double dictionary lookup** | `UIPopupManager` | `ContainsKey` + `TryGetValue` / `ShowPopup` + `ShowUIPopup` → a single `TryGetValue`; `RemoveFromeVisibleList` → one `List.Remove`. |
| **Double type test** | `KitchenObject.TryGetTableware` | `is` followed by `as` → a single `as` + null check. |
| **Recipe scans (O(n))** | `RecipeDatabaseSO` | Dictionary caches built once in `Initialize()`; `CookingTool`/`RecipeResolver` read through them. |

### Correctness bugs reported as perf issues

| Issue | Location | Status |
|---|---|---|
| **Bot update guard** | `BotCustomerController.cs`, `BotStateMachine.cs` | Now `if (!IsServer) return;` — correct on host and dedicated server. |
| **Broken `StopCoroutine`** | `BotManager.cs` | The `Coroutine` returned by `StartCoroutine` is stored and passed back. |
| **`Table.cs` `Destroy(this)`** | `Table.cs` | No longer present. |
| **`UICharacterPopup` wrong operator** | `UICharacterPopup.cs` | `unfittables != null \|\| ...Count > 0` → `&&`. |
| **Interstitial ads never shown** | `InGameState.cs` | `ShowInterstitialAds("Break_Time")` restored and null-guarded. |
| **Plaintext passwords in saves** | `SavedData` / `LoadGame` | SHA-256 + per-save salt (`PasswordHasher`, `v1$salt$hash`); legacy plaintext still loads and is migrated on write. |
| **Ads consent granted by default** | `AdsManager` | Deny-by-default (`IsGdprConsentGranted` false, `IsCcpaOptOut` true) with `SetGdprConsent` / `SetCcpaOptOut` hooks for a future CMP. |

---

## 2. ➖ Stale — already handled or no longer applicable

These were in the original report but do not describe the current code:

- **`BuildingGhost` NetworkObject churn (`BuildingGhost.cs:127-153`)** — the preview is already a *local, non-networked* copy (`CreateLocalPreview`, BuildingGhost.cs:197): it is instantiated under an inactive holder with all MonoBehaviours and `NavMeshObstacle`s disabled. `RefreshVisual` only runs when the player picks an item from the inventory, not per drag frame, so there is nothing to pool.
- **`CuttingCounterController` `ToList().IndexOf`** — no longer present; cutting goes through `RecipeDatabaseSO` dictionary caches.
- **`PanCookingTool` / `PotCookingTool` linear scans** — both delegate to `RecipeResolver`, which reads the dictionary caches.
- **`Renderer.materials` leakage in `BotCustomerController`** — material copies are owned by `HighlightMaterials`, which documents the copy and frees them in `OnDestroy`.
- **`popupType.ToString()` in `UIPopupManager`** — the popup is looked up by enum key now.
- **`Double Dictionary Searches` in `UIPopupManager`** — merged into a single `TryGetValue`.
- **LINQ in UI loops** — the remaining chains (`UIUpgradePopup`, `UICharacterPopup`, `UIMenuPopup`) run when a popup opens or a tab is switched, not per frame. Only worth revisiting together with UI pooling below.

---

## 3. ⏸ Deferred — agent proposal, not confirmed by the owner

**Provenance.** This grouping is the **agent's own recommendation**, written in commit `92a5da3cc`.
No owner sign-off is recorded anywhere in this repo, and no date or quote backs it. Read "Deferred"
here as *"the agent suggests not starting these"* — **not** as a decision that has been made.

Nothing in this list has been dropped or silently deprioritised. Each item still has a full plan file
under `docs/plans/` (`01`–`06`) documenting current state with `file:line`, the proposed change, its
blast radius and how to verify it. The owner can pull any of them back into the work order at any
time; the only reason they sit last in the order is the recommendation below, which is still waiting
on numbers from `docs/plans/13-profiler-baseline.md`.

**Stated reason (technical, not a priority call).** Each one changes on-the-wire or save behaviour,
so it needs a plan plus a multiplayer test pass before it is safe to start.

1. **Redundant `ServerRpc` → `ClientRpc` chains.** `ContainerCounterController`, `Table` and `KitchenObject` forward a `ServerRpc` that does nothing server-side and immediately mirrors to clients, even though the value is already a `NetworkVariable`. Removing them halves RPC traffic, but every consumer that currently reacts to the RPC has to be re-wired to `OnValueChanged` — this is where visual desync would show up.
2. **JSON game-state sync.** `MultiplayerManager` serialises the whole `GameData` to JSON, gzips it and ships it in 4 KB chunks on join (`ReceiveGameDataChunkClientRpc`, see `docs/dataflow.md` §2). Replacing it with structured `NetworkVariable` sync or binary writing removes the CPU cost and the chunking, but the join path and the save snapshot share the same serialiser, so both must move together.
3. **GUID string → integer / `NetworkGuid`.** RPCs carry 36-character content GUIDs (`kitchenObjectSO.Guid`). Compressing them would need a stable GUID↔index table that survives reordering of the config assets, on both hosts and clients, and must not change the save format (`GameData.SaveVersion` bump).
4. **UI object pooling.** Tabs, inventory rows and popup frames are instantiated and destroyed on open/close (`UICharacterPopup`, `UIUpgradePopup`, `UIMenuPopup`). Low value on its own — popups open rarely — but it is the prerequisite for removing the LINQ chains in §2.
5. **Remaining `Camera.main` references (17).** All are in click/drag handlers (`UtilsClass`, `GameInput`, `BuildingPlacementManager`, `GridBuildingSystem`, `PlayerStateMachine`, `DOTweenHandClick`, `Button_Sprite`), so they are event-driven rather than per-frame. Bundle with any input refactor rather than doing them one by one.
6. **Deep singleton chain in `PlayerStateMachine.HandleMovement`.** Runs every frame but is now allocation-free (§1); caching the `PlayerStats` reference would need invalidation on `ReplaceGameDataFromHost` and `OnPlayerStatsAdded`.

---

## 4. Verification

Every change above was checked with:

1. `refresh` + force recompile, then the editor console must be clean of errors and warnings.
2. EditMode test run (**49/49**: save system incl. `PasswordHasher`, grid placement, economy, data classes, `GameData` lookup, `BotAnimation` state hashes).

Profiling was **not** run for the §1 fixes — those changes are reasoned about, not measured.

A baseline captured on 2026-10-12 (Unity Editor Play Mode, single client on a real Relay host) is
recorded in `docs/plans/13-profiler-baseline.md`: across scenarios S1/S2/S5/S6/S10 the main thread
never exceeded **4.59 ms**. Still unmeasured: GC bytes per second, and scenarios S3/S4/S7/S8/S9,
which need a hand-driven run. Those gaps are the reason §3 is still open rather than closed.
