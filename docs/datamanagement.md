# DailyCooking — Data & Prefab Architecture Analysis

## Executive Summary

DailyCooking uses a **ScriptableObject-driven data architecture** for all gameplay content (recipes, ingredients, upgrades, buildings, popups, shop items). Prefabs are composed via nested PrefabInstances of `_`-prefixed base templates. Spawning is GUID-based over the network, but spawned objects read their data from a SO field **baked into the prefab**, creating a circular SO↔prefab reference.

### Completed Improvements (Tier 1 + Tier 2)

- **Tier 1 (Quick Wins):** Deleted dead `CookingRecipeSO`, removed unused recipe lists, fixed "Chesse"/"Tomatoe" typos, renamed `BurnedRecipeSO`→`BurningRecipeSO`, moved misplaced assets, removed dangling `upgradeDataList` prefab override, added `OnValidate`/`ContextMenu` editor validation
- **Tier 2 (Data Integrity):** Created `RecipeDatabaseSO` centralizing all recipes, replaced index-based RPCs with GUID-based, replaced popup string lookup with dictionary, replaced shop reward string with structured `ShopReward` type
- **Tier 3 (Performance & Scale):** Fixed bot pool routing to use `poolIndex` instead of name-based lookup. Kitchen-object pooling and the `PrefabSpawnService` extraction were tried and **reverted** — pooling conflicted with `NetworkObject` lifetimes (see §4.6).

---

## 1. Data Construction (ScriptableObjects)

### 1.1 SO Classes

| Class | File (under `Assets/Scripts/`) | Key Fields |
|---|---|---|
| `KitchenObjectSO` | `SO/KitchenObjectSO/` | `Transform prefab`, `Sprite`, `string objectName`, `KitchenObjectOptionalProcessSO processSO` |
| `RefillerKitchenObjectSO` | `SO/KitchenObjectSO/` | + `float refillingAmount` |
| `KitchenObjectOptionalProcessSO` | `SO/KitchenObjectSO/` | `KitchenObjectSO input`, `List<KitchenObjectSO> processListOutput` |
| `CuttingRecipeSO` | `SO/RecipeSO/` | `input`, `output`, `int cuttingProgressMax` |
| `FryingRecipeSO` | `SO/RecipeSO/` | `input`, `output`, `float fryingTimerMax` |
| `BurningRecipeSO` | `SO/RecipeSO/` | `input`, `output`, `float burningTimerMax` |
| `BakingRecipeSO` | `SO/RecipeSO/` | `input`, `output`, bake time |
| `DeepFryRecipeSO` | `SO/RecipeSO/` | `input`, `output`, deep-fry time |
| `DrinkRecipeSO` | `SO/RecipeSO/` | `List<KitchenObjectSO> input`, `output` |
| `CombineRecipeSO` | `Counters/` | `List<KitchenObjectSO> input`, `output`, `float combineTimerMax` |
| `RecipeDatabaseSO` | `SO/RecipeDatabase/` | the recipe lists above; dictionary caches built in `Initialize()` |
| `CookingToolConfigSO` | `SO/` | `List<CookingToolType> toolTypes`, `supportsOptionMenu`, `Supports()` |
| `UpgradeSO` | `SO/UpgradeSO/` | `upgradeName`, `upgradeDescription`, `upgradeIcon`, `upgradeCosts`, `levelUnlocked`, `UpgradeType`, `UpgradeTarget`, `upgradeValue` |
| `ConfigUpgrade` | `SO/UpgradeSO/` | `List<UpgradeSO> upgrades`, `GetUpgradeOfType()` |
| `AudioClipRefsSO` | `SO/` | arrays for chop, delivery, footstep, etc. |
| `FoodSO` | `KitchenObject/` | `List<KitchenObjectSO> kitchenObjectSOList`, `Sprite`, `string recipeName`, `long price`, `int exp`, `FoodType`, `int unlockLevel`, `int unlockPrice` |
| `PlacedObjectTypeSO` | `GridBuildingSystem/PlacedObjectMVC/` | `id`, `nameString`, `prefab`, `visual`, `icon`, `width`, `height`, `ItemType` |
| `PopupDatabase` | `Databases/` | `List<PopupData>`, keyed by `UIPopupType` |
| `PlacedObjectDatabase` | `Databases/` | `List<PlacedObjectTypeSO> placeObjects` |
| `InventoryTabDatabase` | `UI/UIPopup/Popups/InventoryPopup/` | `List<InventoryTab>` (sprite + type) |
| `ConfigFood` | `Configs/` | `List<FoodSO> foodItems`, GUID dictionary |
| `ConfigShop` | `Configs/` | `List<ConfigShopItem>` (id, name, type, price, rewards, category, unlockLevel) |
| `SerializableScriptableObject` | `UI/UIPopup/Popups/InventoryPopup/` | base class; `_guid` stamped in `OnValidate` via `AssetDatabase` |

