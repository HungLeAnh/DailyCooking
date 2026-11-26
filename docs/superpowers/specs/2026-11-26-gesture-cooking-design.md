# Gesture cooking in the station close-up — design

Date: 2026-11-26 · Status: draft for owner review
Builds on: `docs/superpowers/specs/2026-11-25-third-person-camera-design.md` (merged into `CookingTool`).

## Why

The owner, in their words: cooking is "just press and wait, there is no real handling of the
food". The previous project made the camera glide into a close-up of a cooking tool, but inside it
the player still presses USE and watches a progress bar. This project gives the player real
hands-on actions with the food in that close-up.

Second of three projects (camera → **cooking** → optimization).

## Goals

1. **Drag in:** in a close-up, the player drags the item they are holding into the tool instead of
   pressing USE.
2. **Gestures while cooking:** each tool family has one finger gesture (table below).
3. **Drag out:** the player drags the finished food out of the tool onto the plate/bowl/cup they are
   holding (or into their hand when empty-handed, as USE does today).
4. **Quality:** every gesture step scores 1–3 stars. Stars travel with the ingredient to the plate;
   a served dish pays a tip by its average stars.
5. All 13 cooking tools get their family's gesture.

## Non-goals

- No change to which recipes exist, their ingredients, or their timers (all in `RecipeDatabaseSO`
  and the recipe ScriptableObjects).
- No change to the walking view or the Back button. (The USE button's behaviour at cooking tools
  **does** change — see decision 6.)
- Gestures are not required for food to cook (except cutting, which already only advances by
  player action).
- No physics-driven food.
- No new art pass: gesture prompts, zones and stars may use simple placeholder UI.

## Anti-goals (failure even if it technically works)

- Food that stops cooking when the player walks away. The owner chose: food keeps cooking on its
  own so the player can leave a burger on the pan and go cut tomatoes.
- A gesture that feels "right" to the player but is scored wrong because of network lag.
- Losing a dish because a gesture was missed. A missed gesture only lowers stars. **Burning stays**
  exactly as today (owner: "vẫn cho cháy") — that is the only way to ruin food.
- A step that cannot be done on desktop: every gesture also works with the mouse (mouse drag =
  finger drag), since USE/F no longer cooks.

## Decided by the owner

1. Both drag-and-drop (in and out) **and** gestures while cooking.
2. Gestures affect **quality**, not whether food cooks; burning stays.
3. Five gesture families covering all 13 tools (table below).
4. The proposal for the 5 gestures, the 1–3 star scale and tip 0% / +10% / +25%: approved as written.
5. **Approach A**: the player's device runs and scores the gesture, the server validates and owns
   the stars.
