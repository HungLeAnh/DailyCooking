# Smooth Frame Rate on Phones Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Android runs on three mobile graphics tiers at 60 FPS; Settings offers Graphics [Auto | Low | Medium | High] and FPS [30 | 60]; Auto picks a tier per device and lowers itself (tier, then 30 FPS) when the game stutters, remembering that across launches with a retry; manual levels stay fixed.

**Architecture:** Pure C# decisions with EditMode tests (`DeviceTierPicker`, `FrameRateGovernor`, `PerfMemory`, `GraphicsPreferencesRules`). One persistent `MobilePerformanceManager` loads PlayerPrefs, applies tier/frame rate, drives the governor in Auto and raises `OnTierChanged`; kitchen objects switch their shadows from it; the Settings popup edits the preferences.

**Tech Stack:** Unity 6000.6, URP 17.6, Android/IL2CPP, NUnit EditMode, Unity MCP.

**Spec:** `docs/superpowers/specs/2026-11-26-mobile-framerate-design.md`

## Global Constraints

- Paths relative to `DailyCooking/Assets/`. Settings/assets/prefabs/scenes changed through the Editor or Unity MCP, never hand-edited YAML.
- Tier names exactly: `Mobile Low`, `Mobile Medium`, `Mobile High`. Android default quality = `Mobile Medium`. No level excluded from any platform; the Editor's current level stays `Ultra`.
- Tier values: Low = renderScale 0.75, MSAA off, main shadows off; Medium = 0.85, MSAA off, shadows 1024, distance 20; High = 1.0, MSAA 2×, shadows 2048, distance 30. All: soft shadows off, HDR off, depth texture off unless an effect in use needs it.
- Device pick: High if RAM ≥ 6144 MB and graphics memory ≥ 2048 MB and cores ≥ 8; Low if RAM < 3072 MB or cores < 6; else Medium.
- **Auto** (default) = device pick + remembered downgrade + governor; runs in Android players (`UNITY_ANDROID && !UNITY_EDITOR`) or when forced in the Editor (`MobilePerformanceManager.ForceAutoInEditor`); otherwise Auto leaves the current level. **Low/Medium/High** = that tier everywhere, fixed, no governor.
- Governor: target 60 FPS; window 5 s; frame on target if unscaled dt ≤ (1/60) × 1.15; window fails if < 85 % on target; 2 failed windows in a row → drop one tier; skip the window after a drop; at Mobile Low, 2 more failed windows → 30 FPS (only if FPS not chosen) and stop; never raises in a session; ignore 5 s after a scene load; reset on pause/focus loss.
- Memory (PlayerPrefs, Auto only): start at min(device pick, remembered tier) + remembered FPS; 3 clean sessions in a row → raise one step (30→60 same tier first, then tier +1, never above device pick); any downgrade resets the count.
- FPS row: unchosen → managed (60, Auto may drop to 30); chosen 30/60 → fixed. `QualitySettings.vSyncCount = 0` whenever the frame rate is applied.
- Kill switch PlayerPrefs `perf.governor.enabled` (default 1). Log with prefix `[Perf]`.
- Prop shadows: renderers of `KitchenObject`s Off on Mobile Low/Medium, authored mode otherwise.
- Tests: Unity MCP `run_tests(EditMode)`, baseline 198.

## Review Focus

1. **One-off hitch** (scene load, join snapshot, GC) must not drop quality → `FrameRateGovernorTests.SingleBadWindow_NoDrop`, `ResetWindow_DiscardsPartialWindow` (Task 2).
2. **Remembered tier above today's device pick** never starts above the pick → `PerfMemoryTests.Start_NeverAboveDevicePick` (Task 3).
3. **Retry raising past the device pick or flipping every launch** → `PerfMemoryTests.Raise_StopsAtDevicePickAnd60`, `Downgrade_ResetsCleanCount` (Task 3).
4. **Player chose FPS 60 in Auto** → governor never drops to 30 → `FrameRateGovernorTests.FpsLocked_NeverDropsTo30` (Task 2) + `GraphicsPreferencesTests.ChosenFps_IsLocked` (Task 3).
5. **Corrupt PlayerPrefs** (unknown mode, FPS 45) → Auto, unchosen FPS → `GraphicsPreferencesTests.Load_InvalidValues_FallBack` (Task 3).

