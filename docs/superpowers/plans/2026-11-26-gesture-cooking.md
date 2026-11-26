# Gesture Cooking Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** In a cooking-tool close-up the player drags food in and out and cooks with one gesture per tool family (cut, flip, stir, pour, watch); each gesture step scores 1–3 stars that ride with the food to the plate and pay a quality tip.

**Architecture:** Scoring, star bookkeeping, payment and gesture recognition are pure C# classes with EditMode tests. The local owner's client runs the gesture input and prompts in the close-up and reports one number per gesture to the server through a new `PlayerStateMachine.ReportGestureServerRpc`; the server validates it, records it per cook in a `CookStepRecorder`, and writes a `QualityRecord` onto the output `KitchenObject` (NetworkVariable). Plates keep one record per ingredient; `BotCustomerController.FinishEating` adds the quality tip.

**Tech Stack:** Unity 6000.6, Netcode for GameObjects 2.13 (`INetworkSerializable`, `NetworkVariable`, `NetworkList`), Input System 1.20 (EnhancedTouch), NUnit EditMode.

**Spec:** `docs/superpowers/specs/2026-11-26-gesture-cooking-design.md`

## Global Constraints

- Paths below are relative to `DailyCooking/Assets/`. Scene/prefab/asset edits go through the Unity Editor or Unity MCP (`execute_code` with `SerializedObject`/`PrefabUtility`), never hand-edited YAML.
- Host-authoritative: stars, food and money are only written on the server. Clients only report gesture numbers.
- Stars are 1–3 per gesture step; an object's record is (sum, count) of its gesture steps; count 0 = "no gesture step" and is **left out** of the dish average; a dish with no counted ingredient pays **no** quality tip.
- Chained/multi-input: output record = sum of input records + this step's stars. Burned output = that sum + one 1★ step.
- Quality tip on the dish's base price: 1★ +0 %, 2★ +10 %, 3★ +25 %, on top of the existing wait-time tip; EXP unchanged.
- Food keeps cooking without gestures (except cutting); a missed gesture moment scores 1★; burning unchanged.
- USE/F at a cooking tool (counter with an installed tool) only opens the close-up and sends **no** RPC; USE is hidden in the close-up (BACK only). USE elsewhere unchanged.
- Blender and IceCreamMachine start by pressing down on the tool (hold); Blender's world start button is removed. The Pot keeps its world start button.
- Every gesture also works with the mouse.
- Rounding of averages is **round half up** (2.5 → 3), never `Mathf.RoundToInt`.
- Do not change `DefaultNetworkPrefabs.asset` registrations; adding NetworkVariables/NetworkLists to existing networked components is allowed.
- Tests: Unity MCP `run_tests(EditMode)`; current baseline 159 passing.

## Review Focus

