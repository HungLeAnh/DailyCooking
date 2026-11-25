# Third-person camera with station close-ups — design

Date: 2026-11-25 · Status: draft for owner review

## Why

The game is played from a fixed top-down orthographic camera that the player pans, pinch-zooms and
rotates over the restaurant grid. The owner's problem with it, in their words: you **can't see the
ingredients or the food**, and it is **not interactive with the environment**. The new view must put
food and ingredients in front of the player at a readable size and make the kitchen feel like a
place you are standing in and handling things, not a board you look down on.

This is the first of three projects the owner asked for (camera view → cooking system →
optimization). It goes first because the cooking redesign depends on how close the camera is, and
optimizing code that these two projects will rewrite would be wasted.

## Goals

- **Walking view:** a third-person camera behind and slightly above the local player's character.
- **Station close-up:** interacting with a **cooking tool** (Pan, Pot, Oven, Grill, Fryer, Blender,
  Kettle, Teapot, Coffee/Soda/Ice-cream/Pancake machines, Cutting board — everything built on
  `_BaseCookingTool`) glides the camera to a close framing of that tool, so its ingredients and
  output are large on screen.
- **Leaving a close-up:** pressing the Exit/Back button glides back to the walking view.
- **Interacting:** an on-screen Interact button acts on the object in front of the character.
- **Build mode:** an overhead view while placing/moving furniture, as today.
- **Joystick** moves the character relative to the camera's facing.

## Non-goals

- **No change to how cooking works.** Inside a close-up the player does exactly what they do today:
  put an item in, take it out, cut, the Blender's start button, option menus — now triggered by
  the Interact button instead of a tap.
  Redesigning cooking interactions is the next project and will build on this close-up.
- No first-person mode, no camera choice in settings.
- No change to networking or authority. Cameras are local to each device; nothing about the camera
  is synchronized.

## Anti-goals (would be a failure even if it technically works)

- A close-up on **every** interaction. Plain counters (Clear, Container/Fridge, Tableware stacks,
  Trash), PostBox, tables, plates and customers must **not** zoom; the owner specifically chose
  "only zoom at cooking tools" so fetching and serving stay fast.
- An Interact button that acts on something other than what the player can see is highlighted.
- Another player's avatar blocking the tool during a close-up.

## Decided by the owner

1. Third person while walking, close-up at stations (chosen over pure first person and plain
   third person).
2. Zoom only at cooking tools.
3. The close-up behaviour below (enter, exit, fixed per-tool angle, today's actions inside, fade
   other players) — "looks right".
4. **Approach A** (below): one Cinemachine camera per mode, Cinemachine blends between them.
5. **Customer orders stay as they are:** speech bubbles above customers only. No off-screen
   indicators or order list in this project, even though customers outside the Walk view will not
   be visible.
6. **Close-up looks from the player's side** (not fixed to the tool's front), with a wall check and
   fallback — see "Close-up framing".
7. **Interaction uses an on-screen button on mobile**, not tapping objects in the world.
8. **A close-up is left with an Exit/Back button.** The joystick does not exit it.
9. Close-up placement, build-mode-only pinch zoom and rotate button, and quick blends: approved.

## Approaches considered

| | Approach | Trade-off | Outcome |
|---|---|---|---|
| **A** | One `CinemachineCamera` per mode: a third-person follow camera, a close-up camera (originally one per tool prefab; now one shared camera placed by script — see "Close-up framing"), and today's overhead camera for build mode. A small local script raises the priority of the one that should be live; the `CinemachineBrain` blends. | One extra (inactive) camera object per tool instance. Framing is authored visually in the prefab; blends, lens changes and damping come from Cinemachine. | **Chosen.** |
| B | A single camera; script swaps its target/offset/lens, each tool only carries an empty "close-up point". | Fewer objects, but the glide, angle and lens transitions are hand-written — the same kind of hand-lerping `CameraController` does today, where jitter comes from. | Rejected. |
| C | Drop Cinemachine, write a custom camera. | More work for a worse result; nothing requires it. | Rejected. |

## Current state the builder needs to know

- `Assets/Scripts/Camera/CameraController.cs` drives the one scene `CinemachineCamera`
  ("Virtual Camera" in `GameScene.unity`) **by moving its transform directly**: pan
  (`OnPanMovedDesktop`), pinch/scroll zoom (`HandleZoom`), a 90° rotate from
  `UIHUDManager.OnRotateClicked`, and a tilt to 90° on `BuildingPlacementManager.OnBuildingStart`
  / back to `_initCameraAngle` on `OnBuildingEnd`. Lens is **orthographic** (size 8).