### 1.2 Asset Locations

- SO assets live under `Assets/SO/<ClassName>/`, one folder per class (e.g. `Assets/SO/CuttingRecipeSO/`).
- Config assets are the one deliberate exception: `Assets/Configs/` holds `ConfigFood`, `ConfigShop`, `ConfigUpgrade` and `PlacedObject/`.
- The misplacements and mismatches that used to live here (`SoundClips.asset` in the SO root, `InventoryTabDatabase` under `PopupDatabase/`, the `BurnedRecipeSO` folder name, the empty `InventorySO/TabSO/` folder) are fixed — see Tier 1 in §5.

### 1.3 Data Flow

All SO assets are wired via **serialized fields in scenes/prefabs**. No `Resources.Load` or `FindObjectOfType` for SOs at runtime.

1. **KitchenObjectSO**: `KitchenGameManager.Awake()` builds `kitchenObjectSODic[guid]` from the scene list and calls `RecipeDatabase.Initialize()`. `SpawnKitchenObject` is server-only; it sets the parent before `NetworkObject.Spawn()` so clients never see an unparented item. There is **no** kitchen-object pooling — it conflicted with `NetworkObject` lifetimes (see `docs/dataflow.md` §4).
2. **Recipes**: `RecipeDatabaseSO` owns every recipe list and pre-builds `Dictionary<KitchenObjectSO, T>` caches in `Initialize()`. Cooking tools reach it through `KitchenGameManager.Instance.RecipeDatabase` via `RecipeResolver`; `CookingToolConfigSO` declares which recipe categories a given tool supports (`Frying`, `Baking`, `DeepFry`, `Beverage`, `Combine`, `Cutting`).
3. **PlacedObjectTypeSO**: `GridBuildingSystem` builds a GUID→SO dictionary. `PlacedObjectView` carries the GUID as a server-written `NetworkVariable<FixedString64Bytes>` and re-resolves it in `OnNetworkSpawn` and on change.
4. **FoodSO/Shop/Upgrades**: `ConfigManager` holds the config SOs; saves persist only GUID strings, and the menu/upgrade RPCs take `string foodGuid` / `string upgradeGuid`.
5. **PopupDatabase**: `UIPopupManager` looks the popup up with `popupDatabase.GetPopup(popupType)` — keyed by the `UIPopupType` enum, not by its name.
6. **Shop rewards**: `ConfigShopItem` exposes a typed `List<ShopReward>`; `GameManager.Economy.cs` walks it when a purchase lands. The old `"{guid}_{count}"` string contract is gone.

---

## 2. Prefab Construction

### 2.1 Inventory

- **Location**: `Assets/Prefab/` (singular folder)
- **~130 prefabs** across 10 subfolders: `Bot/`, `Buildings/`, `CookingTool/`, `Counters/`, `Foods/`, `KitchenObjects/`, `Player/`, `PostBox/`, `Tables/`, `UI/`
- **Naming**: PascalCase for concrete prefabs, `_`-prefix for base templates (`_BaseCounter`, `_BaseTable`, `_BaseCookingTool`, `_BaseTablewareCounter`)
- **Composition**: concrete prefabs embed base prefabs as **nested PrefabInstances** — no true prefab variants (`m_PrefabParentObject`).

### 2.2 Spawning (Instantiation Call Sites)

| File:Line | What | Prefab Source |
|---|---|---|
| `KitchenGameManager.cs:156` | Kitchen object | `kitchenObjectSO.prefab` (SO field), resolved by GUID |
| `GridBuildingSystem.cs:316` | Placed counter/building | `placedObjectTypeSO.prefab` (SO field), server-owned |
| `GameManager.cs:119` | Player | serialized `playerPrefab` field |
| `BotManager.cs:29,143,180` | Bot customer | serialized `botPrefabs[]`, served by `ObjectPool<GameObject>` |
| `GridBuildingSystem.cs:129` | PostBox | serialized `postBoxPrefab` |
| `GridInitializer.cs:30,34,39,59` | Roads/floors | constructor-injected from serialized fields |
| `UIPopupManager.cs:60` | UI popups | `popupData.popupPrefab` from `PopupDatabase` SO |
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
- **Network protocol uses GUID strings** for SO references (correct pattern). `RemoveDishFromMenuServerRpc` and `PurchaseUpgradeServerRpc` both take a `string` GUID now, so list reordering no longer breaks the wire protocol.
- Placed objects carry their GUID in a `NetworkVariable<FixedString64Bytes>` on `PlacedObjectView` rather than re-sending it per RPC.
- `DefaultNetworkPrefabs.asset` registers ~50 prefabs for Netcode-for-GameObjects.

