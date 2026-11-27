# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

- The Unity project is in `DailyCooking/` (not the repo root). Game code: `DailyCooking/Assets/Scripts/` (one assembly, `Scripts.asmdef`). Tests: `DailyCooking/Assets/Tests/` (`Tests.asmdef`, Editor-only, EditMode).
- `docs/` holds the design and analysis notes. Start with `docs/project_overview.md` (architecture summary, kept up to date) and `docs/dataflow.md` (network authority per system). Feature designs are in `docs/superpowers/specs/`, implementation plans in `docs/superpowers/plans/`; past optimization/verification work (items 01–21) is in `docs/optimization-log.md`; open/deferred work is tracked in `docs/deferred.md`. Several docs are written in Vietnamese.

## Tooling

- Unity **6000.6.2f1** (Unity 6.6), URP 17.6, Netcode for GameObjects 2.13 + Unity Transport + Relay (Multiplayer Services), Input System, LevelPlay (ads), Unity IAP. Target platform: Android.
- MCP-for-Unity bridge (`com.coplaydev.unity-mcp`) is installed for driving the open Editor.
- Run EditMode tests (Editor must be closed for batch mode):
  ```
  Unity.exe -batchmode -projectPath DailyCooking -runTests -testPlatform EditMode -testResults results.xml
  ```
  Single test/fixture: add `-testFilter <Fixture or Namespace.Fixture.Method>`. Or use Window > General > Test Runner in the Editor.
- Editor menu tools (`Assets/Editor/`): `Tools/Build Checks/*` (pinned enum values, placed-object GUIDs, compile Android player scripts), `Tools/Profiling/*` and `Tools/Verify/*` (`PerfScenarios.cs`: enter game as host, skip tutorial, scripted repro scenarios), config generators (`Tools/ConfigFood|ConfigShop|ConfigUpgrade`, from CSV).
- Multiplayer testing in-Editor uses Multiplayer Play Mode (virtual `Player 2`…).
- Scenes: `MainMenuScene` (entry) and `GameScene` are in the build; `TestScene` and `CookedPreview` are dev-only.

## Architecture (big picture)

**Host-authoritative networking — the rule everywhere.** Clients send intents via `ServerRpc`; the server validates against its own state and config (never client-supplied prices/values), applies the change, and clients learn the result through server-written NetworkVariables/NetworkLists, spawns, or `SendTo.NotServer` mirror RPCs. Mirrors send resulting totals, never "+X" deltas. Read `docs/dataflow.md` before touching any networked system.