---

### Task 1: Mobile tiers and device pick

**Files:** Create `Scripts/Performance/MobileTier.cs`, `Scripts/Performance/DeviceTierPicker.cs`; Test `Tests/DeviceTierPickerTests.cs`

**Interfaces — Produces:** `public enum MobileTier { Low = 0, Medium = 1, High = 2 }`; `public static class MobileTiers { string QualityLevelName(MobileTier t); bool PropShadowsOn(MobileTier t); }`; `public static class DeviceTierPicker { MobileTier Pick(int systemMemoryMB, int graphicsMemoryMB, int processorCount); }`

- [ ] **Step 1: Failing tests:** `Strong_IsHigh` (8192, 4096, 8 → High), `HighNeedsAllThree` (8192, 1024, 8 → Medium), `ZeroGraphicsMemory_NotHigh` (8192, 0, 8 → Medium), `LowRam_IsLow` (2048, 4096, 8 → Low), `FewCores_IsLow` (8192, 4096, 4 → Low), `Typical_IsMedium` (4096, 1024, 8 → Medium), `Names`, `PropShadows_HighOnly`.
- [ ] **Step 2: Run → compile errors.** **Step 3: Implement.** **Step 4: Run EditMode → all pass.**
- [ ] **Step 5: Commit** `feat(perf): mobile tiers and device tier pick`

### Task 2: Frame-rate governor

**Files:** Create `Scripts/Performance/FrameRateGovernor.cs`; Test `Tests/FrameRateGovernorTests.cs`

**Interfaces — Produces:** `public enum GovernorDecision { None, DropTier, DropTo30 }`; `public sealed class FrameRateGovernor`: ctor `(MobileTier startTier, int startFps, bool fpsLocked)`; constants `WindowSeconds = 5f`, `OnTargetRatio = 0.85f`, `Tolerance = 1.15f`, `FailedWindowsToAct = 2`; `MobileTier Tier`, `int TargetFps`, `bool Finished`; `GovernorDecision Feed(float unscaledDt)`; `void ResetWindow()` (drops the partial window and the failed-window streak).

- [ ] **Step 1: Failing tests:** `Smooth60_NeverDrops` (20 s at 1/60 from High); `SingleBadWindow_NoDrop` (5 s at 1/30, 5 s at 1/60); `TwoBadWindows_DropOneTier` (10 s at 1/30 from High → DropTier, Medium); `SkipsWindowAfterDrop` (then 5 s bad → None; 10 more → DropTier to Low); `AtLow_DropsTo30_ThenFinished`; `FpsLocked_NeverDropsTo30` (Low, locked, 20 s bad → never DropTo30, Finished); `ResetWindow_DiscardsPartialWindow` (4 s bad, Reset, 4 s bad, Reset → None); `Tolerance_JustAboveTarget_CountsOnTarget` (dt = 1/60 × 1.1 for 10 s → None).
- [ ] **Step 2: Run → compile errors.** **Step 3: Implement.** **Step 4: Run EditMode → all pass.**
- [ ] **Step 5: Commit** `feat(perf): frame-rate governor`

### Task 3: Remembered downgrade and graphics preferences

**Files:** Create `Scripts/Performance/PerfMemory.cs`, `Scripts/Performance/GraphicsPreferences.cs`; Test `Tests/PerfMemoryTests.cs`, `Tests/GraphicsPreferencesTests.cs`