---

## 4. Problem Inventory

### 4.1 Dead / Unused Code

All three items that used to be listed here are gone: the `CookingRecipeSO` class, the
`cuttingRecipeSOList` / `fryingRecipeSOList` scene fields on `KitchenGameManager`, and the empty
`Assets/SO/InventorySO/TabSO/` folder (Tier 1, §5).

### 4.2 Duplication

| Issue | Locations | Status |
|---|---|---|
| Recipe data duplicated between scene lists and per-prefab arrays | — | ✅ Resolved: `RecipeDatabaseSO` is the single source |
| Shop GUIDs duplicated across three separate lookups | `ConfigShop`, `PlacedObjectDatabase`, `KitchenObjectSODic` | ⏳ Open — three dictionaries over the same GUID space |
| `UIShopItem.GetReward` does a linear `Find` on `PlacedObjectDatabase` | `UIShopItem.cs:107` | ⏳ Open, but only runs once per purchase, so not a perf problem |

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
| `StewBurnedSO.objectName = "Stew"` duplicates FoodSO `Stew` recipe name | ✅ Fixed (`Stew Burned`) |
| `StewSO.price=0, exp=0` — likely unfinished | ✅ Fixed (`35 / 40`) |
| `StewSO.kitchenObjectSOList` trỏ nguyên liệu sống, không khớp bát/đĩa → không phục vụ được | ✅ Fixed (`[StewCookedSO]`) |

### 4.5 Dangling Reference

Resolved — the `propertyPath: upgradeDataList` prefab modification is no longer present on
`SingleTable.prefab` / `DoubleTable.prefab`, and `Table.cs` never had that field (Tier 1, §5).

### 4.6 Pooling

- Kitchen objects are **not** pooled. `KitchenObjectPool` and the `Services/PrefabSpawnService`
  extraction were removed: pooling fought `NetworkObject` lifetimes and late-join state. Spawn and
  despawn go straight through `KitchenGameManager` on the server (see `docs/dataflow.md` §4).
- Bots are still pooled (`BotManager`, `ObjectPool<GameObject>`), routed by the `poolIndex` stored on
  `BotCustomerController` rather than by stripping `"(Clone)"` from the instance name.

### 4.7 Mixed Counter Data Pattern

Counter biết "mình chứa gì" theo **một trong hai** cách. Đây là **quy ước có chủ đích**, không phải
nợ kỹ thuật — đã điều tra đầy đủ ở `docs/plans/10-counter-data-pattern.md`.

**A. Resolve theo GUID** — `PlacedObjectView.GetPlacedObjectTypeSOGuid()` → tra
`KitchenGameManager.KitchenObjectSODic` / `ContainerDataSerializable`, state đi qua
`NetworkVariable`/`NetworkList`.

**B. SO baked trên prefab** — `[SerializeField] private KitchenObjectSO` (ví dụ
`_tablewareKitchenObjectSO`, `postBoxKitchenSO`), không có state mạng.

| Controller | Nội dung | Cách biết SO |
|---|---|---|
| `ClearCounterController` | 1 món cầm tay | **B** (qua `KitchenObject.GetKitchenObjectSO()`) |
| `TrashCounterController` | xoá món | không cần |
| `ContainerCounterController` | 1 slot nguyên liệu | **A** |
| `OptionalContainerCounterController` | N slot | **A** |
| `TablewareCounterController` | không giới hạn, luôn rót bát/dĩa | **B** |
| `CookingToolCounterController` | chấp nhận loại công cụ | enum list baked |
| `PostBox` | 1 kiện hàng | **A** cho nội dung, **B** cho `postBoxKitchenSO` |

#### Quy tắc chọn pattern

