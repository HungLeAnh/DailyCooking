# DailyCooking — Data & Prefab Architecture Analysis

## Executive Summary

DailyCooking uses a **ScriptableObject-driven data architecture** for all gameplay content (recipes, ingredients, upgrades, buildings, popups, shop items). Prefabs are composed via nested PrefabInstances of `_`-prefixed base templates. Spawning is GUID-based over the network, but spawned objects read their data from a SO field **baked into the prefab**, creating a circular SO↔prefab reference.

### Completed Improvements (Tier 1 + Tier 2)

- **Tier 1 (Quick Wins):** Deleted dead `CookingRecipeSO`, removed unused recipe lists, fixed "Chesse"/"Tomatoe" typos, renamed `BurnedRecipeSO`→`BurningRecipeSO`, moved misplaced assets, removed dangling `upgradeDataList` prefab override, added `OnValidate`/`ContextMenu` editor validation
- **Tier 2 (Data Integrity):** Created `RecipeDatabaseSO` centralizing all recipes, replaced index-based RPCs with GUID-based, replaced popup string lookup with dictionary, replaced shop reward string with structured `ShopReward` type
- **Tier 3 (Performance & Scale):** Added `KitchenObjectPool` for pooled kitchen object instantiation, extracted `PrefabSpawnService` from `KitchenGameManager` for placed object spawning, fixed bot pool routing to use pool index instead of name-based lookup

---

## 1. Data Construction (ScriptableObjects)

### 1.1 SO Classes (19 classes, 74 assets)

| Class | File | Assets | Key Fields |
|---|---|---|---|
| `KitchenObjectSO` | `Assets/Scripts/SO/KitchenObjectSO/` | 24 | `Transform prefab`, `Sprite`, `string objectName`, `KitchenObjectOptionalProcessSO processSO` |
| `RefillerKitchenObjectSO` | same dir | 1 | + `float refillingAmount` |
| `KitchenObjectOptionalProcessSO` | `Assets/SO/OptionalProcessSO/` | 1 | `KitchenObjectSO input`, `List<KitchenObjectSO> processListOutput` |
| `CookingRecipeSO` | `Assets/Scripts/SO/RecipeSO/` | **0 (dead)** | `input`, `output`, `float cookingTimerMax` — never used |
| `CuttingRecipeSO` | same dir | 7 | `input`, `output`, `int cuttingProgressMax` |
| `FryingRecipeSO` | same dir | 2 | `input`, `output`, `float fryingTimerMax` |
| `BurningRecipeSO` | `Assets/SO/BurnedRecipeSO/` | 3 | `input`, `output`, `float burningTimerMax` |
| `CombineRecipeSO` | `Assets/Scripts/Counters/` | 1 | `List<KitchenObjectSO> input`, `output`, `float combineTimerMax` |
| `UpgradeSO` | `Assets/Scripts/SO/UpgradeSO/` | 8 | `upgradeName`, `upgradeDescription`, `upgradeIcon`, `upgradeCosts`, `levelUnlocked`, `UpgradeType`, `UpgradeTarget`, `upgradeValue` |
| `ConfigUpgrade` | same dir | 1 | `List<UpgradeSO> upgrades`, `GetUpgradeOfType()` |
| `AudioClipRefsSO` | `Assets/SO/SoundClips.asset` | 1 | arrays for chop, delivery, footstep, etc. |
| `FoodSO` | `Assets/Scripts/KitchenObject/` | 6 | `List<KitchenObjectSO> kitchenObjectSOList`, `Sprite`, `string recipeName`, `long price`, `int exp`, `FoodType`, `int unlockLevel`, `int unlockPrice` |
| `PlacedObjectTypeSO` | `Assets/Scripts/GridBuildingSystem/` | 14 | `id`, `nameString`, `prefab`, `visual`, `icon`, `width`, `height`, `ItemType` |
| `PopupDatabase` | `Assets/Scripts/Databases/` | 1 | `List<PopupData>` (popupName, popupPrefab) |
| `PlacedObjectDatabase` | `Assets/Scripts/Databases/` | 1 | `List<PlacedObjectTypeSO> placeObjects` |
| `InventoryTabDatabase` | `Assets/Scripts/UI/.../InventoryTabDatabase.cs` | 1 | `List<InventoryTab>` (sprite + type) |
| `ConfigFood` | `Assets/Scripts/Configs/` | 1 | `List<FoodSO> foodItems`, GUID dictionary |
| `ConfigShop` | `Assets/Scripts/Configs/` | 1 | `List<ConfigShopItem>` (id, name, type, price, reward, category, unlockLevel) |
| `SerializableScriptableObject` | base class | — | `_guid` stamped in `OnValidate` via `AssetDatabase` |

