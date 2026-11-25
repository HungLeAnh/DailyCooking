# Third-Person Camera with Station Close-Ups Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the top-down camera with a third-person walking camera, a close-up camera that glides to a cooking tool when the player interacts with it, and an overhead camera for build mode. Interaction moves from tapping the world to an on-screen Interact button.

**Architecture:** Three `CinemachineCamera`s (Walk, CloseUp, Build) in `GameScene`. A local, non-networked `CameraModeController` switches between them by priority and the `CinemachineBrain` blends. All decisions (mode transitions, close-up placement, target choice, camera-relative movement, look-finger tracking, occlusion) live in small pure C# classes with EditMode tests. MonoBehaviours only wire them to Unity.

**Tech Stack:** Unity 6000.6, URP, Cinemachine 3 (`com.unity.cinemachine` 6.6.0, `Unity.Cinemachine` namespace), Input System 1.20 (+ EnhancedTouch), Netcode for GameObjects 2.13, NUnit EditMode tests.

**Spec:** `docs/superpowers/specs/2026-11-25-third-person-camera-design.md`

## Global Constraints

- Unity project root: `DailyCooking/` (repo-relative). All paths below are relative to `DailyCooking/Assets/`.
- Scene and prefab changes are made in the Unity Editor (or through the Unity/Coplay MCP), never by hand-editing `.unity`/`.prefab` YAML.
- Cameras are local to each device. Nothing added may carry a `NetworkObject`, add an RPC, or change any prefab's network registration (`DefaultNetworkPrefabs.asset`).
- Host-authoritative flow stays as in `docs/dataflow.md`: interaction still goes through `PlayerStateMachine.InteractServerRpc`, unchanged.
- Zoom **only** at cooking tools: a `CookingTool`, or a `CookingToolCounterController` whose `HasToolInstalled()` is true. Plain counters, trash, tableware, PostBox, tables, plates, customers never zoom.
- A close-up is left only by the Exit/Back button (plus tool despawn and build start). The joystick does nothing during a close-up.
- Tapping the world no longer interacts in play mode. Build mode keeps its existing touch handling.
- Landscape only. The 7 tools that don't nest `_BaseCookingTool` (Coffee, Soda, IceCream, Pancake, Grill, Kettle, Teapot) must not be re-nested.
- `UIHUDElements` is serialized as ints in the scene: append new values only.
- Blends quick: default blend ≤ 0.5 s.
- New pure classes go in the `Scripts` assembly; tests go flat in `Assets/Tests/` (assembly `Tests`, already references `Scripts`).
- Run tests with Unity Test Runner (Window › General › Test Runner › EditMode › Run All) or MCP `run_tests(EditMode)`. Baseline: 49 passing.

## Review Focus