6. **USE at a cooking tool only opens the close-up; it is never used for cooking** (owner: "nút USE
   chỉ để lướt vào cận cảnh chứ không dùng để nấu"). In the walking view, USE (and F) on a tool with
   a cooking tool installed glides into the close-up and sends nothing to the server. Inside the
   close-up every action — putting in, cutting, flipping, stirring, pouring, taking out — is done by
   drag or gesture. The USE button is hidden in the close-up (only BACK remains). USE on everything
   else (fridge, plain counter, plates, customers, trash) is unchanged.
7. **Stars count only gesture steps.** An ingredient that went through no gesture step (bun, raw
   hot dog, lettuce from the fridge) is left out of the dish average. A dish with no gesture step at
   all pays no quality tip. Chained steps average: cut 3★ then fried 1★ → fries 2★.

## Approaches considered

| | Approach | Trade-off | Outcome |
|---|---|---|---|
| **A** | Gestures run locally on the player's client (input, prompts, zones, swipe/circle detection). At the end of each step the client sends one RPC with the measured result (e.g. "flipped at 52 % of the cook time"). The server checks sender, range, tool state, then computes the stars itself from that number and stores them on the food. | No lag in timing gestures. A modified client could report perfect timing to earn tips — accepted, same class of risk as the client-reported rewarded ads today. | **Chosen.** |
| B | Client streams raw finger samples; the server scores. | Cheat-resistant, but 100–200 ms latency shifts timing windows and fill lines; much more traffic. | Rejected. |
| C | Flat screen-space mini-games over the close-up. | Easiest, but it is "pressing UI", not handling food — the thing the owner wants gone. | Rejected. |

## Current state the builder needs to know

- **Tools:** `Counters/CookingTool.cs` (≈900 lines) drives every tool, configured by
  `SO/CookingToolConfigSO` (`toolTypes`, `maxFoodSlots`, `requiresManualStart`, `commitsInputs`,
  `hidesContents`, `outputVessel`, `supportsBurning`, `canBoilWater`, `supportsOptionMenu`). Server
  state: `_netState` (Idle/Cooking/Cooked/Burned), `_netCookingTimer`, `_netBurningTimer`. Recipes are
  resolved by `Counters/CookingTool/RecipeResolver.cs` from `RecipeDatabaseSO`. Timers run on the
  server in `AdvanceCooking()`; `ConsumeInputsAndSpawnOutput` destroys the inputs and **spawns a new
  KitchenObject** for the output.
- **Cutting:** `CookingTool.Cut(actor)` (server) adds `CuttingSpeed` to the timer per call; reached
  through `CookingToolCounterController.InteractAlternateEvent`. It is the only step that advances
  only by player action.
- **Manual start:** Blender, Ice-cream machine and Pot (`requiresManualStart`) wait for
  `StartCooking()`, reached through `PlayerStateMachine.StartCookingServerRpc` from the world-space
  start button (`UI/CookingStartButtonWorldUI.cs`).
- **Vessel:** only the Teapot has an `outputVessel` (EmptyCup, a plain KitchenObject) and keeps the
  tea until the player brings it (`TryCollectWithVessel`). Kettle, CoffeeMachine, SodaMachine, Blender
  and IceCreamMachine have none; their drinks go to the hand or onto the `cup` tableware
  (`Prefab/KitchenObjects/cup.prefab`, a TablewareKitchenObject). These are two different "cups".
- **Inputs per tool:** CoffeeMachine, SodaMachine and Kettle take one input (`maxFoodSlots: 1`).
  The Pot is `DeepFry + Combine` without ingredient restriction, so it also runs single-input
  deep-fry recipes (fries, fried chicken, bacon, corndog) — all under STIR.
- **Two output spawn sites:** `ConsumeInputsAndSpawnOutput` (timed tools) and `CookingTool.Cut()`
  (≈lines 462–463, destroys the input and spawns the cut output). Both must set the output's stars.
- **Interact enters the close-up:** today `PlayerStateMachine.PressInteract()` sends
  `InteractServerRpc` and, for a cooking tool in Walk mode, also calls
  `CameraModeController.RequestCloseUp`. Decision 6 removes the RPC for cooking tools.
- **Plates:** `KitchenObject/TablewareKitchenObject.cs` stores ingredients only as a
  `NetworkList<FixedString64Bytes>` of SO GUIDs (`TryAddIngredient(KitchenObjectSO)`); the added
  ingredient object is destroyed. Callers: `ClearCounterController.cs:18,27`,
  `CookingToolCounterController.cs:158`.
- **Payment:** `Bot/BotCustomerController.FinishEating()` (server) sums `FoodSO.price` and `exp`
  of `waitingFood`, adds the existing **wait-time tip** `tipPercentage` (base `GameDefine.TIP_PERCENTAGE`
  + upgrade, reduced while the customer waits), and hands the total to the table seat; the player
  collects it from the eaten plate (`TablewareKitchenObject.InteractEvent` → `CollectCash`).
- **Close-up:** `Camera/CameraModeController` (`Mode`, `CloseUpTool`, `OnModeChanged`) — gestures are
  only active for the **local** player while `Mode == CloseUp` on that tool.
- **Input:** `Player/GameInput.cs` (touch via EnhancedTouch, `IsScreenPointOverUI`, `OnLookDelta`).
  The look-drag is ignored outside Walk mode, so close-up touches are free for gestures.
- **Interaction:** `PlayerStateMachine.PressInteract()` → `InteractServerRpc(target, alternate)`;
  the server checks owner and `IsInInteractRange`.

## Design

### Quality model

- Each `KitchenObject` carries a server-written quality record (read by all): the **sum and count of
  gesture-step stars** in its history (count 0 = "no gesture step", e.g. raw from the fridge). Its
  star value is the rounded average (1–3) when count > 0.
- A gesture step scores **1–3★**. A step whose gesture moment was missed (never flipped, stir prompts
  ignored, never released) still counts, as 1★. Burned output gets one 1★ step.
- When a tool turns inputs into an output, the output's record = the inputs' records added together
  plus this step's stars (so cut 3★ + fried 1★ → average 2★; Blender inputs carry over too).
- Plates store one record per added ingredient alongside the GUID list. Dish stars = rounded average
  over the ingredients with count > 0; ingredients with count 0 are left out. No such ingredient → no
  quality tip.
- **Quality tip** (on top of the existing wait-time tip, added in `FinishEating`): 1★ +0 %,
  2★ +10 %, 3★ +25 % of that dish's `FoodSO.price`. EXP unchanged.
- Stars are shown as small star icons above the food/plate (world-space, readable in the close-up)
  and briefly on the customer's payment.

### Gesture families

| Family | Tools (by `CookingToolConfigSO`) | Gesture | Scoring (server computes from the reported number) |
|---|---|---|---|
| **CUT** | CuttingBoard | Each swipe across the ingredient is one cut (replaces repeated tapping; still the only way cutting advances). | Per swipe: how centred the swipe crosses the ingredient → 1–3; step stars = rounded average of the swipes on that item. |
| **FLIP** | Pan, Grill, PancakeMachine | Around mid-cook a green window appears; flick up on the food to flip. One flip per cook. | Flip time as fraction of cook time: inside the green window 3★, within a wider margin 2★, never flipped 1★. Burning unchanged. |
| **STIR** | Pot | Every few seconds of cooking a stir prompt appears; draw a circle on the pot within its window. | Fraction of prompts answered → 3★ (all), 2★ (≥ half), 1★ (less). |
| **POUR / HOLD** | Kettle, Teapot, CoffeeMachine, SodaMachine, Blender, IceCreamMachine | *Self-starting tools* (Kettle, Teapot, Coffee, Soda): cooking starts on input as today; during the cook a fill level appears and the player holds to fill and releases in the green band — a timing mini-game on top of the unchanged recipe timer, like FLIP. *Manual-start tools* (Blender, IceCream): pressing down on the tool is the start (sends today's `StartCooking`); the level follows the recipe timer; releasing in the band scores; releasing early does **not** stop the cook (food keeps cooking); holding past the band = "overflow". The world start button is removed: the hold is the only way to start these two. | Release level: inside band 3★, under it 2★, overflow or never released 1★. |
| **WATCH** | Oven, Fryer | Cooking runs as today; a needle moves through a golden zone; drag the food out while the needle is in it. | Removed in golden zone 3★, after "cooked" but outside the zone 2★, burned as today. The golden zone sits after "cooked". |

Exact window sizes, thresholds and durations are tuning values (see "Left to the builder").

### Drag in / drag out (local input, existing server paths)

- In a close-up, the held item appears as an icon in a HUD slot. Dragging it and releasing over the
  tool sends today's `InteractServerRpc` for that tool (same server rules as USE). Releasing
  anywhere else cancels.
- Dragging the finished food out of the tool and releasing over the held-item slot (or the plate
  icon there) sends the same `InteractServerRpc` that USE sends today to take/transfer it.
- USE is hidden in the close-up; drag is the only way in and out. When the player holds nothing,
  dragging the food out puts it in their hand (today's take-out rule).
- Putting in and taking out still go through the server's existing rules (`InteractServerRpc` on the
  tool's counter controller, or a dedicated drop RPC with the same checks); the walking-view USE on a
  cooking tool sends no RPC at all.

### Networking (Approach A)

- Gesture input, prompts, zones and feedback run only on the local owner's client in a close-up.
- One new server RPC per completed step, on `PlayerStateMachine` (owner-checked, range-checked like
  `InteractServerRpc`), carrying: the target tool, the gesture family and the measured number(s)
  (e.g. flip time fraction, release level, stir prompts answered, swipe centredness).
- The server ignores a report that does not match the tool's current state (wrong family, tool not
  cooking, step already reported) and computes stars itself with the same pure scoring functions.
- Other players see the effect (food flips, level fills, stars) through existing/added network
  state; they do not see the gesturing player's finger.

### Code shape (builder's guide, not binding)

Follow the camera project's pattern: the decisions are pure C# classes with EditMode tests
(scoring per family, star averaging, tip calculation, gesture recognisers fed with touch samples);
MonoBehaviours only wire touches, visuals and RPCs.

## Constraints

- Unity 6000.6, URP, Netcode for GameObjects 2.13, Input System 1.20; Android, landscape only.
- Host-authoritative: every change to game state (food, stars, money) is decided on the server.
- No change to network prefab registration (`DefaultNetworkPrefabs.asset`) beyond adding
  NetworkVariables to existing networked components.
- Saves: in-flight food is not saved today; plates/food in the world are not persisted beyond what
  the grid stores. If any star data must persist, `GameData.SaveVersion` + `Migrate()` must be bumped.
- Existing EditMode suite (159 tests) stays green.

## Left to the builder

- Window sizes, thresholds, prompt intervals, swipe/circle recognition tolerances.
- Visual style of prompts, zones, level band, needle and stars (placeholder UI acceptable).
- Where in the close-up the held-item slot sits (must not collide with BACK/USE).
- How the flip/level/needle visuals are synchronised to other players (NetworkVariable vs. one-shot
  RPC), as long as stars stay server-owned.
- Whether the Pot's option menu stays as today in front of the stir gesture (it should).