- That scene camera already carries `CinemachineThirdPersonFollow` and
  `CinemachineRotationComposer` components, and `CameraController` has a commented-out
  `Follow = PlayerStateMachine` line — an earlier, unfinished attempt. Treat them as leftovers.
- `GameInput` (`Assets/Scripts/Player/GameInput.cs`) fires `OnMouseClickPerformed` **on press**
  (`StartClick`); a press held longer than `touchTimeThreshold` (0.1 s) becomes a pan
  (`OnMousePanPerformed`). Two-finger distance change fires `OnScrollPerformed`.
- `PlayerStateMachine` (owner only):
  - `HandleMovement` reads `GameInput.GetMovementVectorNormalized()` and moves along **world X/Z**,
    turning the character to face the move direction.
  - `HandleInteractions` keeps `Context.Highlightable` = interactables within the capsule around
    the player.
  - `PlayerStateMachine_OnMouseClickPerformed` raycasts from `UtilsClass.MainCamera` through the
    tap, resolves `IInteractable` (a tapped `CookingTool` is forwarded to its
    `CookingToolCounterController` via `FindControllerForTool`), rejects it unless it is in
    `Context.Highlightable`, and sends `InteractServerRpc`.
- The `PlayerAction` input asset already has `Interact1` (keyboard F) and `Interact2` (keyboard E)
  actions, exposed by `GameInput.OnInteract1Performed` / `OnInteract2Performed`, with **no
  subscribers** today. On-screen controls can bind to them.
- Interaction alternates automatically today: tapping the same `IHasProgress` target while it is
  mid-progress sends `alternate = true` (e.g. cutting), otherwise a normal interact
  (`PlayerStateMachine.cs` ~197–217).
- World-space HUD (`LookAtCamera`, `ProgressBarUI`, `BurnWarningUI`, `CookingStartButtonWorldUI`)
  and `Window_QuestPointer` read the cached main camera, which stays the single Unity `Camera`
  under the `CinemachineBrain`, so they keep working but will now face a perspective camera.

## Design

### Cameras (all local, none networked)