1. **Looking while walking:** the left thumb holds the joystick while the right thumb drags. Expected: the camera turns with the right thumb, and the joystick finger never turns the camera. → `LookTouchTrackerTests` (Task 3).
2. **The tool disappears mid close-up** (picked up in build mode, or despawned by the host). Expected: back to Walk, with no null errors. → `CameraModeMachineTests.ToolGone_*` (Task 1).
3. **Build mode starts during a close-up, then ends.** Expected: Build, then Walk; it never returns to the old close-up. → `CameraModeMachineTests.BuildEnded_AfterCloseUp_GoesToWalk` (Task 1).
4. **The player stands on top of the tool, or every side of the tool is against a wall.** Expected: a usable shot anyway (the tool's forward side, or top-down). → `CloseUpPlacementTests` (Task 2).
5. **Interact pressed with nothing in range, or with only a plain counter in range.** Expected: no camera change, and no RPC when nothing is in range. → `InteractTargetSelectorTests.Empty_ReturnsMinusOne` (Task 4) plus the manual check in Task 6.

---

### Task 1: Camera mode state machine

**Files:**
- Create: `Scripts/Camera/CameraModeMachine.cs`
- Test: `Tests/CameraModeMachineTests.cs`

**Interfaces:**
- Produces:
  - `public enum CameraMode { Walk, CloseUp, Build }`
  - `public sealed class CameraModeMachine` with:
    - `CameraMode Mode { get; }` (starts `Walk`)
    - `object CloseUpTarget { get; }` (null unless `CloseUp`)
    - `event Action<CameraMode> Changed` (fires only on a real change, including a CloseUp target switch)
    - `bool EnterCloseUp(object target)`: ignored (returns false) in `Build` or when the target is null. In `Walk` it moves to `CloseUp`. In `CloseUp` with a different target it switches target.
    - `void ExitCloseUp()`: `CloseUp` → `Walk`.
    - `void BuildStarted()`: any mode → `Build`, and clears the target.
    - `void BuildEnded()`: `Build` → `Walk`.
    - `void ToolGone(object target)`: if `CloseUp` on that target → `Walk`.

- [ ] **Step 1: Write the failing tests** in `Tests/CameraModeMachineTests.cs`:
  - `Starts_InWalk`: `Mode == Walk`, `CloseUpTarget == null`.
  - `EnterCloseUp_FromWalk_GoesCloseUp`: returns true, `Mode == CloseUp`, `CloseUpTarget == t1`, and `Changed` fired once with `CloseUp`.
  - `EnterCloseUp_Null_IsIgnored`: returns false, stays `Walk`, and `Changed` never fires.
  - `EnterCloseUp_DuringBuild_IsIgnored`: after `BuildStarted()`, returns false and stays `Build`.
  - `EnterCloseUp_OtherTarget_SwitchesTarget`: t1, then t2 → `CloseUpTarget == t2`, and `Changed` fired twice.
  - `EnterCloseUp_SameTarget_NoEvent`: t1 twice → `Changed` fired once.
  - `ExitCloseUp_GoesWalk`.
  - `ToolGone_CurrentTarget_GoesWalk`; `ToolGone_OtherTarget_StaysCloseUp`.
  - `BuildStarted_FromCloseUp_ClearsTarget`: `Mode == Build`, `CloseUpTarget == null`.
  - `BuildEnded_AfterCloseUp_GoesToWalk`: EnterCloseUp(t1) → BuildStarted → BuildEnded → `Mode == Walk`, `CloseUpTarget == null`.

- [ ] **Step 2: Run the tests and check they fail.** Expected: compile error, `CameraModeMachine` not found.
- [ ] **Step 3: Implement `CameraModeMachine`** as specified in Interfaces. Plain C#, no `UnityEngine` dependency.
- [ ] **Step 4: Run the EditMode tests.** Expected: all `CameraModeMachineTests` pass, and the 49 existing tests still pass.
- [ ] **Step 5: Commit**: `git add DailyCooking/Assets/Scripts/Camera/CameraModeMachine.cs* DailyCooking/Assets/Tests/CameraModeMachineTests.cs*`, then `git commit -m "feat(camera): camera mode state machine"`

---

### Task 2: Close-up placement

**Files:**
- Create: `Scripts/Camera/CloseUpPlacement.cs`
- Test: `Tests/CloseUpPlacementTests.cs`

**Interfaces:**
- Produces:
  - `public struct CloseUpShot { public Vector3 Position; public bool TopDown; }`
  - `public static class CloseUpPlacement`, with `public static CloseUpShot Choose(Vector3 lookAt, Vector3 toolForward, Vector3 playerPosition, float distance, float height, Func<Vector3, Vector3, bool> isBlocked)`.
    `isBlocked(from, to)` returns true when line of sight from `from` to `to` is blocked.

Algorithm (the spec fixes the order; the implementer writes it):
1. `preferred` = (playerPosition − lookAt) projected on XZ and normalized. If its length is under 0.01, use `toolForward` projected on XZ and normalized.
2. Candidates in order: `preferred`, then the tool's four sides (`f`, `right`, `−f`, `−right`, where `f` = flattened `toolForward` and `right = Cross(up, f)`) sorted by ascending angle to `preferred`.
3. For each candidate `d`: `pos = lookAt + d * distance + Vector3.up * height`. Return the first where `!isBlocked(pos, lookAt)`, with `TopDown = false`.
4. If all are blocked: `Position = lookAt + Vector3.up * (height + distance)`, `TopDown = true`.

- [ ] **Step 1: Write the failing tests** in `Tests/CloseUpPlacementTests.cs`. Use `lookAt (0,1,0)`, `toolForward (0,0,1)`, `distance 2`, `height 1.5`, tolerance 0.001:
  - `Unblocked_UsesPlayerSide`: player `(3,0,0)`, never blocked → `Position == (2,2.5,0)`, `TopDown == false`.
  - `PlayerOnTool_UsesToolForward`: player `(0,0,0)` → `Position == (0,2.5,2)`.
  - `PreferredBlocked_UsesNearestToolSide`: player `(3,0,1)`, blocked when `Mathf.Abs(from.z) > 0.1f` (blocks the diagonal preferred direction, ≈18° off +X, but not the pure +X side) → `Position == (2,2.5,0)`.
  - `AllBlocked_GoesTopDown`: always blocked → `Position == (0,4.5,0)`, `TopDown == true`.
  - `IsBlocked_ReceivesLookAtAsTarget`: capture the `to` argument and assert it equals `lookAt`.
- [ ] **Step 2: Run the tests and check they fail** (compile error).
- [ ] **Step 3: Implement `CloseUpPlacement.Choose`.**
- [ ] **Step 4: Run the EditMode tests.** All pass.
- [ ] **Step 5: Commit**: `feat(camera): close-up placement from the player's side with fallback`

---

### Task 3: Camera-relative movement and look-finger tracking

**Files:**
- Create: `Scripts/Camera/CameraRelativeMovement.cs`, `Scripts/Camera/LookTouchTracker.cs`
- Test: `Tests/CameraRelativeMovementTests.cs`, `Tests/LookTouchTrackerTests.cs`

**Interfaces:**
- Produces:
  - `public static class CameraRelativeMovement`, with `public static Vector3 ToWorld(Vector2 input, float cameraYawDegrees)`. Rotates `(input.x, 0, input.y)` by the yaw around Y, so yaw 0 means "up = +Z", the same as today.
  - `public sealed class LookTouchTracker`:
    - `void Began(int touchId, Vector2 position, bool overUI)`: adopts the touch as the look finger only if `!overUI` and no look finger is held.
    - `Vector2 Moved(int touchId, Vector2 position)`: returns the delta since the last position for the look finger. Returns `Vector2.zero` for any other touch.
    - `void Ended(int touchId)`: releases the finger if it is the look finger.
    - `bool HasLookFinger { get; }`

- [ ] **Step 1: Write the failing tests.**
  - `CameraRelativeMovementTests`:
    - `Yaw0_UpIsPlusZ`: `ToWorld((0,1), 0) ≈ (0,0,1)`.
    - `Yaw90_UpIsPlusX`: `ToWorld((0,1), 90) ≈ (1,0,0)`.
    - `Yaw90_RightIsMinusZ`: `ToWorld((1,0), 90) ≈ (0,0,-1)`.
    - `Zero_StaysZero`.
    - `Length_IsPreserved`: input `(0.6,0.8)` at yaw 37 → magnitude ≈ 1.
  - `LookTouchTrackerTests`:
    - `TouchOverUI_IsNotAdopted`: `Began(0, p, true)` → `HasLookFinger == false`, and `Moved(0, p2) == zero`.
    - `JoystickFingerThenLookFinger_OnlyLookFingerTurns`: `Began(0,(100,100),true)`, then `Began(1,(800,300),false)`. `Moved(0,(150,100)) == zero`, and `Moved(1,(820,310)) == (20,10)`.
    - `SecondFreeTouch_IsIgnoredWhileLookFingerHeld`: touches 1 and 2 both free → `Moved(2, …) == zero`.
    - `Ended_ReleasesAndAllowsNewFinger`: `Ended(1)`, then `Began(2, …, false)` → `HasLookFinger`, and touch 2's moves return deltas.
    - `FirstMove_DeltaFromBeganPosition`.
- [ ] **Step 2: Run the tests and check they fail.**
- [ ] **Step 3: Implement both classes.**
- [ ] **Step 4: Run the EditMode tests.** All pass.
- [ ] **Step 5: Commit**: `feat(camera): camera-relative movement and look-finger tracking`

---

### Task 4: Interact target selection

**Files:**
- Create: `Scripts/Player/InteractTargetSelector.cs`
- Test: `Tests/InteractTargetSelectorTests.cs`

**Interfaces:**
- Produces: `public static class InteractTargetSelector`, with `public static int SelectIndex(Vector3 playerPosition, Vector3 playerForward, IReadOnlyList<Vector3> candidatePositions, float tieAngleDegrees = 10f)`. Returns −1 for an empty list. All vectors are compared on XZ. The candidate with the smallest angle to `playerForward` wins. Candidates within `tieAngleDegrees` of the best angle are tied, and the nearest of those wins. A candidate at the player's own position counts as angle 0.

- [ ] **Step 1: Write the failing tests** (player at origin, forward `(0,0,1)`):
  - `Empty_ReturnsMinusOne`.
  - `PicksMostInFront`: candidates `[(2,0,0), (0,0,2)]` → 1.
  - `TieWithinAngle_PicksNearest`: `[(0.3,0,3), (0,0,1)]` (≈5.7° apart) → 1.
  - `OutsideTie_AngleBeatsDistance`: `[(1,0,1), (0,0,3)]` → 1.
  - `BehindPlayer_StillSelectableWhenOnlyOne`: `[(0,0,-1)]` → 0.
  - `IgnoresHeight`: `[(0,5,1), (1,0,1)]` → 0.
- [ ] **Step 2: Run the tests and check they fail.**
- [ ] **Step 3: Implement `SelectIndex`.**
- [ ] **Step 4: Run the EditMode tests.** All pass.
- [ ] **Step 5: Commit**: `feat(player): choose the interact target in front of the player`

---

### Task 5: Camera rig, mode controller and look input in the scene

**Files:**
- Create: `Scripts/Camera/CameraModeController.cs`, `Scripts/Camera/CloseUpFraming.cs`
- Modify: `Scripts/Camera/CameraController.cs` (becomes the Build camera's driver), `Scripts/Player/GameInput.cs` (add the look event)
- Modify (Editor): `Scenes/GameScene.unity`, the 13 prefabs in `Prefab/CookingTool/`

**Interfaces:**
- Consumes: `CameraModeMachine`, `CameraMode` (Task 1); `CloseUpPlacement.Choose`, `CloseUpShot` (Task 2); `LookTouchTracker` (Task 3).
- Produces:
  - `public class CloseUpFraming : MonoBehaviour` with serialized `float distance = 1.6f`, `float height = 1.4f`, `float fieldOfView = 40f`, `Vector3 lookAtOffset = new(0, 0.9f, 0)`, plus `public Vector3 LookAt => transform.TransformPoint(lookAtOffset)` and read-only properties for the rest. The values are starting defaults; tune per tool in Step 7.
  - `public class CameraModeController : Singleton<CameraModeController>` (GameScene only), with:
    - `CameraMode Mode { get; }`
    - `event Action<CameraMode> OnModeChanged`
    - `float WalkYaw { get; }`: the Walk camera's Y rotation in degrees.
    - `bool RequestCloseUp(CookingTool tool)`
    - `void ExitCloseUp()`
    - `CookingTool CloseUpTool { get; }`
    - `Vector3 CloseUpLookAt { get; }`: the look-at point used for the current close-up (framing or default)
    - `Transform CloseUpCameraTransform { get; }`
  - `GameInput.OnLookDelta` (`event EventHandler<Vector2>`, pixels per frame).

- [ ] **Step 1: Add `OnLookDelta` to `GameInput`.** Enable `EnhancedTouchSupport` in `Awake`. Each `Update`, feed `Touch.activeTouches` (Began, Moved, Ended/Canceled; `overUI` from `EventSystem.current.IsPointerOverGameObject(touchId)`) into a `LookTouchTracker`, and raise `OnLookDelta` with any non-zero delta. On desktop (no touchscreen), raise the mouse delta while the right mouse button is held. Leave existing events untouched; build mode still uses them.
- [ ] **Step 2: Implement `CloseUpFraming`** as specified in Interfaces.
- [ ] **Step 3: Implement `CameraModeController`:**
  - Serialized `CinemachineCamera walkCamera, closeUpCamera, buildCamera`; `LayerMask closeUpBlockers` (Building, ConstraintWall); `float lookDegreesPerPixel = 0.2f`.
  - Owns a `CameraModeMachine`. On `Changed`: give the live camera `Priority = 20`, the others 0, and raise `OnModeChanged`.
  - `RequestCloseUp(tool)` reads the tool's `CloseUpFraming`, falling back to the defaults above when absent. It calls `CloseUpPlacement.Choose(framing.LookAt, tool.transform.forward, PlayerStateMachine.LocalInstance.transform.position, distance, height, (a,b) => Physics.Linecast(a, b, closeUpBlockers))`. Then it positions `closeUpCamera` at `shot.Position`, rotates it to look at `LookAt`, sets its FOV, and calls `machine.EnterCloseUp(tool)`.
  - In `Update`: if the mode is `CloseUp` and the tool is null or `!tool.IsSpawned`, call `machine.ToolGone(...)`. In `Walk`, set the Walk camera to follow `PlayerStateMachine.LocalInstance` once it exists.
  - Subscribe to `GridBuildingSystem.Instance.BuildingPlacementManager.OnBuildingStart` / `OnBuildingEnd` → `BuildStarted()` / `BuildEnded()`. Use the same "already initialized or wait for `OnObjectSpawned`" pattern `CameraController.Start` uses.
  - `OnLookDelta` in `Walk` turns the Walk camera's orbital horizontal axis by `delta.x * lookDegreesPerPixel`. It is ignored in other modes.
- [ ] **Step 4: Gate `CameraController` to Build mode.** Pan, zoom and rotate handlers return early unless `CameraModeController.Instance.Mode == CameraMode.Build`. Delete the dead `Instance_OnPlayerSpawned` and the commented-out code.
- [ ] **Step 5: Set up the scene (Editor), in `GameScene`:**
  - The existing "Virtual Camera" becomes the Build camera. Switch its lens to **perspective** (FOV 60). Set `CameraController.zoomBounds` to `(30, 70)`. Remove its leftover `CinemachineThirdPersonFollow` and `CinemachineRotationComposer`.
  - Add "Walk Camera": a `CinemachineCamera` with `CinemachineOrbitalFollow` (horizontal axis wraps), `CinemachineRotationComposer` and `CinemachineDeoccluder` (collide against Building and ConstraintWall).
  - Add "CloseUp Camera": a `CinemachineCamera` with no Follow/LookAt components; it is placed by script.
  - Add `CameraModeController` to the camera root and wire all three cameras. On `MainCamera`'s `CinemachineBrain`, set the default blend to EaseInOut 0.35 s.
- [ ] **Step 6: Add `CloseUpFraming`** to all 13 prefabs in `Prefab/CookingTool/`. For the 6 that nest `_BaseCookingTool`, add it once on the base, then check the nested prefabs received it. Add it directly to the other 7 (Coffee, Soda, IceCream, Pancake, Grill, Kettle, Teapot).
- [ ] **Step 7: Check it manually in Play Mode** (single player, landscape Game view):
  - Walk camera follows behind the character.
  - Dragging off-UI turns it.
  - Starting build mode blends to the overhead view, where pan and zoom work. Ending build mode blends back.
  - Temporarily calling `RequestCloseUp` on a pan (Inspector context menu or test hook) glides to a close-up from the player's side.
  - Then tune each tool's framing so its contents fill roughly the middle third of the screen.
- [ ] **Step 8: Run the EditMode tests.** All pass, and the console has no errors.
- [ ] **Step 9: Commit**: `feat(camera): walk/close-up/build Cinemachine rig with mode controller`

---

### Task 6: Player movement, Interact button and close-up trigger

**Files:**
- Modify: `Scripts/Player/PlayerStateMachine.cs` (`Start`/`OnDestroy` subscriptions, `PlayerStateMachine_OnMouseClickPerformed`, `HandleInteractions`, `HandleMovement`)
- Modify: `Scripts/Player/PlayerStateContext.cs` (add the target)
- Modify: `Scripts/MVC/Controllers/CounterControllers/CookingToolCounterController.cs` (expose the tool)
- Modify: `Scripts/UI/UIHUDManager.cs` (new elements and clicks)
- Modify (Editor): the HUD in `GameScene`

**Interfaces:**
- Consumes: `CameraRelativeMovement.ToWorld` (Task 3); `InteractTargetSelector.SelectIndex` (Task 4); `CameraModeController.Instance` with `Mode`, `WalkYaw`, `RequestCloseUp`, `ExitCloseUp`, `CloseUpTool`, `OnModeChanged` (Task 5).
- Produces:
  - `PlayerStateContext.InteractTarget` (`IInteractable`, set every frame by the owner)
  - `PlayerStateMachine.PressInteract()` (public; owner only)
  - `CookingToolCounterController.InstalledTool` (`CookingTool`, null when none)
  - `UIHUDElements.Interact` and `UIHUDElements.Back`, appended after `Character`
  - `UIHUDManager.OnInteractClick()`, `UIHUDManager.OnBackClick()`

- [ ] **Step 1: Movement.** In `HandleMovement`, build `moveDir` with `CameraRelativeMovement.ToWorld(inputVector, yaw)`. Use `yaw = CameraModeController.Instance?.WalkYaw ?? 0` in `Walk`, and today's world axes (yaw 0) in `Build`. Return without moving when the mode is `CloseUp`. Do the same direction change in `HandleInteractions`, where `Context.LastInteractDir` is set.
- [ ] **Step 2: Target selection.** In `HandleInteractions`:
  - Keep building `Context.Highlightable` as the full in-range list. `CookingStartButtonWorldUI` reads it.
  - Resolve each entry to an `IInteractable`: `GetComponentInParent<IInteractable>()`, or `CookingTool` → `CookingToolCounterController.FindControllerForTool`.
  - Pick `Context.InteractTarget` with `InteractTargetSelector.SelectIndex(transform.position, transform.forward, positions)`.
  - Call `OnSelected()` only on the target's highlightable and `OnDeselected()` on the previous one; drop today's "select all in range". In `CloseUp`, lock the target to `CloseUpTool`'s controller.
- [ ] **Step 3: `PressInteract()`.**
  - Return if not owner, input disabled, or the target is null.
  - Compute `alternate` as today: the target is `IHasProgress`, not `IsDone()`, and `GetProgress() != -1`. Call `RequestInteract(target, alternate)`.
  - If the mode isn't `CloseUp`, the target is a `CookingToolCounterController` with `HasToolInstalled()`, and the mode isn't `Build`, call `CameraModeController.Instance.RequestCloseUp(controller.InstalledTool)`.
  - Subscribe `GameInput.OnInteract1Performed` → `PressInteract` (the F key).
  - Remove the `OnMouseClickPerformed` subscription and delete `PlayerStateMachine_OnMouseClickPerformed`.
- [ ] **Step 4: HUD.**
  - Append `Interact` and `Back` to `UIHUDElements`.
  - Add an Interact button (bottom-right) and a Back button (top-left) to the GameScene HUD, registered in `uiHUDElementList`. Clicks call `OnInteractClick()` → `PlayerStateMachine.LocalInstance?.PressInteract()` and `OnBackClick()` → `CameraModeController.Instance.ExitCloseUp()`.
  - On `CameraModeController.OnModeChanged`, apply visibility:
    - Interact: shown in Walk and CloseUp.
    - Back: shown in CloseUp only.
    - Rotate: shown in Build only.
  - Pinch zoom is already gated by Task 5.
- [ ] **Step 5: Run the EditMode tests.** All pass.
- [ ] **Step 6: Check it manually in Play Mode**, as host plus one MPPM client:
  - The joystick follows the camera direction.
  - Tapping the world does nothing.
  - Only the faced object highlights.
  - Interact at a fridge or plain counter picks up or drops with **no zoom**; with nothing in range, nothing happens.
  - Interact at a pan glides into a close-up. Interact again puts in or takes out items. On the cutting board, repeated Interact cuts.
  - The joystick does nothing in a close-up, and Back glides out.
  - Picking the tool up in build mode during a close-up returns to Walk, then Build.
  - The Blender's world start button still works in the close-up.
  - The client's camera never moves when the host zooms.
- [ ] **Step 7: Commit**: `feat(player): interact button, camera-relative movement and cooking-tool close-up`

---

### Task 7: Hide avatars blocking a close-up

**Files:**
- Create: `Scripts/Camera/SegmentProximity.cs`, `Scripts/Camera/CloseUpOccluderHider.cs`
- Modify: `Scripts/Player/PlayerStateMachine.cs` (static registry)
- Test: `Tests/SegmentProximityTests.cs`

**Interfaces:**
- Consumes: `CameraModeController.OnModeChanged`, `Mode`, `CloseUpLookAt`, `CloseUpCameraTransform` (Task 5).
- Produces:
  - `public static bool SegmentProximity.IsNear(Vector3 a, Vector3 b, Vector3 point, float radius)`: true when the distance from `point` to segment `ab` is ≤ `radius`.
  - `public static IReadOnlyList<PlayerStateMachine> PlayerStateMachine.All`, kept in `OnNetworkSpawn` / `OnNetworkDespawn`.

- [ ] **Step 1: Write the failing tests:**
  - `PointOnSegment_IsNear`.
  - `PointBesideMiddle_UsesPerpendicularDistance`: `a (0,0,0)`, `b (0,0,10)`, `p (0.5,0,5)`, radius 0.6 → true; radius 0.4 → false.
  - `PointBeyondEnd_UsesEndpointDistance`: `p (0,0,11)`, radius 0.9 → false; radius 1.1 → true.
  - `DegenerateSegment_UsesPointDistance`.
- [ ] **Step 2: Run the tests and check they fail.**
- [ ] **Step 3: Implement `SegmentProximity.IsNear`** and the `PlayerStateMachine.All` registry.
- [ ] **Step 4: Implement `CloseUpOccluderHider`** (on the camera root):
  - While the mode is `CloseUp`, each frame, for every avatar in `PlayerStateMachine.All`, the local one included, test `IsNear(CameraModeController.Instance.CloseUpCameraTransform.position, CameraModeController.Instance.CloseUpLookAt, avatar.position + Vector3.up * 1f, 0.6f)`.
  - Disable the avatar's `Renderer`s while near, and re-enable them when not near or when leaving `CloseUp`. Remember exactly which renderers were turned off so nothing else gets re-enabled.
- [ ] **Step 5: Run the EditMode tests.** All pass.
- [ ] **Step 6: Check it manually** with host plus client: a client avatar standing between the host's close-up camera and the tool disappears for the host only, then reappears after Back.
- [ ] **Step 7: Commit**: `feat(camera): hide avatars that block a close-up`

---

### Task 8: Tutorial and docs pass

**Files:**
- Modify as needed: `Scripts/Tutorial/*.cs`, `docs/project_overview.md`

- [ ] **Step 1:** Play through the first-time tutorial (fresh save). For each step whose hint points at a screen position or tells the player to tap a world object, change the hint to the Interact button, or re-target it to the new camera. List the changed steps in the commit message.
- [ ] **Step 2:** In `docs/project_overview.md` under Core Architecture, add one short paragraph: the three-camera rig, `CameraModeController`, and the Interact/Back buttons, linking the spec.
- [ ] **Step 3:** Run the EditMode tests. All pass.
- [ ] **Step 4: Commit**: `docs+tutorial: adapt tutorial hints and overview to the third-person camera`