### 1.2 Asset Locations (conventions and issues)

- Most SO assets live under `Assets/SO/<ClassName>/` — consistent.
- **Misplaced**: `AudioClipRefsSO` sits in `Assets/SO/` root; `InventoryTabDatabase` is in `Assets/SO/PopupDatabase/` (wrong folder); `ConfigFood`/`ConfigShop`/`ConfigUpgrade` are in `Assets/Configs/` (separate from SOs).
- **Folder/class mismatch**: `Assets/SO/BurnedRecipeSO/` folder contains `BurningRecipeSO` assets.
- **Empty folder**: `Assets/SO/InventorySO/TabSO/` — no assets, leftover from abandoned `TabSO` idea.

### 1.3 Data Flow

All SO assets are wired via **serialized fields in scenes/prefabs**. No `Resources.Load` or `FindObjectOfType` for SOs at runtime.

1. **KitchenObjectSO**: `KitchenGameManager.Awake()` builds `kitchenObjectSODic[guid]` from scene list and pre-warms `KitchenObjectPool`. `SpawnKitchenObjectServerRpc` passes only the GUID string over the network; lookup resolves the SO; `KitchenObjectPool.GetKitchenObject(guid)` retrieves a pooled instance (or `Instantiate` as fallback); `NetworkObject.Spawn(true)` makes it visible.
2. **Recipes**: linear scan `foreach (recipe in array) if (recipe.input == inputSO)` in counter controllers. Recipe arrays are baked into counter prefabs (`CuttingCounter.prefab`, `PanCookingTool.prefab`, `PotCookingTool.prefab`).
3. **PlacedObjectTypeSO**: `GridBuildingSystem` builds a GUID→SO dictionary; `PlacedObjectView` re-resolves by GUID string after network spawn.
4. **FoodSO/Shop/Upgrades**: `ConfigManager` holds the config SOs; saves persist only GUID strings.
5. **PopupDatabase**: `UIPopupManager.CreatePopup` → `popupDatabase.GetPopup(popupType.ToString())` (string-keyed by enum name).
6. **Shop rewards**: `ConfigShopItem.reward` is a raw string like `"{guid}_{count}"` parsed ad hoc in `UIShopItem`.

---

## 2. Prefab Construction

### 2.1 Inventory

- **Location**: `Assets/Prefab/` (singular folder)
- **~110 prefabs** across 11 subfolders: `Bot/`, `Buildings/`, `CookingTool/`, `Counters/`, `Foods/`, `KitchenObjects/`, `Player/`, `PostBox/`, `Tables/`, `UI/`
- **Naming**: PascalCase for concrete prefabs, `_`-prefix for base templates (`_BaseCounter`, `_BaseTable`, `_BaseCookingTool`, `_BaseTablewareCounter`)
- **Composition**: concrete prefabs embed base prefabs as **nested PrefabInstances** — no true prefab variants (`m_PrefabParentObject`).

### 2.2 Spawning (Instantiation Call Sites)