1. **Player leaves mid-cook** (never flips/stirs/releases): food cooks normally and the step scores 1★, no errors → `CookStepRecorderTests.NoReports_ScoresOneStar_*` (Task 3).
2. **Two players at one tool** both report a flip/pour: only the first counts; stir prompts count once each → `CookStepRecorderTests.SecondFlip_Ignored`, `StirSamePromptTwice_CountsOnce` (Task 3).
3. **Stale or forged report** (flip fraction far from the server's cook fraction, a stir prompt index from the future): ignored → `CookStepRecorderTests.FlipFarFromServer_Ignored`, `StirFuturePrompt_Ignored` (Task 3).
4. **Dish with only raw ingredients, or containing burned food**: no quality tip / burned counts as a 1★ step → `QualityRulesTests.DishStars_AllRaw_IsZero`, `BurnedChain_AddsOneStar` (Task 1).
5. **Drag released over nothing, BACK or another tool**: no RPC, held item stays in hand → manual check in Task 7.

---

### Task 1: Quality record, dish stars and payment

**Files:**
- Create: `Scripts/KitchenObject/QualityRecord.cs`, `Scripts/KitchenObject/QualityRules.cs`
- Test: `Tests/QualityRulesTests.cs`

**Interfaces:**
- Produces:
  - `public struct QualityRecord : INetworkSerializable, IEquatable<QualityRecord>` with `ushort Sum; byte Count;`, `static QualityRecord None`, `QualityRecord WithStep(int stars)` (stars clamped 1..3), `QualityRecord Plus(QualityRecord other)`, `int Stars` (0 when `Count == 0`, else round-half-up of `Sum/Count`).
  - `public static class QualityRules`: `int DishStars(IEnumerable<QualityRecord> ingredients)` (average over records with `Count > 0`, round half up; 0 if none), `float TipPercent(int stars)` (0→0, 1→0, 2→0.10f, 3→0.25f), `int Payment(int baseCash, float waitTipPercent, int dishStars)` = `baseCash + (int)(baseCash * waitTipPercent) + (int)(baseCash * TipPercent(dishStars))`.

- [ ] **Step 1: Write failing tests** `Tests/QualityRulesTests.cs`:
  - `None_HasZeroStars`: `QualityRecord.None.Stars == 0`.
  - `WithStep_ClampsToOneToThree`: `None.WithStep(5).Stars == 3`, `None.WithStep(0).Stars == 1`.
  - `ChainedSteps_Average`: `None.WithStep(3).WithStep(1).Stars == 2`.
  - `HalfRoundsUp`: `None.WithStep(3).WithStep(2).Stars == 3`.
  - `Plus_AddsSumsAndCounts`: `(3★).Plus(1★)` → `Sum == 4, Count == 2`.
  - `BurnedChain_AddsOneStar`: `None.WithStep(3).WithStep(1)` (burned step) → `Stars == 2`.
  - `DishStars_IgnoresRaw`: `[None, 3★, 3★]` → 3.
  - `DishStars_AllRaw_IsZero`: `[None, None]` → 0.
  - `DishStars_Mixed`: `[1★, 3★, 2★]` → 2.
  - `TipPercent_Values`: 0→0, 1→0, 2→0.10, 3→0.25.
  - `Payment_AddsWaitAndQualityTip`: `Payment(100, 0.2f, 3) == 145`; `Payment(100, 0.2f, 0) == 120`.
- [ ] **Step 2: Run; expect compile errors** (types missing).
- [ ] **Step 3: Implement both files.** `NetworkSerialize` writes `Sum` and `Count`.
- [ ] **Step 4: Run EditMode** → all pass (159 + 11).
- [ ] **Step 5: Commit** `feat(cooking): quality record, dish stars and payment rules`

---

### Task 2: Gesture scoring and recognisers

**Files:**
- Create: `Scripts/Counters/Gestures/GestureFamily.cs`, `Scripts/Counters/Gestures/GestureScoring.cs`, `Scripts/Counters/Gestures/GestureRecognizers.cs`
- Test: `Tests/GestureScoringTests.cs`, `Tests/GestureRecognizersTests.cs`

**Interfaces:**
- Produces:
  - `public enum GestureFamily : byte { None, Cut, Flip, Stir, Pour, Watch }` (append-only; serialized in assets).
  - `public static class GestureScoring` with tuning constants and:
    - `int CutStars(float centredness)`: ≥0.7 → 3, ≥0.35 → 2, else 1.
    - `int FlipStars(float flipFraction)`: `FlipWindowStart = 0.45f`, `FlipWindowEnd = 0.65f`, `FlipMargin = 0.15f`; inside window 3; within margin outside it 2; otherwise or `< 0` (never flipped) 1.
    - `int StirStars(int answered, int expected)`: `expected <= 0` → 3; `answered >= expected` → 3; `answered * 2 >= expected` → 2; else 1. `StirIntervalSeconds = 2f`.
    - `int PourStars(float level)`: `PourBandMin = 0.8f`, `PourBandMax = 0.95f`; in band 3; below 2 (and `>= 0`); above band or `< 0` (never released) 1.
    - `int WatchStars(float burnFraction)`: `WatchZoneStart = 0.1f`, `WatchZoneEnd = 0.5f`; in zone 3; else 2 (burned handled by the burned step).
  - `public static class GestureRecognizers`:
    - `float SwipeCentredness(Vector2 start, Vector2 end, Vector2 centre, float radius)`: −1 if the segment does not pass within `radius` of `centre` or is shorter than `radius`; else `1 - distance(centre, segment) / radius`.
    - `public sealed class StirTracker { void Reset(Vector2 centre); bool Feed(Vector2 position); }` — accumulates signed angle around `centre`; returns true once `|angle| ≥ 300°`, then resets the accumulator.
    - `float PourLevel(float holdSeconds, float fillSeconds)` = `fillSeconds <= 0 ? 1 : holdSeconds / fillSeconds` (not clamped).

- [ ] **Step 1: Write failing tests** with these assertions:
  - Cut: `CutStars(0.9f)==3`, `CutStars(0.5f)==2`, `CutStars(0.1f)==1`.
  - Flip: `FlipStars(0.55f)==3`, `FlipStars(0.35f)==2`, `FlipStars(0.75f)==2`, `FlipStars(0.1f)==1`, `FlipStars(-1f)==1`.
  - Stir: `StirStars(4,4)==3`, `StirStars(2,4)==2`, `StirStars(1,4)==1`, `StirStars(0,0)==3`.
  - Pour: `PourStars(0.9f)==3`, `PourStars(0.5f)==2`, `PourStars(1.2f)==1`, `PourStars(-1f)==1`.
  - Watch: `WatchStars(0.3f)==3`, `WatchStars(0.05f)==2`, `WatchStars(0.7f)==2`.
  - Swipe: through centre `(-50,0)→(50,0)`, centre `0`, radius 40 → 1; offset `(-50,20)→(50,20)` → 0.5; miss `(-50,60)→(50,60)` → −1; too short `(-10,0)→(10,0)` → −1.
  - Stir: feeding 13 points around a circle of radius 50 (0°…360° step 30°) returns true exactly once, at or after the 300° point; feeding back-and-forth ±30° ten times never returns true.
  - Pour: `PourLevel(1,2)==0.5f`, `PourLevel(3,2)==1.5f`, `PourLevel(1,0)==1`.
- [ ] **Step 2: Run; expect compile errors.**
- [ ] **Step 3: Implement the three files.**
- [ ] **Step 4: Run EditMode** → all pass.
- [ ] **Step 5: Commit** `feat(cooking): gesture scoring and recognisers`

---

### Task 3: Per-cook step recorder

**Files:**
- Create: `Scripts/Counters/Gestures/CookStepRecorder.cs`
- Test: `Tests/CookStepRecorderTests.cs`

**Interfaces:**
- Consumes: `GestureFamily`, `GestureScoring` (Task 2).
- Produces: `public sealed class CookStepRecorder` (server-side, one per `CookingTool`):
  - `const float MaxReportDrift = 0.2f;`
  - `void Reset()` — new cook.
  - `bool RecordSwipe(float centredness)` — ignores `< 0`; appends `CutStars`.
  - `bool RecordFlip(float clientFraction, float serverFraction)` — false if already flipped or `|client − server| > MaxReportDrift`.
  - `bool RecordStir(int promptIndex, int currentPromptIndex)` — false if `promptIndex > currentPromptIndex`, `< 0`, or already recorded.
  - `bool RecordPour(float level, float serverLevel)` — false if already released or `|level − serverLevel| > MaxReportDrift`.
  - `int StepStars(GestureFamily family, int expectedStirPrompts)` — Cut: round-half-up average of swipe stars (1 if none); Flip: `FlipStars(recorded or -1)`; Stir: `StirStars(count, expected)`; Pour: `PourStars(recorded or -1)`; Watch/None: 1 (watch is scored at take-out by `GestureScoring.WatchStars`).

- [ ] **Step 1: Write failing tests:**
  - `NoReports_ScoresOneStar_Flip` (`StepStars(Flip,0)==1`), `NoReports_ScoresOneStar_Pour`, `NoReports_ScoresOneStar_Stir` (`StepStars(Stir,3)==1`), `NoReports_Cut_IsOne`.
  - `Flip_InWindow_Three`: `RecordFlip(0.55f,0.5f)` true → `StepStars(Flip,0)==3`.
  - `SecondFlip_Ignored`: second `RecordFlip(0.1f,0.1f)` false; stars still 3.
  - `FlipFarFromServer_Ignored`: `RecordFlip(0.55f,0.9f)` false → stars 1.
  - `StirSamePromptTwice_CountsOnce`: prompts 0,0,1 with current 1 → `StepStars(Stir,4)==2`.
  - `StirFuturePrompt_Ignored`: `RecordStir(3, 1)` false.
  - `Swipes_Average`: swipes 0.9, 0.1 → `StepStars(Cut,0)==2`; a `-1` swipe is ignored.
  - `Pour_InBand_Three` and `SecondPour_Ignored`.
  - `Reset_ClearsEverything`.
- [ ] **Step 2: Run; expect compile errors.**
- [ ] **Step 3: Implement `CookStepRecorder`.**
- [ ] **Step 4: Run EditMode** → all pass.
- [ ] **Step 5: Commit** `feat(cooking): per-cook gesture step recorder`

---

### Task 4: Stars on food, plates and payment (networked)

**Files:**
- Modify: `Scripts/KitchenObject/KitchenObject.cs` (add quality NetworkVariable), `Scripts/KitchenObject/TablewareKitchenObject.cs` (per-ingredient records), `Scripts/MVC/Controllers/CounterControllers/ClearCounterController.cs:18,27`, `Scripts/MVC/Controllers/CounterControllers/CookingToolCounterController.cs:158`, `Scripts/Bot/BotCustomerController.cs` (`FinishEating`)

**Interfaces:**
- Consumes: `QualityRecord`, `QualityRules` (Task 1).
- Produces:
  - `KitchenObject.Quality` (`QualityRecord`, read) and `KitchenObject.SetQuality(QualityRecord)` (server only; ignored on clients).
  - `TablewareKitchenObject.TryAddIngredient(KitchenObjectSO so, QualityRecord quality)` replaces the one-argument overload (all three callers pass the added object's `Quality`); `NetworkList<QualityRecord> ingredientQualities` kept index-aligned with `ingredientGuids`; `int DishStars` → `QualityRules.DishStars(ingredientQualities)`.
  - `FinishEating` computes `baseCash` (sum of prices) and pays `QualityRules.Payment(baseCash, tipPercentage.Value, plate.DishStars)` where `plate` = the `TablewareKitchenObject` on `targetTable` at `TargetSeatIndex.Value` (0 stars if none).

- [ ] **Step 1: Implement** the members above. Clearing a plate (if any path clears `ingredientGuids`) clears `ingredientQualities` too.
- [ ] **Step 2: Compile, run EditMode** → all pass, console has no errors.
- [ ] **Step 3: Play Mode check** (Unity MCP, host): spawn a plate, `TryAddIngredient(so, None.WithStep(3))` and `(so2, None)` on the server → `DishStars == 3` on host; call `FinishEating` path with a seated customer (or `QualityRules.Payment` with the plate's stars) and confirm cash = base + wait tip + 25 %.
- [ ] **Step 4: Commit** `feat(cooking): stars ride on food and plates and pay a quality tip`

---

### Task 5: Server-side gesture steps on cooking tools

**Files:**
- Modify: `Scripts/SO/CookingToolConfigSO.cs` (add `public GestureFamily gestureFamily;`), the 13 assets in `SO/CookingToolConfigSO/`, `Scripts/Counters/CookingTool.cs`, `Scripts/MVC/Controllers/CounterControllers/CookingToolCounterController.cs`, `Scripts/Player/PlayerStateMachine.cs`

**Interfaces:**
- Consumes: `CookStepRecorder`, `GestureScoring`, `GestureFamily` (Tasks 2–3); `KitchenObject.SetQuality/Quality` (Task 4).
- Produces:
  - Asset values: CuttingBoard=Cut; Pan, Grill, PancakeMachine=Flip; Pot=Stir; Kettle, Teapot, CoffeeMachine, SodaMachine, Blender, IceCreamMachine=Pour; Oven, Fryer=Watch.
  - `CookingTool.GestureFamily` (from config), `CookingTool.CookFraction` (cook timer / max, 0..1, readable on clients), `CookingTool.BurnFraction`, `CookingTool.CurrentStirPromptIndex` = `floor(cookTimer / StirIntervalSeconds)`, `CookingTool.ExpectedStirPrompts` = `floor(CookingTimeMax / StirIntervalSeconds)`.
  - `bool CookingTool.ApplyGesture(PlayerStateMachine actor, GestureFamily family, float value)` (server): rejects if `family != GestureFamily` or state doesn't allow it (Cut needs cutting active; Flip/Stir need `Cooking`; Pour needs `Cooking` for self-starting tools, and for manual-start tools a value of `-2` means "press down = start" → `TryStartCooking()`, then later a level); Cut → `RecordSwipe(value)` then `Cut(actor)`; Flip → `RecordFlip(value, CookFraction)` + `FlipFeedbackClientRpc()`; Stir → `RecordStir((int)value, CurrentStirPromptIndex)`; Pour → `RecordPour(value, CookFraction)`.
  - Output stars at both spawn sites: `ConsumeInputsAndSpawnOutput` and `Cut()` set `output.SetQuality(sum of input Quality).WithStep(recorder.StepStars(family, ExpectedStirPrompts))`; burned output = cooked food's quality `.WithStep(1)`. `recorder.Reset()` when a new cook starts and after output.
  - Watch: when food is taken out of a Watch tool in `Cooked` state (the take-out branch of `CookingToolCounterController.HandleCookingInteraction`), the server applies `.WithStep(GestureScoring.WatchStars(BurnFraction))` to that food before it moves.
  - `CookingToolCounterController.ApplyGesture(actor, family, value)` → `_cookingTool.ApplyGesture` (null-safe).
  - `[Rpc(SendTo.Server)] PlayerStateMachine.ReportGestureServerRpc(NetworkBehaviourReference target, GestureFamily family, float value, RpcParams rpcParams = default)` — same owner and `IsInInteractRange` checks as `InteractServerRpc`, then `controller.ApplyGesture(this, family, value)`. Public client helper `PlayerStateMachine.ReportGesture(CookingToolCounterController target, GestureFamily family, float value)` (owner only).

- [ ] **Step 1: Add `gestureFamily`** to `CookingToolConfigSO`; set the 13 assets via `SerializedObject` (values above); verify by reading them back.
- [ ] **Step 2: Implement** the `CookingTool` / controller / RPC members above.
- [ ] **Step 3: Compile; run EditMode** → all pass.
- [ ] **Step 4: Play Mode check** (host, scripted like the camera project: unlock grid, place Stove+Pan, CuttingBoard on a Clear Counter, Oven): 
  - Pan + burger: `ReportGesture(Flip, CookFraction)` at ~0.55 → output quality 3★; a second cook without reports → 1★.
  - CuttingBoard + tomato: swipes 0.9 ×N until cut → slices 3★; burger cut chain not needed.
  - Oven: take out at `BurnFraction` 0.3 → +3★ step.
  - No console errors.
- [ ] **Step 5: Commit** `feat(cooking): server records gesture steps and writes stars on cooked food`

---

### Task 6: USE only opens the close-up; Blender starts by holding

**Files:**
- Modify: `Scripts/Player/PlayerStateMachine.cs` (`PressInteract`), `Scripts/UI/UIHUDManager.cs` (`ApplyCameraModeVisibility`), `Prefab/CookingTool/BlenderCookingTool.prefab` (remove the `CookingStartButtonWorldUI` object)

**Interfaces:**
- Consumes: `CameraModeController` (camera project).
- Produces: `PressInteract()` — when the target is a `CookingToolCounterController` with `HasToolInstalled()`: in Walk mode call `RequestCloseUp(controller.InstalledTool)` and **return without** `RequestInteract`; in CloseUp mode do nothing. Interact element visible only in `Walk`.

- [ ] **Step 1: Implement**; remove the Blender's world start button object from the prefab (Pot keeps its button).
- [ ] **Step 2: Run EditMode** → all pass.
- [ ] **Step 3: Play Mode check:** USE at a Pan while holding a burger → close-up opens, burger still in hand, pan empty, no RPC side effect; USE at fridge/plain counter unchanged; USE hidden in close-up, BACK shown.
- [ ] **Step 4: Commit** `feat(cooking): USE at a cooking tool only opens the close-up`

---

### Task 7: Drag in / drag out in the close-up

**Files:**
- Create: `Scripts/Counters/Gestures/CloseUpPointer.cs` (unified touch/mouse pointer for the close-up), `Scripts/UI/HeldItemSlotUI.cs`
- Modify (Editor): `Scenes/GameScene.unity` — `HeldItemSlot` element under HUD `Bot_Right` area (bottom-left of the close-up, clear of BACK), registered as `UIHUDElements.HeldItem` (appended to the enum), visible only in `CloseUp`

**Interfaces:**
- Produces:
  - `CloseUpPointer` (MonoBehaviour on `Camera Rig`): active only when `CameraModeController.Mode == CloseUp`; events `OnDown(Vector2 screen)`, `OnDrag(Vector2 screen)`, `OnUp(Vector2 screen)` for one pointer (first touch not over UI via `GameInput.IsScreenPointOverUI`, or left mouse) — except a press that starts on the held-item slot, which it reports as `OnSlotDragStart`.
  - `HeldItemSlotUI` shows `PlayerStateMachine.LocalInstance.GetKitchenObject()`'s `KitchenObjectSO.sprite` (empty when nothing held).
  - Drag in: slot drag released over the close-up tool (raycast from `Camera.main` hits the `CloseUpTool` or its counter) → `PlayerStateMachine.LocalInstance` sends today's `InteractServerRpc` for the counter controller (expose `public void RequestInteractFromCloseUp(CookingToolCounterController target)`). Released elsewhere → nothing.
  - Drag out: a drag that starts on food inside the close-up tool (raycast hits a `KitchenObject` parented to the tool) and is released over the held-item slot → same `RequestInteractFromCloseUp`. For Watch tools this is the take-out that Task 5 scores.
  - While dragging, a ghost icon follows the pointer.

- [ ] **Step 1: Implement** `CloseUpPointer`, `HeldItemSlotUI`, the slot UI element and the two drag paths.
- [ ] **Step 2: Run EditMode** → all pass.
- [ ] **Step 3: Play Mode check (host):** in a Pan close-up holding a burger: drag slot→pan puts it in; drag pan food→slot with an empty hand takes it; with a plate in hand, cooked food → slot moves onto the plate; drag released on empty space or on BACK does nothing (Review Focus 5).
- [ ] **Step 4: Commit** `feat(cooking): drag food in and out in the close-up`

---

### Task 8: Family gestures and prompts

**Files:**
- Create: `Scripts/Counters/Gestures/CloseUpGestureController.cs` (on `Camera Rig`), `Scripts/Counters/Gestures/GesturePromptUI.cs` (world-space prompts on the tool)
- Modify: `Prefab/CookingTool/_BaseCookingTool.prefab` and the 7 non-nested tool prefabs (add a `GesturePromptUI` child placed like the existing progress bar)

**Interfaces:**
- Consumes: `CloseUpPointer` (Task 7), `GestureRecognizers`, `GestureScoring` constants (Task 2), `CookingTool.GestureFamily/CookFraction/BurnFraction/CurrentStirPromptIndex/CurrentState/RequiresManualStart` and `PlayerStateMachine.ReportGesture` (Task 5).
- Produces: per family, on the local client in the close-up (pointer gestures that are not drags from the slot or from food):
  - **Cut:** each pointer down→up is a swipe → `SwipeCentredness` against the food's screen centre/radius → `ReportGesture(Cut, c)` when `c >= 0`.
  - **Flip:** prompt bar with the green window `[FlipWindowStart, FlipWindowEnd]` over `CookFraction`; an upward flick on the food (up-move ≥ 8 % of screen height within 0.3 s) → `ReportGesture(Flip, CookFraction)` once per cook.
  - **Stir:** a ring prompt appears while `CurrentStirPromptIndex` is unanswered; `StirTracker.Feed` true → `ReportGesture(Stir, CurrentStirPromptIndex)`.
  - **Pour:** a level bar with the band `[PourBandMin, PourBandMax]`; pointer down on the tool starts the hold (manual-start tool in `Idle` with food → first `ReportGesture(Pour, -2)` to start); level = `PourLevel(holdSeconds, CookingTimeMax)`; pointer up → `ReportGesture(Pour, level)`.
  - **Watch:** a needle over `BurnFraction` with the golden zone `[WatchZoneStart, WatchZoneEnd]` shown once `Cooked`; taking the food out is the Task 7 drag.
  - `GesturePromptUI` hides when the local player is not in this tool's close-up.

- [ ] **Step 1: Implement** the controller and the prompt UI; add the prompt child to the prefabs (via `PrefabUtility`).
- [ ] **Step 2: Run EditMode** → all pass.
- [ ] **Step 3: Play Mode check (host, scripted pointer input through the controller's public test hooks or simulated mouse):** one cook per family ends with the expected stars on the output (good timing → 3★, no gesture → 1★); screenshot each prompt in the close-up.
- [ ] **Step 4: Commit** `feat(cooking): cut, flip, stir, pour and watch gestures in the close-up`

---

### Task 9: Star display, docs

**Files:**
- Create: `Scripts/UI/QualityStarsUI.cs` (world-space, above food and plates)
- Modify: the food/plate prefabs' shared base (`Prefab/KitchenObjects/_Base*` or the KitchenObject prefabs used by `KitchenObjectSO.prefab`) to include it; `docs/project_overview.md`; `docs/dataflow.md` §3/§6

**Interfaces:**
- Consumes: `KitchenObject.Quality`, `TablewareKitchenObject.DishStars` (Task 4).
- Produces: 1–3 small stars over food with `Quality.Count > 0` and over plates with `DishStars > 0`; hidden otherwise; readable in the close-up.

- [ ] **Step 1: Implement** `QualityStarsUI` (subscribe to the NetworkVariable/NetworkList change) and add it to the prefabs.
- [ ] **Step 2: Docs:** overview paragraph "Gesture cooking" (families, stars, tip, the new RPC); `dataflow.md` — `ReportGestureServerRpc` under player interactions, quality tip under economy.
- [ ] **Step 3: Run EditMode** → all pass; Play Mode screenshot of a 3★ burger on a plate.
- [ ] **Step 4: Commit** `feat(cooking): show quality stars on food and plates`
