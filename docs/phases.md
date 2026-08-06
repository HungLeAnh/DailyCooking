# DailyCooking — Content Build Phases

This document records every phase of content building for the DailyCooking game.
All menu items live under the `DailyCooking` menu in the Unity Editor and are
implemented in `Assets/Editor/DailyCookingContentBuilder.cs`.

| Phase | Name | Menu Item |
| --- | --- | --- |
| 1 | Baseline game content | (pre-existing, no builder) |
| 2 | Create KitchenObjects | `DailyCooking/Phase 2 - Create KitchenObjects` |
| 3 | Create Recipes | `DailyCooking/Phase 3 - Create Recipes` |
| 4 | Create Cooking Tools | `DailyCooking/Phase 4 - Create Cooking Tools` |
| 5 | Create Dishes | `DailyCooking/Phase 5 - Create Dishes` |

---

## Phase 1 — Baseline Game Content (pre-existing)

The working multiplayer cooking game before this content batch. Not created by the
builder; it is the starting point Phase 2–5 extend.

**Existing KitchenObjectSOs** (`Assets/SO/KitchenObjectSO/`, 26):

`BowlSO`, `BunSO`, `BurgerCookedSO`, `BurgerTrashSO`, `BurgerUncookedSO`,
`CarrotSlicesSO`, `CarrotSO`, `CheeseSliceSO`, `CheeseSO`, `Cup`,
`LettuceSliceSO`, `LettuceSO`, `OnionSlicesSO`, `OnionSO`, `PlateSO`,
`PotatoChoppedSO`, `PotatoMashedSO`, `PotatoSO`, `StewBurnedSO`, `StewCookedSO`,
`TomatoSliceSO`, `TomatoSlicesSO`, `TomatoSO`, `VegetableBurgerCookedSO`,
`VegetableBurgerUncookedSO`, `RefillBoxSO`

**Existing base dishes** (in `ConfigFood`): `CheeseBurgerSO`, `HamburgerFullSO`,
`OnionSaladSO`, `SaladSO`, `StewSO`, `VegetableHamBurgerFullSO`.

**Existing tableware prefabs**: `plate.prefab`, `bowl.prefab`, `cup.prefab`
(cup accepts Stew; Stew has a `FoodCompleteVisual`).

**Existing recipe database**: Stew cooking; 3 burning recipes (Burger, Stew,
VegetableBurger) — these were already valid and untouched by later phases.

**Existing shop** (`ConfigShop`, 21 items, ids 0–20): counters, tables, walls +
9 ingredient unlocks (Tomatoes, Buns, Carrots, Cheese, Lettuces, Onions, Potatoes,
Burger, Vegetable Burger).

**Existing systems**: counters (StovePan, CuttingBoard, Trash, Delivery, Food
Table, Plates Cabinet, ingredient cabinets, RefillBox), `MenuData`, `PlacedObjectDatabase`.

---

## Phase 2 — Create KitchenObjects

Adds a "fast-food" ingredient batch cloned from the **Ultimate Food Pack collection**
models, as networked `KitchenObject` prefabs with `KitchenObjectSO` assets.

### What it does
- Creates **29 KitchenObjectSOs** (`Assets/SO/KitchenObjectSO/`) + prefabs
  (`Assets/Prefab/KitchenObjects/food_ingredient_*.prefab`):
  Dough, Croissant, ChickenBreast, RoastChicken, BeefSteak, CookedSteak, Salmon,
  CookedSalmon, BakedPotato, PotatoSlices, FrenchFries, CorndogRaw, CorndogCooked,
  ChickenLeg, FriedChicken, BaconRaw, BaconCooked, Cucumber, CucumberSlice,
  BellPepperRed, BellPepperSlice, Orange, Milk, Peach, OrangeJuice, Milkshake,
  PeachSmoothie, HotDog, and the `Cup` tableware.
- Clones each model from `Assets/Ultimate Food Pack collection/Prefabs`, sets layer
  10, strips colliders, adds `NetworkObject` + `KitchenObject` + `FollowTransform`.
- Sets `prefab`, `Sprite`, `objectName` on each SO (placeholder icon
  `Assets/_Assets/Icons/Ingredients/food_ingredient.png`).
- Registers every prefab in `Assets/DefaultNetworkPrefabs.asset`.
- Appends each SO to `KitchenGameManager.kitchenObjectSOList` in
  `MainMenuScene.unity` (saves the scene).
- Appends to `plate.prefab` `validKitchenObjectSOList` all cooked/finish items
  (`addToPlateValidList`) and to `cup.prefab` the three drinks (`addToCupValidList`).