> **Nội dung thay đổi lúc chạy** (cùng một prefab nhưng tuỳ nhà hàng có thể chứa rau khác nhau,
> hoặc bị người chơi lấy/đặt) → **A**.
> **Nội dung cố định** (luôn ra bát/dĩa, không có gì để resolve) → **B** + guard null.

Vì sao không chuẩn hoá tất cả về A: `TablewareCounterController` chuyển sang A sẽ thêm
`NetworkVariable`, thêm đường sync host→client, thêm bề mặt lỗi — mà **không thêm giá trị nào**,
vì nó không có state để đồng bộ. Ngược lại, container mà dùng B thì không thể biết tủ lạnh đang
chứa gì.

#### Bắt buộc khi dùng B

Field baked phải được kiểm null và **log lỗi to**, vì `KitchenGameManager.SpawnKitchenObject`
trả `null` im lặng khi SO thiếu:

```csharp
// TablewareCounterController.InteractEvent
if (_tablewareKitchenObjectSO == null)
{
    Debug.LogError("TablewareCounterController: _tablewareKitchenObjectSO is not assigned!", this);
    return;
}
```

#### Bắt buộc khi thêm counter mới

1. Nội dung có đổi lúc chạy không? Có → A, không → B.
2. Dùng B → thêm guard null + `LogError` như trên; ghi 1 dòng comment trong class nói rõ
   "fixed-content, baked on purpose" để người sau không tưởng là thiếu sót.
3. Dùng A → nhớ rằng GUID là **Unity asset GUID** (`PlacedObjectTypeSO.Guid`), không phải id số;
   xem §4.2 về ba nguồn dữ liệu và mục 12 trong `docs/plans/`.

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

### Tier 2 — Data Integrity (4 of 6 done)

| # | Action | Rationale | Status |
|---|---|---|---|
| 1 | Create `RecipeDatabaseSO` centralizing all recipes | Single source of truth | ✅ Done |
| 2 | Spawned kitchen objects read SO from prefab-baked field (circular ref noted) | Future improvement | ⏳ Pending |
| 3 | Replace index-based RPCs with GUID-based | Robust to reordering | ✅ Done |
| 4 | Standardize all counters to GUID-resolved pattern | Consistent data access | ⏳ Pending |
| 5 | Replace popup string lookup with `Dictionary<UIPopupType, PopupData>` | No magic string coupling | ✅ Done |
| 6 | Replace `"{guid}_{count}"` shop reward string with structured `ShopReward` | Type-safe reward data | ✅ Done |

### Tier 3 — Performance & Scale

| # | Action | Rationale | Status |
|---|---|---|---|
| 1 | Add `ObjectPool<GameObject>` for kitchen objects | Reduce GC + NetworkObject spawn/despawn churn | ❌ Reverted — pooling fought `NetworkObject` lifetimes; kitchen objects spawn/despawn directly again |
| 2 | Extract `PrefabSpawnService` from `KitchenGameManager` | Single responsibility | ❌ Reverted with the pooling change |
| 3 | Consider Addressables for content streaming | Enables DLC, reduces build size | ⏳ Future consideration |
| 4 | Add a `PlacedObjectDatabase`-style lookup to `UIShopItem` instead of linear `Find` | Performance + consistency | ⏳ Open — `UIShopItem.cs:107` still does a `Find`, once per purchase |
| 5 | Bot pool routing by `poolIndex` instead of name-based lookup | Survives prefab renames | ✅ Done |

---

## 6. Key File References

| Purpose | Path |
|---|---|
| SO base class | `Assets/Scripts/UI/UIPopup/Popups/InventoryPopup/SerializableScriptableObject.cs` |
| KitchenObjectSO | `Assets/Scripts/SO/KitchenObjectSO/KitchenObjectSO.cs` |
| KitchenObject (spawned object) | `Assets/Scripts/KitchenObject/KitchenObject.cs` |
| RecipeDatabaseSO (recipe source of truth) | `Assets/Scripts/SO/RecipeDatabase/RecipeDatabaseSO.cs` |
| RecipeResolver (per-slot recipe state) | `Assets/Scripts/Counters/RecipeResolver.cs` |
| CookingToolConfigSO (which categories a tool handles) | `Assets/Scripts/SO/CookingToolConfigSO.cs` |
| KitchenGameManager (spawner, GUID dictionary) | `Assets/Scripts/Counters/KitchenGameManager.cs` |
| GridBuildingSystem (GUID dictionary) | `Assets/Scripts/GridBuildingSystem/GridSystem/GridBuildingSystem.cs` |
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