- **Interactions:** a click/USE on a counter, plate or customer goes through `PlayerStateMachine.InteractServerRpc` (ownership + range checked), which runs the target's `InteractEvent(actor)` on the server. Per-player UI feedback goes through `UIManager` (server-owned NetworkBehaviour, `RpcTarget.Single`): `ShowAlert(actor, …)`, `ShowOptionMenu(actor, …)`, choices return via `UIManager.RequestOption` → `IHasOptionalSO.ApplyOption`. Player classes hold no UI calls.
- **Kitchen objects** are spawned only by the server via `KitchenGameManager.SpawnKitchenObject` (parent set before `Spawn`). The holder is a server-written NetworkVariable on `KitchenObject`; `DestroySelf` is server-only. No pooling for networked objects (deliberate).
- **Gesture cooking:** USE at a cooking tool only opens a local close-up camera. `CloseUpGestureController` reports one number per gesture via `PlayerStateMachine.ReportGestureServerRpc`; the server's `CookingTool.ApplyGesture` validates it, `CookStepRecorder` records it, and a `QualityRecord` (stars) is written on the output `KitchenObject`. `QualityRules.Payment` turns plate stars into a tip. The client scores nothing.
- **Grid building:** `BuildingGhost` is a local preview; client pre-checks `GridBuildingSystem.CanPlace`, then `PlaceObjectServerRpc` re-checks and spawns. Each peer registers placed objects in `PlacedObjectView.OnNetworkSpawn/OnNetworkDespawn`.
- **Sessions:** `MultiplayerManager` / `SessionManager` (Relay). Join = connection approval by player id (`ConnectionPayload`, `SeatRegistry`, 4 players, seats held 60 s), then the host sends gzipped `GameData` in 4 KB chunks (transport max payload is 6144). `SessionStateMachine` drives the lifecycle; every end goes through `MultiplayerManager.RunExit`. Heartbeat + 60 s client reconnect hands the avatar (and held food) back via `ChangeOwnership`.
- **Host ads delegated to clients:** `GameManager.AdDelegation.cs` + `AdDelegationRules`; ads go through `IAdProvider` (`LevelPlayAdProvider` on device, `FakeAdProvider` in Editor).
- **Managers / singletons:** `Assets/Scripts/Managers/`, base classes in `Assets/Scripts/Common/` (`Singleton<T>`, `NetworkSingleton<T>`, `Persistent*` variants only flip `PersistAcrossScenes`). `GameManager` is a partial class: core, `.Save`, `.Economy` (all purchase/exchange/unlock ServerRpcs), `.Data`, `.AdDelegation`.
- **FSMs:** generic framework in `Assets/Scripts/StateMachine/`; game-level FSM on `GameManager` (MainMenu ↔ InGame); player FSM in `Assets/Scripts/Player/` (`PlayerStateMachine`, states via `PlayerStateFactory`).
- **Counters (MVC):** `Assets/Scripts/MVC/Controllers/CounterControllers/`. Container counters share `BaseContainerCounterController`.
- **Cameras:** three local Cinemachine cameras (Walk / CloseUp / Build) switched by `CameraModeController`; the decisions live in pure, unit-tested classes (`CameraModeMachine`, `CloseUpPlacement`, `InteractTargetSelector`, `LookTouchTracker`).
- **Mobile performance:** `MobilePerformanceManager` + `DeviceTierPicker`, `FrameRateGovernor`, `PerfMemory`; logs are prefixed `[Perf]`.

**Testability pattern:** logic that needs tests is pulled out of MonoBehaviours/NetworkBehaviours into plain C# classes (rules, state machines, recognizers, recorders) that the EditMode tests exercise directly. Follow that when adding logic.

## Data and content rules

- All ScriptableObjects derive from `SerializableScriptableObject`, which stamps a GUID from the asset path in `OnValidate`. Saves and the network protocol reference content only by these GUIDs — don't rename/move SO assets carelessly.
- `RecipeDatabaseSO` is the single source of recipes. Prices/rewards live in config SOs (`ConfigShop`, `ConfigFood`, `ConfigUpgrade`, `Customization_Data` on `ConfigManager`).
- **Enums serialized as numbers** (`ShopItemType`, `ShopItemCategory`, …) are stored in scenes/prefabs/assets: only append new values, never insert or renumber. `BuildChecks.cs` pins the numbers; when appending, update the pinned list in the same commit.
- Counter content pattern (`docs/datamanagement.md` §4.7): content that changes at runtime → resolve by GUID with networked state (A); fixed content → SO baked on the prefab (B), which must null-check and `Debug.LogError` (since `SpawnKitchenObject` silently returns null) and carry a "fixed-content, baked on purpose" comment.
- Adding a new food/counter touches: the SO asset in `Assets/SO/`, the prefab in `Assets/Prefab/`, registration in `KitchenGameManager.kitchenObjectSOList` (scene) or `GridBuildingSystem`, and `DefaultNetworkPrefabs.asset`.
- Runtime code must not mutate shared material/asset instances (copy with `new Material(...)`), or play mode dirties committed assets.
- Persistence: only the host saves (`GameManager.CanSave`); writes are atomic with `.bak` fallback; bump `GameData.SaveVersion` and extend `Migrate()` for save-format changes. JSON `$type` is restricted by a type binder — register new polymorphic types there.

## Git notes

- Main branch is `master`. Commit messages use conventional prefixes with a scope, e.g. `fix(net): …`, `chore(ads): …`, `docs: …`.
- Keep third-party SDK file churn (LevelPlay, Google Play Games, `GeneratedLocalRepo`) out of feature commits unless the change is the SDK update itself.