**Interfaces — Produces:**
- `public struct PerfMemoryState { public int RememberedTier; /* -1 none */ public int RememberedFps; /* 0 none */ public int CleanSessions; }`; `public static class PerfMemory`: `(MobileTier tier, int fps) StartupChoice(MobileTier devicePick, PerfMemoryState s)`; `PerfMemoryState AfterSession(PerfMemoryState s, bool downgraded, MobileTier devicePick)`; `PerfMemoryState AfterDowngrade(PerfMemoryState s, MobileTier tier, int fps)`; `const int CleanSessionsToRetry = 3`.
- `public enum GraphicsMode { Auto = 0, Low = 1, Medium = 2, High = 3 }`; `public struct GraphicsPreferences { public GraphicsMode Mode; public int ChosenFps; /* 0 = not chosen */ }` with `static Default` (Auto, 0); `public static class GraphicsPreferencesRules`: `GraphicsPreferences Sanitize(int storedMode, int storedFps)`; `MobileTier? ManualTier(GraphicsPreferences p)`; `bool GovernorRuns(GraphicsPreferences p, bool killSwitchOn, bool autoAllowed)`; `bool FpsLocked(GraphicsPreferences p)`; `int StartFps(GraphicsPreferences p, int managedFps)`; `GraphicsMode Next(GraphicsMode m)`, `GraphicsMode Previous(GraphicsMode m)`; `int ToggleFps(int fps)`.

- [ ] **Step 1: Failing tests:**
  - PerfMemory: `NoMemory_StartsAtDevicePick60`; `Start_NeverAboveDevicePick`; `RememberedLower_StartsThere` (Low/30, pick High → Low/30); `AfterDowngrade_RemembersAndResets`; `Downgrade_ResetsCleanCount`; `ThreeCleanSessions_Raise30To60First`; `Raise_ThenTierUp` (Low/60 → Medium/60); `Raise_StopsAtDevicePickAnd60` (Medium/60, pick Medium → cleared, RememberedTier -1); `TwoClean_NoRaise`.
  - GraphicsPreferences: `Default_IsAutoUnchosenFps`; `Load_InvalidValues_FallBack` (Sanitize(7, 45) → Auto/0; Sanitize(2, 30) → Medium/30); `Auto_GovernorRuns`; `Manual_GovernorOff`; `KillSwitch_GovernorOff`; `NotAutoAllowed_GovernorOff`; `ChosenFps_IsLocked`; `ManualTier_Maps`; `StartFps_ChosenWins` (30 vs managed 60 → 30; 0 → 60); `NextPrevious_Cycle`; `ToggleFps`.
- [ ] **Step 2: Run → compile errors.** **Step 3: Implement.** **Step 4: Run EditMode → all pass.**
- [ ] **Step 5: Commit** `feat(perf): remembered downgrade and graphics preferences`

### Task 4: Mobile quality levels

**Files:** Create (Editor) `Settings/Mobile_Low_RPAsset.asset`, `Settings/Mobile_Medium_RPAsset.asset`, `Settings/Mobile_High_RPAsset.asset` (copies of `Settings/Mobile_RPAsset.asset`, renderer `Settings/Mobile_Renderer.asset`); Modify `ProjectSettings/QualitySettings.asset`.

- [ ] **Step 1:** Check whether any renderer feature / volume effect in use needs the depth texture; note it in the ledger.
- [ ] **Step 2:** Create the three URP assets with the Global Constraints values; append three levels (non-pipeline settings copied from `Medium`, `customRenderPipeline` = new asset, exact names); Android default = `Mobile Medium`; keep current `Ultra`; save.
- [ ] **Step 3: Verify by script** (names, URP values, Android default, current level, clean console); EditMode passes.
- [ ] **Step 4: Commit** `feat(perf): three mobile quality levels, Android defaults to Mobile Medium`

### Task 5: Performance manager

**Files:** Create `Scripts/Performance/MobilePerformanceManager.cs`, `Scripts/Performance/PerfPrefs.cs` (PlayerPrefs keys and load/save for `PerfMemoryState`, `GraphicsPreferences`, kill switch, "downgraded this session" flag)

**Interfaces:** Consumes Tasks 1–3. Produces `public class MobilePerformanceManager : MonoBehaviour` (`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`, `DontDestroyOnLoad`): `static Instance`, `static bool ForceAutoInEditor`, `GraphicsPreferences Preferences`, `MobileTier? CurrentTier`, `int CurrentFps`, `event Action<MobileTier?> OnTierChanged`, `void SetPreferences(GraphicsPreferences p)` (save + re-apply now; switching to Auto starts a fresh governor at the current state).