| Camera | Lives on | Follows / looks at | Live when |
|---|---|---|---|
| **Walk** | `GameScene` | local player's avatar, set when the local avatar spawns (owner only) | default in play |
| **Close-up** | one shared camera in `GameScene`, low priority until used | placed by script each time it is used (see "Close-up framing"), looking at the active tool | the local player is "at" a cooking tool |
| **Build** | `GameScene` (today's camera, repurposed) | free; keeps today's pan / zoom / clamp | `OnBuildingStart` → `OnBuildingEnd` |

- Walk camera: perspective, behind and above the character; the player turns it by dragging (see
  input). It must not clip through restaurant walls (`GridWall`) — use Cinemachine's
  occlusion handling or an equivalent.
- Nothing added for the close-up may carry a `NetworkObject` or change a tool's network prefab
  registration.

### Close-up framing

Players rotate tools in build mode (4 directions) and existing saves have tools facing every way, so
the close-up cannot be fixed to a tool's front: in some kitchens it would sit inside a `GridWall` or
behind a counter row. Instead:

- Each cooking tool carries a small, non-networked **framing component** with per-tool values:
  distance, height, pitch, lens (FOV) and a look-at point. Tools without it fall back to shared
  defaults. (This replaces the earlier idea of a camera child on each prefab, which also only would
  have reached the 6 tools that nest `_BaseCookingTool`.)
- When a close-up starts, the script places the shared close-up camera on the **side of the tool
  the local player is standing on** (direction tool → player, on the ground plane), at the tool's
  height/distance/pitch, then lets Cinemachine blend to it.
- The camera sits **above the local avatar's head**, steep enough that the player's own body is not
  between the camera and the tool. If it still occludes, the local avatar is faded the same way as
  other players.
- Before using a position, check line of sight from it to the look-at point against walls and
  counters. If blocked, try the tool's other sides in order of closeness to the player's side; if
  all are blocked, use a straight top-down close-up over the tool.
- The position is chosen once when the close-up starts; it does not follow the player around.
- Priority switching lives in one small local component (working name `CameraModeController`) with
  three modes: `Walk`, `CloseUp(tool)`, `Build`. `Build` wins over everything; entering build mode
  from a close-up exits the close-up first.

### Choosing the target (Interact button)

- Tapping objects in the world no longer interacts (in play mode; build mode keeps its own
  touch handling for the building ghost).
- The **target** is one interactable among those already in range (`Context.Highlightable`): the
  one most directly in front of the character, ties broken by distance. A `CookingTool` sitting on a counter
  resolves to its `CookingToolCounterController` as today (`FindControllerForTool`).
- Only the current target is highlighted (today every nearby interactable is), so the player can
  see what the button will act on.
- Pressing **Interact** sends today's `InteractServerRpc` for the target, keeping today's automatic
  choice of normal vs alternate interaction for `IHasProgress` targets (one button, no separate
  "alternate" button).
- The on-screen Interact button binds to the existing `Interact1` action, so keyboard F works the
  same on desktop. The Interact button is hidden in build mode.

### Entering a close-up

- Trigger: on the **local owner's client**, when Interact is pressed and the target is a cooking
  tool — a `CookingTool`, or a `CookingToolCounterController` whose `HasToolInstalled()` is true —
  i.e. the same moment `InteractServerRpc` is sent. The interaction itself still goes to the server
  unchanged; the zoom does not wait for the server's answer.
- While in a close-up, the target is locked to that tool: Interact keeps acting on it (put in, take
  out, cut), even if another interactable is also in range.

### Leaving a close-up

- The **Exit/Back** button returns to `Walk`. The joystick does nothing during a close-up and does
  not exit it.
- Also returns to `Walk` if the tool despawns (picked up in build mode, destroyed) or build mode
  starts.

### Input

- **Joystick** moves relative to the Walk camera's yaw (camera forward projected on the ground).
  Ignored in `CloseUp`. In `Build` it behaves as today.
- **Look:** a drag anywhere on the screen that is not over UI (joystick, buttons) turns the Walk
  camera. Since taps no longer interact, there is no tap-vs-drag conflict. Desktop equivalent:
  right-mouse drag (builder may pick another binding).
- **Pinch zoom** applies only in `Build`. The 90° **rotate** HUD button is shown only in `Build`.

### Other players during a close-up

Other players' avatars that sit between the close-up camera and its tool fade out (or hide) on the
local client only, and come back when the close-up ends.

### UI

- An **Interact** button, visible in `Walk` and `CloseUp`.
- An **Exit/Back** button, visible only in `CloseUp`.
- The tool's existing world HUD (progress bar, burn warning, Blender start button, option menu)
  must stay readable and tappable in the close-up framing.

## Constraints

- Unity 6000.6, URP, Cinemachine package `com.unity.cinemachine` 6.6.0 (CM3 API:
  `CinemachineCamera`), Input System 1.20, Netcode for GameObjects 2.13. Android, landscape only.
- Host-authoritative networking stays exactly as described in `docs/dataflow.md`; no new RPCs.
- Of the 13 cooking-tool prefabs, only 6 nest `_BaseCookingTool.prefab` (Pan, Pot, Oven, Fryer,
  Blender, CuttingBoard). The other 7 (Coffee, Soda, IceCream, Pancake, Grill, Kettle, Teapot) carry
  their own `CookingTool`. A camera added to the base therefore reaches only 6; the other 7 need it
  added one by one (or via a runtime component). Do not re-nest them: that risks changing their
  network prefab registration.
- The game is locked to landscape (`defaultScreenOrientation: 3`); framing only has to work in
  landscape.
- `CookingToolCounterController` also sits on `ClearCounter` and `StoveCounter` (a counter that can
  hold a tool). The zoom trigger must require an installed tool (`HasToolInstalled()`), not just the
  controller type, or plain counters will zoom.
- Existing EditMode tests (`Assets/Tests`, 49 tests at last count) must stay green.

## Left to the builder

- Exact Walk camera offsets, damping, FOV, look sensitivity and the drag
  movement threshold.
- Blend times and curves (aim: quick, under ~0.5 s, so fridge ↔ tool trips stay snappy).
- Per-tool close-up framing values, and the shared defaults.
- The exact line-of-sight test (raycast vs sphere cast, which layers count as blockers).
- Whether the Build camera stays orthographic or becomes a perspective top-down view. Note that
  Cinemachine cannot smoothly blend orthographic ↔ perspective; a perspective top-down view gives a
  smooth build-mode transition, at the cost of reworking `HandleZoom` to FOV/height.
- How other players are faded (material alpha, layer culling, renderer toggle).
- Removing the leftover `ThirdPersonFollow`/`RotationComposer` components and dead
  `CameraController` code once replaced.
- Checking tutorial hints (`Assets/Scripts/Tutorial`) that point at screen positions or assume the
  top-down view, and adjusting them.