| File:Line | What | Prefab Source |
|---|---|---|
| `KitchenGameManager.cs:156` | Kitchen object | `kitchenObjectSO.prefab` (SO field) |
| `KitchenGameManager.cs:172` | Placed counter/building | `placedObjectTypeSO.prefab` (SO field) |
| `GameManager.cs:90` | Player | serialized `playerPrefab` field |
| `BotManager.cs:27` | Bot customer | serialized `botPrefabs[]` array |
| `GridBuildingSystem.cs:117` | PostBox | serialized `postBoxPrefab` |
| `GridInitializer.cs:32,36,41,57` | Roads/floors | constructor-injected from serialized fields |
| `TablewareCounterController.cs:68` | Tableware visual | serialized `_tablewareVisualPrefab` |
| `UIPopupManager.cs:73` | UI popups | `popupData.popupPrefab` from `PopupDatabase` SO |
| Various UI scripts | UI items | serialized `_itemPrefab`, `tabPrefab`, etc. |

**Key points**: Runtime gameplay spawning uses only two mechanisms — SO-held prefab references and serialized MonoBehaviour fields. No `Resources.Load` paths in game code, no Addressables.

### 2.3 Prefab↔SO Circular Reference

Confirmed circular pattern:
- `TomatoSO.asset` → `food_ingredient_tomato.prefab` (via `KitchenObjectSO.prefab`)
- `food_ingredient_tomato.prefab` → `TomatoSO` (via `KitchenObject.kitchenObjectSO` field)
- Same pattern for `PlateSO`↔`plate.prefab`, `BowlSO`↔`bowl.prefab`

**Risk**: Two sources of truth that can desync. Updating one requires touching the other.

### 2.4 Data on Spawned Objects

Spawned objects read their data from the **SO field baked into the prefab** (`KitchenObject.kitchenObjectSO`, `KitchenObject.cs:8`), NOT from the SO whose GUID was sent in the spawn RPC. The SO passed to `SpawnKitchenObject` is only used for prefab selection.

---

## 3. Networking Pattern

- All networked objects inherit `NetworkBehaviour`.
- Only server/host authorizes state changes; clients request via `ServerRpc`.
- **Network protocol uses GUID strings** for SO references (correct pattern).
- **Exception**: `RemoveDishFromMenuServerRpc(int dishIndex)` and `PurchaseUpgradeServerRpc(int upgradeIndex)` use **list index** instead of GUID — fragile to reordering.
- `DefaultNetworkPrefabs.asset` registers ~50 prefabs for Netcode-for-GameObjects.

---

## 4. Problem Inventory

### 4.1 Dead / Unused Code

| Issue | Location | Impact |
|---|---|---|
| `CookingRecipeSO` class — 0 assets, 0 runtime references | `Assets/Scripts/SO/RecipeSO/` | Dead code, misleading |
| `KitchenGameManager.cuttingRecipeSOList` / `.fryingRecipeSOList` — never read | `KitchenGameManager.cs:30-31` | Duplicated with per-prefab arrays |
| Empty `Assets/SO/InventorySO/TabSO/` folder | — | Leftover, confusing |

### 4.2 Duplication

| Issue | Locations |
|---|---|
| Recipe data exists in both scene-level lists AND per-prefab arrays | `KitchenGameManager` scene fields vs. `CuttingCounter.prefab` / `PanCookingTool.prefab` / `PotCookingTool.prefab` |
| Shop GUIDs duplicated across `ConfigShop`, `PlacedObjectDatabase`, `KitchenObjectSODic` | `ConfigShop.cs`, `PlacedObjectDatabase.cs`, `KitchenGameManager.cs` |
| Linear lookup in `UIShopItem.GetReward` duplicates `GridBuildingSystem.GetPlacedObjectTypeSOByGuid` | `UIShopItem.cs`, `GridBuildingSystem.cs` |

### 4.3 Fragile Couplings

| Issue | Location | Status |
|---|---|---|
| Popup lookup by `enum.ToString()` — rename breaks silently | `UIPopupManager.cs` | ✅ Fixed |
| Shop reward format `"{guid}_{count}"` — contract in two places | `ConfigShopSO.asset`, `UIShopItem.cs` | ✅ Fixed |
| Index-based RPCs (`dishIndex`, `upgradeIndex`) — list order = wire protocol | `KitchenGameManager`, `UpgradeManager` | ✅ Fixed |
| Bot pool routing by stripping `"(Clone)"` from name — breaks on rename | `BotManager.cs` | ✅ Fixed |
| Numeric enum values stored as raw ints in `ConfigShopSO.asset` | `ConfigShopSO.asset` | ⏳ Pending |

