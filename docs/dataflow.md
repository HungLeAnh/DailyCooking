# DailyCooking — New Data Flow (Post Data-Management Implementation)

Reference: `docs/datamanagement.md` (architecture analysis + improvement plan).

## 1. Construction (edit-time)

All ScriptableObjects inherit `SerializableScriptableObject`; `OnValidate` stamps `_guid` from
`AssetDatabase.AssetPathToGUID` (`Assets/Scripts/UI/UIPopup/Popups/InventoryPopup/SerializableScriptableObject.cs:12`).

`RecipeDatabaseSO` is the single source of truth for all recipes
(`Assets/Scripts/SO/RecipeDatabase/RecipeDatabaseSO.cs:6`).

## 2. Boot (server)

`KitchenGameManager.Awake()` (`Assets/Scripts/Counters/KitchenGameManager.cs:53`):

1. Builds `kitchenObjectSODic[guid → KitchenObjectSO]` from the scene list.
2. `recipeDatabase.Initialize()` — pre-builds 4 dictionary caches (input SO → recipe) instead of linear scans.
3. `popupDatabase.Initialize()` — enum-keyed popup dictionary.
4. `KitchenObjectPool.PrewarmPools()` — pools 5 instances per kitchen-object type (pool min 10 / max 50).

## 3. Runtime flows

### Kitchen object spawn (client → server)

`SpawnKitchenObject(SO, parent, index)` → `SpawnKitchenObjectServerRpc(guid, networkRef, index)`
(`KitchenGameManager.cs:150`) — only the GUID crosses the wire → server resolves the SO via the
dictionary → gets the object from the pool (`KitchenObjectPool.GetKitchenObject(guid)`) or `Instantiate`
fallback → `Spawn(true)` → parented to the counter.

### Placed objects (buildings/counters)

`CreatePlacedObjectViewServerRpc(guid, ...)` → delegated to
`PrefabSpawnService.SpawnPlacedObjectDirect` (`Assets/Scripts/Services/PrefabSpawnService.cs:14`)
→ GUID→SO lookup in `GridBuildingSystem` → instantiate prefab → `PlacedObjectView.Intialize(guid, ...)`
→ spawn + ownership change → `NotifyClientOfSpawnClientRpc` → `OnSpawnRequestCompleted` callback.

### Recipes

Counters no longer scan per-prefab arrays; they query
`KitchenGameManager.Instance.RecipeDatabase.GetCuttingRecipe(input)` / `GetFryingRecipe` /
`GetBurningRecipe` / `GetCombineRecipe` — O(1) dictionary lookup
(`CuttingCounterController.cs:137`, `PanCookingTool.cs`, `PotCookingTool.cs`).

### Popups

`UIPopupManager` → `popupDatabase.GetPopup(popupType)` keyed by the `UIPopupType` enum
(`UIPopupManager.cs:67`) — no magic-string lookup.

## 4. Persistence & networking

- Saves persist only GUID strings (decoupled pattern preserved).
- Network protocol uses GUID strings for all SO references (index-based RPCs replaced).

## 5. Known remaining risks

- Circular SO↔prefab references (`KitchenObject.kitchenObjectSO` baked into the prefab) — two sources of
  truth can desync (datamanagement.md §2.3).
- `StewBurnedSO.objectName = "Stew"` duplicates FoodSO `Stew` recipe name (pending).
- `StewSO.price=0, exp=0` — likely unfinished (pending).
- `TablewareCounter` (SO baked on prefab) vs `ContainerCounter` (GUID-resolved at runtime) mixed patterns — no
  enforced standard (datamanagement.md §4.7).
- Adding new content still requires touching 4 places (SO asset, prefab, scene list, `DefaultNetworkPrefabs.asset`).