- [ ] **Step 1: Implement:** startup — process the last session (`AfterSession` with the stored flag), clear the flag; resolve tier/fps (manual → that tier + `StartFps`; Auto when allowed → `StartupChoice(devicePick, memory)` + `StartFps`; Auto not allowed → leave level, apply fps); apply (`SetQualityLevel(index of name, true)`, `vSyncCount = 0`, `targetFrameRate`); log. Update — when `GovernorRuns`: feed; on decision apply + `AfterDowngrade` + set the flag + log; ignore 5 s after `sceneLoaded`; `ResetWindow` on pause/focus loss.
- [ ] **Step 2: Run EditMode → all pass.**
- [ ] **Step 3: Play Mode (Editor):** default Auto (not forced) → level stays `Ultra`, fps 60; `ForceAutoInEditor` + a fake device pick → `Mobile Medium`; drive bad frames through a test hook → `[Perf]` logs DropTier to `Mobile Low`; restart Play Mode → starts at remembered Low; kill switch off → no governor; manual High → `Mobile High`, no governor; restore prefs afterwards.
- [ ] **Step 4: Commit** `feat(perf): mobile performance manager with governor, memory and kill switch`

### Task 6: Prop shadows per tier

**Files:** Modify `Scripts/KitchenObject/KitchenObject.cs`

**Interfaces:** Consumes `MobilePerformanceManager.CurrentTier`, `OnTierChanged`, `MobileTiers.PropShadowsOn`. Produces `void KitchenObject.ApplyPropShadows(bool on)` (caches renderers and authored `shadowCastingMode` on first call).

- [ ] **Step 1: Implement:** on spawn apply `on = tier == null || PropShadowsOn(tier)`; on `OnTierChanged` re-apply to every `KitchenObject.Spawned` by index; unsubscribe on despawn.
- [ ] **Step 2: Run EditMode → all pass.**
- [ ] **Step 3: Play Mode busy kitchen:** Medium → kitchen-object renderers Off, counters/tools unchanged; newly spawned item Off; High → authored modes back; Shadow Casters Count lower on Medium than High.
- [ ] **Step 4: Commit** `feat(perf): small props stop casting shadows on low and medium tiers`

### Task 7: Settings rows

**Files:** Modify `Scripts/UI/UIPopup/Popups/OptionsPopup/UISettingsPopup.cs`; Modify (Editor) the settings popup prefab (`PopupDatabase` entry `UIPopupType.UISettingPopup`).

**Interfaces:** Consumes `MobilePerformanceManager.Instance` (`Preferences`, `SetPreferences`, `CurrentFps`), `GraphicsPreferencesRules.Next/Previous/ToggleFps`.

- [ ] **Step 1: Implement:** rows `Graphics  [<] Auto [>]` and `FPS  [<] 60 [>]` under the music slider, existing popup style; Graphics arrows cycle; FPS arrows toggle and set `ChosenFps`; FPS shows `CurrentFps` while unchosen; `UpdateVisual` refreshes; null `Instance` → show defaults, no-op.
- [ ] **Step 2: Run EditMode → all pass.**
- [ ] **Step 3: Play Mode:** Settings in MainMenu and GameScene; screenshot; Graphics → High → `Mobile High`; FPS → 30 → targetFrameRate 30; reopen → kept.
- [ ] **Step 4: Commit** `feat(perf): graphics and FPS settings`

### Task 8: GC cleanup and docs

**Files:** Modify `Scripts/UI/QualityStarsOverlay.cs`, `Scripts/Camera/CloseUpOccluderHider.cs`, `docs/project_overview.md`, `docs/optimization-log.md`

- [ ] **Step 1:** Index loops over `KitchenObject.Spawned` / `PlayerStateMachine.All`; cache the held object's renderer array in the hider (refresh only when the held object changes).
- [ ] **Step 2:** Play Mode busy kitchen: `GC Allocated In Frame` not above the ≈1.4 KB/frame baseline with a hidden avatar holding food.
- [ ] **Step 3:** Docs: overview paragraph "Mobile performance"; `optimization-log.md` section with the 2026-11-26 measurements and what remains for on-device testing.
- [ ] **Step 4: Run EditMode → all pass. Commit** `perf: no per-frame garbage in the star overlay and avatar hider; docs`