### 4.4 Typos / Naming

| Issue | Status |
|---|---|
| "Chesse" typo across `ChesseSO`, `ChesseSliceSO`, `Chesse-ChesseSlices.asset`, `ChesseBurger.prefab`, in-game display name | ✅ Fixed |
| `Tomatoe-TomatoSlices.asset` filename typo | ✅ Fixed |
| `Assets/SO/BurnedRecipeSO/` folder name ≠ class name `BurningRecipeSO` | ✅ Fixed |
| `InventoryTabDatabase.asset` in `PopupDatabase/` folder | ✅ Fixed |
| `SoundClips.asset` in `Assets/SO/` root (not in subfolder) | ✅ Fixed |
| `StewBurnedSO.objectName = "Stew"` duplicates FoodSO `Stew` recipe name | ⏳ Pending |
| `StewSO.price=0, exp=0` — likely unfinished | ⏳ Pending |

### 4.5 Dangling Reference

- `DoubleTable.prefab` and `SingleTable.prefab` contain a prefab-modification `propertyPath: upgradeDataList` whose `objectReference` points to a **deleted SO asset** (`{fileID: 11400000, guid: b387cc8f...}`). The field no longer exists on `Table.cs`.

### 4.6 Missing Pooling (Resolved)

- Kitchen objects (ingredients, plates, stews) now use `ObjectPool<GameObject>` via `KitchenObjectPool` singleton (pool size 10 per KitchenObjectSO type).
- Bots already used `ObjectPool<GameObject>` (pool size 10). Bot pool routing now uses `poolIndex` stored on `BotCustomerController` instead of name-based lookup.

### 4.7 Mixed Counter Data Pattern

- **Tableware counters** (`BowlTablewareCounter`, `PlateTablewareCounter`): SO baked directly on the prefab (`_tablewareKitchenObjectSO`).
- **Container counters** (`ContainerCounter`, `OptionalContainerCounter`): resolve SO by GUID at runtime via `KitchenGameManager.GetKitchenObjectSOByGuid()`.
- New counters must pick the "right" pattern by convention only — no enforced standard.

### 4.8 Content Registration Burden

Adding a new food/counter requires touching **4 places**:
1. Create SO asset in `Assets/SO/`
2. Create prefab in `Assets/Prefab/`
3. Register in `KitchenGameManager.kitchenObjectSOList` (scene) or `GridBuildingSystem`
4. Add to `DefaultNetworkPrefabs.asset`

---

## 5. Improvement Plan

### Tier 1 — Quick Wins (safe, low risk) ✅ ALL COMPLETED

| # | Action | Rationale | Status |
|---|---|---|---|
| 1 | Delete `CookingRecipeSO` class and its folder | Dead code, zero usage | ✅ Done |
| 2 | Remove `cuttingRecipeSOList`/`fryingRecipeSOList` from `KitchenGameManager` | Unused duplicates | ✅ Done |
| 3 | Delete empty `Assets/SO/InventorySO/TabSO/` folder | Leftover | ✅ Done |
| 4 | Fix "Chesse" → "Cheese" across all assets and prefab names | Consistency | ✅ Done |
| 5 | Fix "Tomatoe" → "Tomato" filename | Consistency | ✅ Done |
| 6 | Rename `Assets/SO/BurnedRecipeSO/` folder → `BurningRecipeSO/` | Match class name | ✅ Done |
| 7 | Move `InventoryTabDatabase.asset` to `Assets/SO/InventoryTabDatabase/` | Consistent placement | ✅ Done |
| 8 | Move `SoundClips.asset` to `Assets/SO/AudioClipRefsSO/` | Consistent placement | ✅ Done |
| 9 | Remove dangling `upgradeDataList` prefab override from `SingleTable`/`DoubleTable` | Broken reference | ✅ Done |
| 10 | Add `OnValidate` editor check + ContextMenu validation in `SerializableScriptableObject` | Prevent future desync | ✅ Done |