Idempotent: existing SOs/prefabs are re-wired, not duplicated.

### Verification
- 54 KitchenObjectSO assets total (26 base + 29 new, `Cup` maps to `Cup.asset`).
- All new KOs present in scene list + network prefab list; plate/cup valid lists extended.

---

## Phase 3 — Create Recipes

Creates recipe assets and registers them in `Assets/SO/RecipeDatabase/RecipeDatabase.asset`.

### Recipes created

**Baking** (`Assets/SO/BakingRecipeSO/`, 7):

| Recipe | Input | Output | Timer |
| --- | --- | --- | --- |
| Bake_Dough_Croissant | Dough | Croissant | 8s |
| Bake_ChickenBreast_RoastChicken | ChickenBreast | RoastChicken | 12s |
| Bake_BeefSteak_CookedSteak | BeefSteak | CookedSteak | 10s |
| Bake_Salmon_CookedSalmon | Salmon | CookedSalmon | 10s |
| Bake_CorndogRaw_CorndogCooked | CorndogRaw | CorndogCooked | 9s |
| Bake_BaconRaw_BaconCooked | BaconRaw | BaconCooked | 7s |
| Bake_PotatoSlices_BakedPotato | PotatoSlices | BakedPotato | 11s |

**Deep Fry** (`Assets/SO/DeepFryRecipeSO/`, 4):

| Recipe | Input | Output | Timer |
| --- | --- | --- | --- |
| Fry_PotatoSlices_FrenchFries | PotatoSlices | FrenchFries | 6s |
| Fry_ChickenLeg_FriedChicken | ChickenLeg | FriedChicken | 8s |
| Fry_CorndogRaw_CorndogCooked | CorndogRaw | CorndogCooked | 7s |
| Fry_BaconRaw_BaconCooked | BaconRaw | BaconCooked | 6s |

**Drink** (`Assets/SO/DrinkRecipeSO/`, 3):

| Recipe | Input | Output | Timer |
| --- | --- | --- | --- |
| Drink_Milk_Milkshake | Milk | Milkshake | 4s |
| Drink_Orange_OrangeJuice | Orange | OrangeJuice | 4s |
| Drink_Peach_PeachSmoothie | Peach | PeachSmoothie | 4s |

**Burning** (`Assets/SO/BurningRecipeSO/`, 12) — input → `BurgerTrashSO`, 5s:
RoastChicken, CookedSteak, CookedSalmon, BakedPotato, CorndogCooked, BaconCooked,
Croissant, FrenchFries, FriedChicken, Milkshake, OrangeJuice, PeachSmoothie.

> **Note:** These 12 were originally created with output `"BurgerTrash"` (nonexistent
> asset → null). Fixed in Phase 5.

---

## Phase 4 — Create Cooking Tools

Builds new counter prefabs for the new cooking methods, cloned from
`StovePanCounter.prefab` (the `PanCookingTool` child swapped for the new tool type),
plus a matching `PlacedObjectTypeSO` registered in the `PlacedObjectDatabase`.

| Counter prefab | Tool component | Tool child | PlacedObjectSO | Display |
| --- | --- | --- | --- | --- |
| `OvenCookingToolCounter.prefab` | `OvenCookingTool` | OvenCookingTool | `OvenCounter.asset` | Oven |
| `DeepFryerCounter.prefab` | `DeepFryCookingTool` | DeepFryCookingTool | `DeepFryerCounter.asset` | Deep Fryer |
| `BeverageMachineCounter.prefab` | `BeverageCookingTool` | BeverageCookingTool | `BeverageCounter.asset` | Beverage Machine |

Each counter:
- Carries over `placePoint`, `progressBarUI`, `burnWarningUI`, `burnShowProgressAmount`.
- Re-points `StoveCounterController._cookingTool` to the new tool.
- Gets a unique placed-object `id` (max existing + 1) and a placeholder icon.
- Is registered as a network prefab and added to the `PlacedObjectDatabase`.

---

## Phase 5 — Create Dishes

Adds the full dish menu: 14 dish `FoodSO`s, ingredient unlocks in the shop, the
BaconBurger plate requirement, and the burning-output bug fix.

### Dishes created (`Assets/SO/FoodSO/`, 14 — 11 plate + 3 drink)

