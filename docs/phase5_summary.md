# Phase 5 Summary — Dish Menu Content & Burning Fix

Date: 2026-08-06

## Objective

Add a full dish menu for the DailyCooking game:

- 14 new dish `FoodSO`s (11 plate dishes + 3 drinks), registered into `ConfigFood`.
- 13 ingredient-unlock items added to `ConfigShop`.
- Extend the plate's valid-ingredient list so `BurgerCookedSO` (BaconBurger ingredient) is accepted.
- Fix the burning-recipe bug where all `Burn_*` recipes pointed at a nonexistent output.

## Bug Fixed: Burning Recipes Pointed to Nonexistent Asset

`Assets/SO/BurningRecipeSO/Burn_*.asset` assets were created with output name
`"BurgerTrash"`, but the actual KitchenObjectSO asset is `BurgerTrashSO`. As a
result every `Burn_*` recipe had a `null` output (broken burning for 12 cooked
ingredients).

Fixed in two places:

1. **Builder source** — `DailyCookingContentBuilder.cs` now references
   `"BurgerTrashSO"` when creating burning recipes.
2. **Existing assets** — `FixBurningOutputs()` was corrected to patch each
   `BurningRecipeSO` asset individually (via a `SerializedObject` on the asset),
   then re-run to fix all 12 null outputs. The previous implementation tried to
   write through the database list element (an object reference), which silently
   failed — and that NRE also aborted Phase 5 before its final log line.

## Changes

### Code

| File | Change |
| --- | --- |
| `Assets/Editor/DailyCookingContentBuilder.cs` | Added `FOOD_SO_DIR = "Assets/SO/FoodSO"` constant. Added Phase 5 section: `FoodDef`/`ShopDef` structs, `CreateFoodSO`, `RegisterFoodInConfigFood`, `AddShopIngredientItem`, `FixBurningOutputs`, `Phase5_CreateDishes_Menu` + `Phase5_CreateDishes`. `CreateFoodSO` now sets `_guid = AssetDatabase.AssetPathToGUID(path)` via `SerializedObject` right after creation (OnValidate does not fire on `CreateAsset`). Menu item: `DailyCooking/Phase 5 - Create Dishes`. |

### Data / Assets Created

**Dish FoodSOs** (`Assets/SO/FoodSO/`), 11 plate + 3 drink:

| Dish | Ingredients | FoodType | Price | Unlock Lv |
| --- | --- | --- | --- | --- |
| RoastChickenMeal | RoastChicken + BakedPotato | MainDish | 240 | 4 |
| SteakFrites | CookedSteak + FrenchFries | MainDish | 280 | 5 |
| GrilledSalmonPlate | CookedSalmon + BakedPotato | MainDish | 260 | 5 |
| BakedPotatoes | BakedPotato | MainDish | 130 | 2 |
| CorndogSpecial | CorndogCooked | MainDish | 170 | 3 |
| CroissantSnack | Croissant | Dessert | 100 | 2 |
| ChickenNuggetsMeal | FriedChicken + FrenchFries | MainDish | 190 | 4 |
| BaconBurger | BunSO, BurgerCookedSO, BaconCooked, LettuceSliceSO, TomatoSlicesSO, CheeseSliceSO | MainDish | 300 | 5 |
| LoadedFries | FrenchFries, BaconCooked, CheeseSliceSO | MainDish | 220 | 4 |
| HotDogMeal | HotDog | MainDish | 150 | 3 |
| GardenSalad | LettuceSliceSO, TomatoSlicesSO, CucumberSlice, OnionSlicesSO | Appetizer | 110 | 2 |
| OrangeJuiceDrink | OrangeJuice | Drink | 130 | 2 |
| MilkshakeDrink | Milkshake | Drink | 150 | 3 |
| PeachSmoothieDrink | PeachSmoothie | Drink | 160 | 3 |

**ConfigFood** — `Assets/Configs/ConfigFood/ConfigFoodSO.asset`:
grew from 6 → 20 food items (all 14 new dishes registered by Guid).

**ConfigShop** — `Assets/Configs/ConfigShop/ConfigShopSO.asset`:
grew from 21 → 34 shop items; 13 new ingredient-unlock items (ids 22–34):

| id | Name | Ingredient | Category | Price | Unlock Lv |
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

**Plate valid list** — `Assets/Prefab/KitchenObjects/plate.prefab`:
appended `BurgerCookedSO` so BaconBurger can be assembled.

**Burning recipes** — `Assets/SO/BurningRecipeSO/Burn_*.asset`:
12 `Burn_*` assets' `output` re-pointed to `BurgerTrashSO`.

## Verification (all passed)

- `burningRecipes = 15`, `nullOutputs = 0` (12 patched + 3 pre-existing valid).
- `ConfigFood = 20` food items; `emptyGuid = 0`.
- `ConfigShop = 34` shop items (`ingredientItems = 22`).
- Plate valid list = 19, `BurgerCookedSO = True`.
- 20 `.asset` files under `Assets/SO/FoodSO`.
- No compile errors; no console errors/warnings after recompile.
- Play-mode smoke test on `MainMenuScene` ran clean (no errors/warnings).

## Notes / Deferred

- Dish serving/completion is **data-driven** (`BotCustomerController.cs` compares
  the customer's `foodSO.kitchenObjectSOList` against the tableware's
  `GetKitchenObjectSOList()`); `FoodCompleteVisual` is visual-only.
- Wiring `FoodCompleteVisual` onto the shared `plate.prefab` / `cup.prefab` for
  the 14 new dishes was intentionally **deferred** (visual polish; risk to shared
  prefabs). New dishes function without it.
- The 3 pre-existing burning recipes (Burger, Stew, VegetableBurger) were
  already valid and were left untouched.