### Tier 2 — Data Integrity (medium effort) ✅ ALL COMPLETED

| # | Action | Rationale | Status |
|---|---|---|---|
| 1 | Create `RecipeDatabaseSO` centralizing all recipes | Single source of truth | ✅ Done |
| 2 | Spawned kitchen objects read SO from prefab-baked field (circular ref noted) | Future improvement | ⏳ Pending |
| 3 | Replace index-based RPCs with GUID-based | Robust to reordering | ✅ Done |
| 4 | Standardize all counters to GUID-resolved pattern | Consistent data access | ⏳ Pending |
| 5 | Replace popup string lookup with `Dictionary<UIPopupType, PopupData>` | No magic string coupling | ✅ Done |
| 6 | Replace `"{guid}_{count}"` shop reward string with structured `ShopReward` | Type-safe reward data | ✅ Done |

### Tier 3 — Performance & Scale ✅ ALL COMPLETED

| # | Action | Rationale | Status |
|---|---|---|---|
| 1 | Add `ObjectPool<GameObject>` for kitchen objects | Reduce GC + NetworkObject spawn/despawn churn | ✅ Done |
| 2 | Extract `PrefabSpawnService` from `KitchenGameManager` | Single responsibility | ✅ Done |
| 3 | Consider Addressables for content streaming | Enables DLC, reduces build size | ⏳ Future consideration |
| 4 | Add a `PlacedObjectDatabase`-style lookup to `UIShopItem` instead of linear `Find` | Performance + consistency | ✅ Already in place

---

## 6. Key File References

| Purpose | Path |
|---|---|
| SO base class | `Assets/Scripts/UI/UIPopup/Popups/InventoryPopup/SerializableScriptableObject.cs` |
| KitchenObjectSO | `Assets/Scripts/SO/KitchenObjectSO/KitchenObjectSO.cs` |
| KitchenObject (spawned object) | `Assets/Scripts/KitchenObject/KitchenObject.cs` |
| KitchenObjectPool (object pooling) | `Assets/Scripts/KitchenObject/KitchenObjectPool.cs` |
| PrefabSpawnService (placed object spawning) | `Assets/Scripts/Services/PrefabSpawnService.cs` |
| KitchenGameManager (spawner) | `Assets/Scripts/Counters/KitchenGameManager.cs` |
| GridBuildingSystem (GUID dict) | `Assets/Scripts/GridBuildingSystem/` |
| PlacedObjectTypeSO | `Assets/Scripts/GridBuildingSystem/PlacedObjectMVC/PlacedObjectTypeSO.cs` |
| BotManager (pooling example) | `Assets/Scripts/Bot/BotManager.cs` |
| UIPopupManager (popup spawning) | `Assets/Scripts/UI/UIPopup/UIPopupManager.cs` |
| ConfigFood / ConfigShop | `Assets/Scripts/Configs/` |
| DefaultNetworkPrefabs | `Assets/DefaultNetworkPrefabs.asset` |
| Scene wiring | `Assets/Scenes/MainMenuScene.unity`, `Assets/Scenes/GameScene.unity` |

---

## 7. Implementation Notes

- **Save system** stores only GUID strings — this is the correct decoupled pattern and should be preserved. Any change to SO GUIDs (e.g., reimporting) will break saves; the `OnValidate` GUID stamp mitigates this.
- **Singleton coupling**: consumers reach SOs through global instances (`KitchenGameManager.Instance`, `GridBuildingSystem.Instance`, `ConfigManager.Instance`). This is acceptable at current scale but should be considered for DI if the project grows.
- **ScriptableObject data is shared** at runtime — clone with `Instantiate()` if per-instance mutation is needed (per skill docs).
- **All network state changes** must go through `ServerRpc` — never modify `NetworkVariable` directly from a client (per skill docs).