| Dish | Ingredients | FoodType | Price | Exp | Unlock Lv |
| --- | --- | --- | --- | --- | --- |
| RoastChickenMeal | RoastChicken + BakedPotato | MainDish | 240 | 30 | 4 |
| SteakFrites | CookedSteak + FrenchFries | MainDish | 280 | 32 | 5 |
| GrilledSalmonPlate | CookedSalmon + BakedPotato | MainDish | 260 | 30 | 5 |
| BakedPotatoes | BakedPotato | MainDish | 130 | 16 | 2 |
| CorndogSpecial | CorndogCooked | MainDish | 170 | 20 | 3 |
| CroissantSnack | Croissant | Dessert | 100 | 14 | 2 |
| ChickenNuggetsMeal | FriedChicken + FrenchFries | MainDish | 190 | 22 | 4 |
| BaconBurger | BunSO, BurgerCookedSO, BaconCooked, LettuceSliceSO, TomatoSlicesSO, CheeseSliceSO | MainDish | 300 | 34 | 5 |
| LoadedFries | FrenchFries + BaconCooked + CheeseSliceSO | MainDish | 220 | 26 | 4 |
| HotDogMeal | HotDog | MainDish | 150 | 18 | 3 |
| GardenSalad | LettuceSliceSO, TomatoSlicesSO, CucumberSlice, OnionSlicesSO | Appetizer | 110 | 15 | 2 |
| OrangeJuiceDrink | OrangeJuice | Drink | 130 | 16 | 2 |
| MilkshakeDrink | Milkshake | Drink | 150 | 20 | 3 |
| PeachSmoothieDrink | PeachSmoothie | Drink | 160 | 22 | 3 |

- Each `FoodSO` gets `recipeName`, `kitchenObjectSOList`, `foodType`, `price`, `exp`,
  `unlockLevel`, `unlockPrice`, sprite, and a **`_guid`** set to its asset GUID
  (required: `ConfigFood` keys `FoodItemDic` by `food.Guid`; OnValidate does not
  fire on `CreateAsset`, so it is set explicitly via `SerializedObject`).
- All 14 registered into `ConfigFood` (`Assets/Configs/ConfigFood/ConfigFoodSO.asset`).

### Shop ingredient unlocks (`Assets/Configs/ConfigShop/ConfigShopSO.asset`, ids 22–34)

| id | Name | Ingredient KO | Category | Price | Unlock Lv |
| --- | --- | --- | --- | --- | --- |
| 22 | Chicken Breast | ChickenBreast | Meats | 25 | 2 |
| 23 | Beef Steak | BeefSteak | Meats | 30 | 3 |
| 24 | Salmon | Salmon | Meats | 35 | 4 |
| 25 | Potato Slices | PotatoSlices | Vegetables | 18 | 2 |
| 26 | Corndog (Raw) | CorndogRaw | Patties | 40 | 3 |
| 27 | Chicken Leg | ChickenLeg | Meats | 22 | 2 |
| 28 | Bacon (Raw) | BaconRaw | Patties | 24 | 3 |
| 29 | Cucumber | Cucumber | Vegetables | 15 | 2 |
| 30 | Bell Pepper | BellPepperRed | Vegetables | 16 | 2 |
| 31 | Orange | Orange | Vegetables | 18 | 2 |
| 32 | Milk | Milk | Dairy | 16 | 2 |
| 33 | Peach | Peach | Vegetables | 20 | 3 |
| 34 | Dough | Dough | Bakery | 14 | 2 |

### Plate valid list
Appended `BurgerCookedSO` to `plate.prefab` `validKitchenObjectSOList` so BaconBurger
can be assembled.

### Burning-output bug fix
`FixBurningOutputs()` re-pointed all 12 `Burn_*` recipe outputs to `BurgerTrashSO`
(they were created pointing at the nonexistent `"BurgerTrash"`). Fix patches each
`BurningRecipeSO` **asset** directly (a `SerializedObject` on the asset) — the initial
implementation wrote through the database list element (an object reference), which
silently failed and also NRE-crashed the Phase 5 run before its final log line.

---

## Final Verification (post-Phase 5)

| Check | Result |
| --- | --- |
| Burning recipes | 15 total, `nullOutputs = 0` (12 fixed + 3 base valid) |
| ConfigFood food items | 20 (6 base + 14 new), `emptyGuid = 0` |
| ConfigShop shop items | 34 (21 base + 13 new), ingredient items = 22 |
| FoodSO assets on disk | 20 |
| KitchenObjectSO assets | 54 |
| Plate valid list | contains `BurgerCookedSO` |
| Compile | no errors |
| Console (play-mode smoke test, MainMenuScene) | no errors/warnings |

## Deferred

- `FoodCompleteVisual` wiring on `plate.prefab` / `cup.prefab` for the 14 new dishes —
  visual polish only; serving/completion is data-driven
  (`BotCustomerController.cs` compares the customer's `foodSO.kitchenObjectSOList`
  to the tableware's `GetKitchenObjectSOList()`), so new dishes function without it.
