# Smooth frame rate on phones — design

Date: 2026-11-26 · Status: draft for owner review
Third project of three (camera → cooking → **optimization**).

## Why

The owner wants the game to stop stuttering and dropping frames while playing ("giật / tụt FPS
khi chơi"). They chose a 60 FPS target that lowers itself on weak phones (approach A), and a Settings
choice: **Graphics = Auto** picks a tier by device and lowers itself when the game stutters;
choosing Low/Medium/High by hand keeps that level fixed ("đổi auto thành tự hạ").

## What was measured (Editor, PC, 2026-11-26)

Busy kitchen: 13 cooking tools (8 cooking), 4 tables, 9 customers, 25 dishes on the menu,
host only. 15 s of Profiler data (1,697 frames):

- **Game code is cheap on PC:** < 0.5 ms/frame of script time. No gameplay hitch; the only long
  frames were the profiler starting and stopping.
- **GC:** about 1.4 KB/frame (≈1.1 KB of it IMGUI `GUI.Repaint`, likely Editor-only); incremental
  GC is already on.
- **Rendering:** about 168k triangles, 156k vertices, 239 shadow casters, 78 visible skinned
  meshes (≈8 customization parts per character), 33 SetPass calls.
- **Settings — the likely cause on phones:**
  - Quality levels Very Low … Ultra use `Assets/URPDefaultResources/*.asset`; the current level is
    **Ultra** (4096 shadow map, 150 m shadow distance, soft shadows, depth texture).
  - **No Android default quality level is set** (`m_PerPlatformDefaultQuality` only lists Switch 2),
    so Android builds risk running Ultra.
  - **`Application.targetFrameRate` is never set**, so Android runs at its default 30 FPS cap.
  - GPU skinning is on, SRP Batcher is on.
  - `Assets/Settings/Mobile_RPAsset.asset` / `Mobile_Renderer.asset` exist but no quality level uses them.

On-device numbers are not available: APK builds are blocked (missing keystore password and
Android SDK platform 35). Everything below must be re-checked on a phone once builds work.

## Goals

1. Android targets **60 FPS** by default.
2. Three Android graphics tiers — **Mobile Low / Mobile Medium / Mobile High** — tuned for phones.
3. **At launch** the game picks a tier from the device (RAM, GPU, CPU cores).
4. **In Auto, while playing**, if the game cannot hold 60 FPS for several seconds it drops one
   tier; already on Mobile Low and still not holding it, it switches to a **30 FPS** cap (only if
   the player has not chosen an FPS). It never raises quality within a session.
5. Small props (food, plates, cups, small kitchen objects) **do not cast shadows on Mobile Low and
   Mobile Medium** (switched at spawn and when the tier changes; Mobile High and PC keep them).
6. Remove the per-frame garbage our own code makes (see "GC cleanup").
7. **A downgrade is remembered** across launches, with a retry rule (owner decision 2).
8. **Safety valve:** the governor can be switched off without a code change; every tier choice and
   governor decision is logged (`[Perf] ...`).

## Decided by the owner

1. 60 FPS target that lowers itself on weak phones; approach A.
2. **Remember a downgrade, retry later:** the next launch starts at the lowered tier/frame rate;
   after 3 sessions in a row without a downgrade, it tries one step higher (never above the tier
   the device would get at launch).
3. **Work and check in the Editor + safety switch:** no APK in this project (builds are blocked);
   add logging and an off switch; on-device tuning happens once builds work.
4. **Player settings:** the in-game Settings popup gets two rows — **Graphics [Auto | Low |
   Medium | High]** and **FPS [30 | 60]**. **Auto = device pick + automatic lowering**; a manual
   level is fixed (no lowering). Saved for next time. (The owner briefly dropped the automatic
   lowering, then restored it as the Auto mode.)

## Non-goals

- No change to PC/Editor quality levels or what the Editor shows by default.
- No new art (no LOD meshes, no texture re-import pass, no character mesh merging) in this project.
- No Unity Adaptive Performance package.

## Anti-goals

- In Auto: tiers flickering up and down; dropping quality for a one-off hitch (loading, scene
  change, join snapshot); a phone that could hold 60 stuck at 30 by a too-eager rule.
- A manual Graphics/FPS choice being changed by the game.
- Breaking the look on PC/Editor or for screenshots.

## Approaches considered

| | Approach | Trade-off | Outcome |
|---|---|---|---|
| **A** | 3 Android tiers + device-based pick at launch + runtime downgrade governor + 30 FPS fallback; props stop casting shadows; GC cleanup. | Best result with no new package; device thresholds are estimates until measured on phones. | **Chosen**; the governor runs only in Graphics = Auto. |
| B | One fixed tuned tier for Android, 60 FPS, no runtime adaptation. | Simple, low risk; weak phones still drop, strong phones look worse than needed. | Rejected. |
| C | Unity Adaptive Performance (the built-in module is already in the manifest; it needs a provider). | Better thermal reaction, but vendor-dependent and untestable without devices. | Rejected. |

## Design

### Tiers (new quality levels, Android only)

Three new quality levels, each with its own URP asset copied from `Mobile_RPAsset` and its own
renderer (copy of `Mobile_Renderer`, keeping any renderer features the game uses). Starting values
(tunable):

| Tier | Render scale | MSAA | Main light shadows | Shadow res | Shadow distance | Soft shadows | Depth texture | HDR |
|---|---|---|---|---|---|---|---|---|
| Mobile Low | 0.75 | off | off | — | — | off | off | off |
| Mobile Medium | 0.85 | off | on | 1024 | 20 m | off | off | off |
| Mobile High | 1.0 | 2× | on | 2048 | 30 m | off | off | off |

- No level is excluded from any platform (the Editor builds for Android, and excluding Android from
  the old levels would change what the Editor shows). Android's **default** quality level =
  **Mobile Medium** (fallback if detection fails). The device pick only runs in real Android
  players (`UNITY_ANDROID && !UNITY_EDITOR`), so the Editor stays on its current level (Ultra)
  unless a level is chosen in Settings.
- Depth texture stays off unless a used effect needs it (builder: check the outline/volume effects
  in `DefaultVolumeProfile` and the renderer features before turning it off).

### Picking a tier at launch

A pure function `DeviceTierPicker.Pick(systemMemoryMB, graphicsMemoryMB, processorCount)` →
tier. Starting thresholds (estimates, to be tuned on devices):
- High: RAM ≥ 6 GB and graphics memory ≥ 2 GB and ≥ 8 cores.
- Low: RAM < 3 GB or < 6 cores.
- otherwise Medium.

Applied once at app start on Android when Graphics = Auto (`RuntimeInitializeOnLoadMethod`),
together with the chosen frame rate (default 60) and `QualitySettings.vSyncCount = 0`. In the
Editor, Auto leaves the current level (Ultra) alone; choosing a level in Settings applies it.

### Runtime governor (Graphics = Auto only)

A pure class `FrameRateGovernor` fed with unscaled frame times:
- Measures in windows of 5 s. A window "fails" when fewer than 85 % of its frames reach the target
  (frame time ≤ 1/60 s × 1.15).
- After **2 failed windows in a row**: drop one tier (Mobile High → Medium → Low); after a drop,
  ignore the next window (the new settings settle).
- On Mobile Low, after 2 more failed windows: switch to **30 FPS** and stop — unless the player
  chose an FPS in Settings, then it just stops.
- Never raises the tier or the frame rate within the session.
- **Ignores** loading, scene changes, pause and background: the driver resets the window on scene
  load, on `OnApplicationPause`/`OnApplicationFocus(false)`, and for the first 5 s after a scene loads.

Runs only when Graphics = Auto, the kill switch is on, and auto is allowed (Android player, or
forced in the Editor for testing). Each change is logged (`[Perf] tier → Mobile Low`).

### Remembering a downgrade (Auto only)

Stored in `PlayerPrefs` (device-local): the lowest tier and frame rate reached, and a count of
consecutive sessions without a downgrade. At launch in Auto: start at min(device pick, remembered
tier) and the remembered frame rate. A session without a downgrade increments the count (counted
at the next launch); at 3, raise one step (30 FPS → 60 FPS at the same tier first, then one tier
up, never above the device pick) and reset the count. Any downgrade resets the count to 0.

### Props stop casting shadows (Mobile Low / Medium only)

At runtime, not in the prefabs: when a kitchen object spawns (and when the tier changes), its
renderers get `shadowCastingMode = Off` if the current tier is Mobile Low or Mobile Medium, and
keep their authored mode otherwise. "Kitchen object" = anything with a `KitchenObject` component
(everything spawned through `KitchenObjectSO.prefab`: food, ingredients, plates, bowls, cups).
Counters, tools, tables, walls and characters keep casting shadows. PC/Editor look is unchanged.

### Player settings (Settings popup)

`UI/UIPopup/Popups/OptionsPopup/UISettingsPopup.cs` (today: sound and music sliders, Guide, Quit)
gets two selector rows under the sliders, in the popup's existing style (`<` value `>`):

```
SETTINGS
 Sound     [=====----]
 Music     [===------]
 Graphics  < Auto >      Auto / Low / Medium / High
 FPS       < 60 >        30 / 60
 [Guide]   [Quit]
```

- Labels are English like the rest of the game UI (builder's call to match existing text style).
- **Graphics = Auto** (default): device pick at launch + remembered downgrade + runtime governor.
- **Graphics = Low/Medium/High:** that Mobile tier is applied now and at every launch; no governor;
  the remembered-downgrade state is ignored (kept, not deleted).
- **FPS:** until the player touches it, the row shows the current frame rate and Auto may fall back
  to 30. Once the player picks 30 or 60 it is fixed: the governor never changes the frame rate
  (it may still lower the tier in Auto).
- Changes apply immediately (no restart) and are stored in `PlayerPrefs` (device-local).
- These rows also work in the Editor (choosing a level applies that Mobile tier), which doubles as
  the Editor test path for the tiers.

### Safety valve and logging

- `PlayerPrefs` key `perf.governor.enabled` (default on) turns the governor off (developer toggle,
  no player UI).
- Log at launch and on every Settings change: device numbers, device pick, remembered tier, chosen
  mode, applied tier and frame rate; log every governor decision (`[Perf] ...`).

### GC cleanup

- `QualityStarsOverlay` and `CloseUpOccluderHider` iterate `KitchenObject.Spawned` /
  `PlayerStateMachine.All` by index (these are exposed as `IReadOnlyList<T>`; `foreach` over the
  interface allocates an enumerator every frame).
- `CloseUpOccluderHider` calls `held.GetComponentsInChildren<Renderer>()` every frame while a
  hidden avatar holds food: cache it per held object instead.
- (The `foreach` over `hidden.Keys` does not allocate and stays.)

## Constraints

- Unity 6000.6, URP 17.6, Android only target; IL2CPP as today.
- Existing EditMode suite (198 tests) stays green.
- Editor/PC: unchanged (Ultra); nothing excluded from any platform.

## Left to the builder

- Exact tier values and device thresholds (starting values above).
- Whether `Mobile_RPAsset`'s other settings (e.g. additional lights, LUT size) need adjusting.
- Which prefabs count as "small props" beyond everything referenced by `KitchenObjectSO.prefab`.
