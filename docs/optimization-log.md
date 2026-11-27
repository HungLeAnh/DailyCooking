# Nhật ký tối ưu & kiểm chứng — DailyCooking

File này **thay cho** `docs/plans/*` (README + 20 plan `01-…` → `20-…`, mục 21 chỉ có trong README)
và `docs/optimization_plan.md`. Đây là bản rút gọn: giữ lại hiện trạng, quyết định, `file:line`,
commit, con số đo và rủi ro còn lại; bỏ phần tường thuật từng bước.

- **Bản gốc đầy đủ** nằm trong git history: `git log -- docs/plans` (và
  `git log -- docs/optimization_plan.md`) để tìm commit, rồi `git show <commit>:docs/plans/<file>`.
- **Việc chưa xong / đang hoãn** được theo dõi ở [`docs/deferred.md`](deferred.md) — file này chỉ là
  nhật ký, không phải danh sách việc.
- `file:line` ghi theo thời điểm viết plan gốc (2026-10); code có thể đã dịch dòng.
- Mọi số đo là **Editor / MPPM / probe**, trừ khi ghi khác. Chưa có số nào trên Player build/Android.

---

## 1. Bảng trạng thái 01–21

| # | Mục | TT | Kết luận một dòng |
|---|---|---|---|
| 01 | Bỏ chuỗi ServerRpc → ClientRpc | ⚠️ | Mô tả gốc sai; chuỗi RPC thật ở `GameManager.Data.cs` là **bắt buộc** (`GameData` không phải NetworkVariable). Hoãn |
| 02 | Thay JSON snapshot khi join | ✅ | Đã đo 2026-10-26 → **KHÔNG LÀM**: JSON + decompress + replace chỉ 53–62 ms; >1 s là `OnDataSyncToNewClient`/TMP |
| 03 | GUID string → số / 16 byte | ⏸ | Chưa làm; khuyến nghị **không làm** (~28 B/RPC, không phải hot path) |
| 04 | Giảm Instantiate/Destroy ở UI popup | ✅/⏸ | Bước 1 `UIMenuPopup` create-once + pool xong 2026-10-21; Bước 2–4 bỏ ngỏ; chưa có số GC |
| 05 | Cache `Camera.main` còn lại | ✅ | Đã cache hết; là **vệ sinh code**, không phải thắng perf có số đo |
| 06 | Cache `PlayerStats` | ⚠️ | Allocation đã xong (`288f03fd6`); phần còn lại là micro → **không làm** |
| 07 | `price`/`exp` của Stew | ✅ | `0/0 → 35/40`, chốt giữ nguyên 2026-10-21; phục vụ thu **54 coins / 40 XP** |
| 08 | `StewBurnedSO.objectName` | ✅ | `Stew → Stew Burned`, chốt giữ nguyên 2026-10-21 |
| 09 | Ghim số enum `ShopItemCategory` | ✅ | Ghim số + `None = 9999`, migrate 3 dòng asset; `[InspectorName]` + guard lúc build (`BuildChecks.cs`) đã có |
| 10 | Hai pattern nạp dữ liệu counter | ✅ | Hai pattern đều đúng; chỉ thêm guard null + quy ước |
| 11 | Vòng `KitchenObjectSO` ↔ prefab | ⏸ | Chỉ 1 vòng thật, bắt buộc; hoãn tới khi làm Addressables |
| 12 | Tra GUID shop reward | ✅ | Dictionary + `LogError` + `GetById` xong; ô ẩn "dính" đã sửa; Bước 3 tùy chọn |
| 13 | Profiler baseline | 🟡 | S1–S10 đã đo trên Editor/MPPM; p95 < 16 ms; **GC/bandwidth trên Player build vẫn chờ** |
| 14 | Runtime ghi vào `GridShaderMaterial.mat` | ✅ | Dùng `new Material(...)` + `Dispose`; `.mat` không còn bẩn sau play |
| 15 | Cắt trên thớt không bấm được | ✅ | Hồi quy thiếu `IHasProgress`; đã sửa (`caddb06fe`), chơi kiểm vòng cắt đầy đủ |
| 16 | Tự động hoá input + kéo ghost bằng chuột | ✅ | Công thức IgnoreFocus/AllDeviceInput…/runPlayerUpdatesInEditMode; kéo ghost PASS |
| 17 | Build APK + 2/4 client | 🟡 | APK chặn (keystore + SDK 35); 2/4 player, movement, bandwidth, snapshot đã kiểm |
| 18 | Warning `Look rotation viewing vector is zero` | ✅ | Bọc Slerp trong `if (Context.IsWalking)` |
| 19 | 3 subscriber `OnMouseClickPerformed` | ✅ | Không có bug: 1 click = 1 event = 1 nhát cắt |
| 20 | Kéo ghost bằng touch | ✅ | Inject `TouchState` Began/Moved/Ended → `IsDragging` đúng, ghost bám ô |
| 21 | `StewSO.kitchenObjectSOList` trỏ sai | ✅ | Đổi thành `[StewCookedSO]` → Stew phục vụ được |

---

## 2. Chi tiết từng mục

### 01 — Bỏ chuỗi ServerRpc → ClientRpc · ⚠️ · Rủi ro cao

- **Mô tả gốc sai.** `optimization_plan` §3.1 nói `ContainerCounterController.cs:154-168`, `Table.cs`,
  `KitchenObject.cs` có ServerRpc rỗng rồi mirror ClientRpc. Thực tế `Table.cs`, `KitchenObject.cs`,
  `BaseContainerCounterController.cs` **không có RPC nào**; `ContainerCounterController` /
  `OptionalContainerCounterController` chỉ có comment nhắc `PlayerStateMachine.InteractServerRpc`.
- Toàn project dùng `[Rpc(SendTo.X)]` (NGO 2.x), 55 chỗ, tập trung ở `GameManager.Data.cs`,
  `GameManager.Economy.cs`, `GridBuildingSystem.cs`, `MultiplayerManager.cs`, `UIManager.cs`,
  `CookingTool.cs`, `PlayerStateMachine.cs`.
- **Chuỗi thật** ở `GameManager.Data.cs` / `GameManager.Economy.cs` (vd `GameManager.Data.cs:11-25`
  `UpdateRestaurantNameServerRpc` → `UpdateRestaurantNameClientRpc`). **Không thừa**: `GameData`
  (`Managers/GameData.cs`) là object C# thuần; bỏ ClientRpc thì client không bao giờ thấy đổi tên,
  thêm món, trừ tiền, mở khoá… 13 cặp mirror cần giữ (đổi tên, menu, inventory, PostBox,
  customization, nhận thưởng, upgrade, currency, exp/level).
- **Hướng thay thế (chưa làm):**
  - Bước 1 — gộp mirror, không đổi mô hình: vd `GameManager.Data.cs:62-100`
    `ServerAddInventory`/`ServerRemoveInventory` → `ServerApplyInventoryDelta(guid, delta)`. Nhẹ, cần
    test 2 client.
  - Bước 2 — đưa `RestaurantData.Coins/Gems/Exp/Level`, `InventoryData`, `MenuData` lên
    `NetworkVariable`/`NetworkList` (= mục 02 lựa chọn 3). Nặng: save, join, UI binding đổi; protocol
    join đổi (host/client phải cùng bản). Rủi ro mất sync âm thầm.
- Kiểm chứng (nếu làm): compile Android, 49/49, MPPM host + client + late joiner (đổi tên, mua item,
  late joiner thấy đúng). Mỗi bước một commit.
- Bandwidth đo ở mục 13 (4 player): **18.6–20.9 KB/s/client** di chuyển, **3.4–5.1** nấu — dưới ngưỡng
  100 KB/s → chưa có bằng chứng cấp thiết.

### 02 — JSON snapshot khi join · ✅ KHÔNG LÀM (2026-10-26) · Rủi ro cao nhất

- **Hiện trạng** (`MultiplayerManager.cs`): `:24` `SNAPSHOT_CHUNK_SIZE = 4000`; `:103` client gọi
  `SyncDataToNewClientServerRpc()`; `:108-128` server `ConvertGameDataToJson` → UTF8 → GZip
  `Optimal` → cắt 4000 B → `ReceiveGameDataChunkClientRpc(snapshotId, i, chunkCount, chunk, …)`
  (`Debug.Log` độ dài ở `:119`); `:129-148` client gom chunk; `:149-168` giải nén →
  `FileDataHandler.LoadFromJson` → `ReplaceGameDataFromHost` → `OnDataSyncToNewClient`;
  `:169-182` `Compress`/`Decompress`. Chạy **1 lần mỗi lần join**, không per-frame.
- **Ràng buộc:** join snapshot và file save **dùng chung bộ serialise** (`:112` dùng
  `DataHandler.ConvertGameDataToJson`, `:159` `snapshotHandler.LoadFromJson`, cùng
  `FileDataHandler`/`SaveJson.CreateSettings()`). Đổi một đường mà không đổi đường kia → không đọc được
  dữ liệu của nhau. `SaveJson.CreateSettings` là type binder chống đọc file lạ (`datamanagement` §7).
- Lựa chọn đã cân nhắc: (1) giữ JSON, bỏ chunking; (2) binary `FastBufferWriter`/MessagePack — phải
  viết lại type binder; (3) `NetworkVariable` từng phần = mục 01 bước 2. **Không chọn cái nào.**
- **Số đo quyết định** (probe tách phase, chi tiết ở mục 13/17): `GameScene` → `GameData != null`
  **0.541 s** (483 B / 1 chunk) và **0.578 s** (601 menu, 12,685 B / **4 chunk**); JSON + giải nén +
  `ReplaceGameDataFromHost` **53–62 ms**; `OnDataSyncToNewClient` **0.995–1.066 s** (subscriber
  `UIPlayerResources.Initialize()` cập nhật TMP). Multi-chunk ghép đúng, hash host == client
  `39A40EB1C19887EE4DC4D273`, `menu=601`, sentinel có cả hai phía; `chunkCount=4` < ngưỡng >5.
- **Quyết định:** không sửa wire format, không hạ `SNAPSHOT_CHUNK_SIZE`. Muốn giảm thời gian join UX
  thì điều tra `OnDataSyncToNewClient`/TMP.

### 03 — GUID string → số · ⏸ Chưa làm · Rủi ro TB–cao

- `.Guid` (`SerializableScriptableObject.cs`, `_guid` gán từ `AssetDatabase.AssetPathToGUID` trong
  `OnValidate`) = **Unity asset GUID 32 hex**, nằm trong asset YAML và **file save**. (Sửa số cũ:
  `optimization_plan` ghi 36 ký tự — sai, 36 là `System.Guid` dạng `8-4-4-4-12`.)
- RPC chở GUID: `GridBuildingSystem.cs:364-365` `PlaceObjectServerRpc`; `GameManager.Data.cs:28,45`
  Add/RemoveDishToMenu; `:66,73,93` Add/RemoveInventoryData, RemovePostBoxData ClientRpc;
  `GameManager.Economy.cs:134,160` `UnlockDishServerRpc`/`PurchaseUpgradeServerRpc`;
  `UIManager.cs:57` `ShowOptionMenuRpc` (chuỗi GUID ghép, nặng nhất). ~33 B/GUID.
- `PlacedObjectView.cs:12`, `RefillerKitchenObject.cs:7` đã dùng
  `NetworkVariable<FixedString64Bytes>` — đúng, không đụng.
- Lựa chọn: A — 2 `ulong`/16 byte (−15 B/GUID, không đổi định danh); B — `uint` hash (−29 B, **va chạm
  hash = gán nhầm món**, bắt buộc build-time validation); **C — không làm (khuyến nghị)**.
- Quy tắc nếu làm: **không đổi `_guid` trong asset hay save**, chỉ đổi cách mang qua mạng; host/client
  phải cùng bản.

### 04 — UI popup Instantiate/Destroy · ✅ Bước 1 (2026-10-21) / ⏸ Bước 2–4

- **Đã có sẵn:** `UIPopupManager.cs:11,34-71` cache popup root theo `UIPopupType` (`Instantiate` ở `:60`
  chỉ lần đầu, `SetupPopup()` `:62` chỉ khi tạo). `UICharacterPopup.FillInvetoryItems()`
  (`:169-213`) đã pool grow-only. Không popup nào tạo lại root — churn nằm ở con.
- **Churn còn lại (lúc lập plan):** `UIMenuPopup.cs:44-111` (destroy/instantiate category + menu item
  mỗi lần mở, food grid mỗi lần bấm category, LINQ `:99-107`); `UICharacterPopup.cs:140-157` badge
  unfit; `UIUpgradePopup.cs:55` `GetComponentsInChildren` mỗi lần mở; `UIOptionMenuPopup.cs:74,97,146`;
  `UILoadRestaurantPopup.cs:47,54`; `UIRewardPopup`/`UILevelUpPopup` `:36,51`;
  `UIFoodDetailPopup.cs:60,65`.
- **Bước 1 đã làm (`UIMenuPopup.cs`):** `Initialize()` = `BuildCategoriesIfNeeded()` +
  `RefreshMenuList()` + chọn lại tab `All`; category dựng một lần từ enum `FoodType`; menu card / food
  item pool (đủ thì bind, thiếu thì `Instantiate`, thừa thì `SetActive(false)`); `Start()` không build
  lần 2; cache `Dictionary<FoodType, List<FoodSO>>` thay LINQ; `GetFirstUnlockedFoodItem()` chỉ trả
  item `activeSelf`; `AddMenuItemCard()` tái dùng card ẩn, `OnRemoveDishClicked()` ẩn thay `Destroy`.
- **Listener (rủi ro chính):** `UIFoodItem.SetMenuFoodItem()` `RemoveAllListeners()` trên
  `infoButton`/`foodButton` và `-=` handler "dish unlocked"; `UIMenuItem.SetMenuFoodItem()`
  `RemoveAllListeners()` trên `buttonRemove`, thêm property `FoodSO`; popup gắn listener **sau** bind.
- **Kiểm (host đơn):** 1 lần mở → 5 category / 20 food item; `ShowPopup` 10 lần → vẫn **5 / 20 / 0**
  (trước đây 1 frame 10 lần sinh 55 category item); All 20/20, Appetizer 3/3, MainDish 13/13, Dessert
  1/1, Drink 3/3, 0 item sai `foodType`; thêm/thêm trùng/xoá/mở lại/thêm lại đều đúng 1 card, pool giữ
  1 card; console 0/0; 49/49; 110 assemblies.
- **Chưa:** số GC Alloc trước/sau (không có số đo); client thấy card mới sau
  `AddDishToMenuServerRpc`; chạy lại toàn bộ tutorial tay; Bước 2 (badge `UICharacterPopup`), Bước 3
  (popup mở ít — khuyến nghị bỏ), Bước 4 (cache `UIUpgradeItem[]`).

### 05 — Cache `Camera.main` · ✅

- Grep trước khi sửa: **17 lần gọi trên 16 dòng** (plan ghi 16; `ShakeCamera` có 2 lần trên 1 dòng;
  `GridBuildingSystem` ở dòng 191 chứ không phải 188). Đường per-frame duy nhất:
  `BuildingGhost.cs:132,160` → `BuildingPlacementManager.cs:107` (2 lookup/frame khi kéo ghost);
  `UtilsClass.cs:603` `ShakeCamera` 2 lần/frame khi rung; còn lại theo click/touch.
- **Đã làm:** `UtilsClass.MainCamera` (static, lazily cache `Camera.main`, cùng pattern
  `LookAtCamera.cs:16-17` / `Window_QuestPointer.cs:31-32` từ commit `bcb3d4b6f`); 11 lần trong
  `UtilsClass.cs` → `MainCamera`; `BuildingPlacementManager.cs:107`, `GridBuildingSystem.cs:191`,
  `PlayerStateMachine.cs:170`, `GameInput.cs:188`, `DOTweenHandClick.cs:93`, `Button_Sprite.cs:155`
  → `UtilsClass.MainCamera` (3 file thêm `using CodeMonkey.Utils;`); `ShakeCamera` gộp thành 1 biến
  `cam` + guard null. Không đổi chữ ký public.
- Kiểm 2026-11-14 (`optimization_plan` §3.5): dưới `Assets/Scripts` chỉ còn cache helper
  `UtilsClass.cs:32`, `Window_QuestPointer.cs:32`, `LookAtCamera.cs:17` và comment
  `CameraController.cs:142`.
- **Kiểm chứng:** 0/0, 49/49, 110 assemblies; `UtilsClass.MainCamera == Camera.main` ở MainMenuScene và
  GameScene, `nearClipPlane = 0.01`; raycast hai camera `sameHit=True` (`ClearCounter(Clone)`); click
  thật đặt tomato lên counter qua `PlayerStateMachine.cs:171`; kéo ghost bằng chuột PASS (mục 16).
- `ShakeCamera` **không dùng trong gameplay** (chỉ `Tools/Profiling/S10 - ShakeCamera 10s`); trong
  GameScene `CinemachineBrain` ghi đè transform. Kiểm tách rời 2026-10-20 (tắt `CinemachineBrain` +
  `CameraController`, `ShakeCamera(0.5f, 60f)`): 30/30 vị trí khác nhau, biên độ `x ≈ 0.993`,
  `y ≈ 0.997`, `z = 0`, không lệch dần.
- **Không phải tối ưu có số đo** — p95 đã trong ngưỡng; không có so sánh frame-time trước/sau.
- Rủi ro còn lại: nếu sau này đổi camera runtime thì thêm `InvalidateMainCameraCache()`.

### 06 — Cache `PlayerStats` · ⚠️ Không làm

- `GameData.GetPlayerStatsById` đã bỏ closure (commit `288f03fd6`, vòng `for`) → 0 GC.
- `PlayerStateMachine.cs:120-128` `Update()` có `if (!IsOwner) return;` → 1 lần/frame;
  `GetOwnerPlayerId()` (`:438-445`) không alloc khi là owner; list ≤ 4 (`MAX_PLAYER_AMOUNT = 4`).
- Còn lại `GetOwnerStats()` (`:447-453`): 2 đọc static + quét ≤4 phần tử/frame. Cache reference sẽ
  phải invalidate ở `UnsubscribeFromOwnerStats()` (`:392-399`), `OnPlayerStatsAdded`, và khi stats bị
  remove (`TryAddPlayerStats` tạo `new PlayerStats`) — rủi ro stale reference.
- S2 (đứng yên) chỉ 2.92–3.55 ms main thread cho **toàn bộ** frame → **KHÔNG LÀM**.

### 07 — `price`/`exp` món Stew · ✅

- `StewSO.asset` (`_guid: 62129fd5079c12a43a0ee71c1e752c79`) có `price: 0`, `exp: 0`,
  `unlockPrice: 60`, `unlockLevel: 6` — FoodSO duy nhất có 0. `BotCustomerController.cs:409-423`
  `FinishEating()` cộng `food.price`/`food.exp` → khách trả $0 / 0 XP; UI hiện `$0` ở
  `UIFoodItem.cs:41-42`, `UIMenuItem.cs:19-20`, `UIFoodDetailPopup.cs:52-53`.
- So sánh: Simple Salad 5/10 (lv1), Onion Salad 10/15 (lv2), Cheese Hamburger 20/25 (lv3),
  Hamburger Full 30/30 (lv4), Vegetable Hamburger Full 30/35 (lv5).
- **Đã sửa:** phương án A `price = 35`, `exp = 40`; **chủ dự án chốt giữ nguyên 2026-10-21**. Không đổi
  `_guid`. Thêm: `kitchenObjectSOList` → `[StewCookedSO]` (xem mục 21).
- **Kiểm:** 2026-10-12 menu popup `Stew — $35 / 40 XP` (log `[Perf] stew price=$35 exp=40
  unlockLevel=6`); 2026-10-21 bot order Stew → phục vụ bằng **bát** → `Eating` → `eaten=True`,
  `pendingCash=54`, `pendingExp=40` → collect `Coins 200→254`, `Exp 0→40`. 54 = 35 + tip 19 (tip gốc
  0.7, bị 1 bậc Sad còn 0.56). Chạy lại sau reimport asset vẫn đúng.
- Giới hạn: bot đặt thẳng vào ghế + state `OrderFood`; order/serve/collect qua `InteractEvent`, không
  qua click (S3 kiểm click riêng). Stew phải phục vụ trong bát, không phải đĩa.

### 08 — `StewBurnedSO.objectName` · ✅

- `objectName: Stew` → **`Stew Burned`** (tiền lệ `Burger Trash`, `Burger Cooked`…); **chủ dự án chốt
  giữ nguyên 2026-10-21**. Không đổi `_guid`, `prefab`, `Sprite`, `m_Name`.
- `objectName` chỉ dùng hiển thị: `UIFoodDetailPopup.cs:70`, `OptionMenuItemUI.cs:19`.
- Kiểm: 2026-10-12 option menu hiện `Stew Cooked` / `Stew Burned`; 2026-10-21 nấu thật
  `Potato Chopped` → `Cooking` (`cookMax = 5`) → `Stew Cooked` → `Stew Burned`
  (`stew_bowlBurned(Clone)`, burn timer `5.00`).

### 09 — Ghim số enum `ShopItemCategory` · ✅

- Unity serialize enum thành **số** (asset/prefab/scene/mạng). `ShopItemType` đã ghim (comment: chèn
  `Ingredient` May 2026 làm dịch dữ liệu Gem/Coin) và còn đi qua RPC: `GameManager.Economy.cs:118`
  `ExchangeCurrencyServerRpc`, `:260` `ClaimDailyFreeServerRpc`, `:282`
  `IncreaseDailyFreeCountClientRpc`; serialize ở `ShopManager.cs:8`, `UICurrencyItem.cs:8`,
  `UIDailyFreeItem.cs`.
- **Đã làm:** ghim `Counters = 0 … Meats = 7`, `None` dời `8` → **`9999`**; `ConfigShopSO.asset`
  migrate tay 3 dòng `category: 8` → `9999` (không regenerate bằng `Tools/ConfigShop/Create
  ConfigShopSO` để giữ chỉnh tay trong Inspector). `ConfigShop.csv` lưu **tên** nên không đổi;
  `ShopItemCategory` không đi qua RPC. 0/0, 49/49.
- Kiểm 2026-10-12: shop tab Restaurant vẫn có nhóm `None`; `UIShopItem.GetReward` `default:` là no-op
  → đổi `8`→`9999` chỉ đổi nhãn nhóm.
- Nơi đọc `Category`: `UIShopPopup.cs:67,78` (`ToLookup`), `:102,121` (`ScrollTo`/`GetUIShopItem`,
  dùng ở `BuildingTutorial.cs:35,51`), `UIShopItem.cs:102-121`.
- **Quy tắc enum (còn hiệu lực):**
  - Không bao giờ reorder/renumber giá trị enum đã serialize; chỉ **append**.
  - Category mới append ngay sau `Meats` với số kế tiếp (vd `Drinks = 8`), luôn **nhỏ hơn**
    `None = 9999`. **Cấm dùng số ≥ 9999.**
  - Không viết custom drawer serialize enum thành chuỗi (đổi format mọi asset, lợi ích thấp).
- Bước 2 (`[InspectorName("Counters (0)")]`… trong `ShopItemCategory.cs`) và Bước 3 (guard lúc build:
  `PinnedShopEnumGuard : IPreprocessBuildWithReport` trong `Assets/Editor/BuildChecks.cs`, kiểm cả
  `ShopItemCategory` lẫn `ShopItemType`; chạy tay: `Tools/Build Checks/Verify Pinned Shop Enums`) **đã làm**
  (`217875b19`; plan gốc ghi "chưa làm" là cũ). Đã có thêm `Tools = 8`. Thêm giá trị enum → append vào
  danh sách ghim trong `BuildChecks.cs` cùng commit.

### 10 — Hai pattern nạp dữ liệu counter · ✅

| Controller | Nội dung | Mạng | Cách biết SO |
|---|---|---|---|
| `ClearCounterController` | 1 món | object con networked | B: `KitchenObject.GetKitchenObjectSO()` |
| `TrashCounterController` | xoá món | — | không cần |
| `ContainerCounterController` | 1 slot | `NetworkVariable<ContainerDataSerializable>` | A: GUID → `KitchenObjectSODic` |
| `OptionalContainerCounterController` | N slot | `NetworkList<ContainerDataSerializable>` | A |
| `TablewareCounterController` | luôn ra bát/dĩa | không state | B: `[SerializeField] KitchenObjectSO` (`:5`) |
| `CookingToolCounterController` | loại công cụ | tool của `CookingTool` | enum list baked (`:7`) |
| `PostBox` | 1 kiện | theo GUID kiện | A nội dung + B `postBoxKitchenSO` (`:10`) |

- **Kết luận:** hai pattern đều đúng, không phải nợ kỹ thuật. Chuẩn hoá Tableware sang A = thêm state
  mạng, thêm đường sync, đổi wire format, không thêm giá trị.
- **Đã làm:** guard null trong `TablewareCounterController.InteractEvent` →
  `Debug.LogError("TablewareCounterController: _tablewareKitchenObjectSO is not assigned!", this)`
  (trước đây `KitchenGameManager.cs:163` trả `null` im lặng); comment quy ước trong file; quy ước dán vào
  `docs/datamanagement.md` §4.7 (2026-10-20). 0/0, 49/49.
- **Quy ước (còn hiệu lực):** *Counter có nội dung thay đổi lúc chạy (fridge/refillable) → pattern A
  (`PlacedObjectView` GUID + `NetworkVariable`, xem `BaseContainerCounterController`). Counter nội dung
  cố định → pattern B (SO baked trên prefab) + guard null.*

### 11 — Vòng `KitchenObjectSO` ↔ prefab · ⏸ Hoãn tới Addressables

- `PlacedObjectTypeSO` ↔ prefab: **không vòng** — `PlacedObjectView.cs:15` không `[SerializeField]`,
  gán lúc chạy qua GUID (`:25`, `:97`).
- `KitchenObjectSO.prefab` → prefab → `[SerializeField] kitchenObjectSO` (`KitchenObject.cs:27`):
  **có vòng, và bắt buộc** — `KitchenGameManager.cs:167` spawn không gán lại SO. Bỏ serialize mà không
  gán → `GetKitchenObjectSO()` null ở `TablewareKitchenObject.cs:70`,
  `CookingToolCounterController.cs:108`, `ClearCounterController`, `FoodCompleteVisual.cs:60`.
- Không vòng: `PostBox.postBoxKitchenSO` (`PostBox.cs:10`), `TablewareCounterController` (`:5`),
  `acceptedToolTypes` (enum).
- **Khuyến nghị:** giữ nguyên (chạy đúng). Khi làm Addressables tách group thì phá vòng: bỏ
  `[SerializeField]`, thêm `SetKitchenObjectSO`, gán ngay sau `Instantiate` ở `KitchenGameManager.cs:167`,
  audit mọi prefab có `KitchenObject` và mọi đường spawn không qua `SpawnKitchenObject`; kiểm late joiner.

### 12 — Tra GUID shop reward · ✅

- Ba nguồn khác namespace: `ConfigShop.ShopItems` (list), `GridBuildingSystem.PlacedObjectTypeSODictionary`
  (`:58`, dựng `:86-89`), `KitchenGameManager.KitchenObjectSODic` (`:38,47,54-57`);
  `ShopItemCategory` là cờ phân nhánh. Không gộp 2 dictionary (mất an toàn type).
- Vấn đề cũ ở `UIShopItem.cs:96-133` `GetReward`: linear `PlacedObjects.Find` và **fail im lặng**
  (ô shop biến mất, không log). Server `GameManager.Economy.cs:84-115` đã ổn; `ShopManager.OnPurchase`
  chỉ gửi `item.Id`.
- **Đã làm:** Bước 1 dictionary (`217875b19`; `UIShopItem.cs:108-109`
  `PlacedObjectTypeSODictionary.GetValueOrDefault`, không còn `PlacedObjects.Find` trong
  `Assets/Scripts`); Bước 2 `LogError` có guid + tên + category (`572f37092`); Bước 4
  `ConfigShop.GetById` (`217875b19`; `GameManager.Economy.cs:88-89`). Bước 3 `ShopRewardResolver`
  chung — **chưa làm, tùy chọn**.
- **Test neg (2026-10-20, chỉ sửa trong bộ nhớ):** guid `deadbeef…` trên ô Fridge →
  `UIShopItem: no PlacedObjectTypeSO for shop reward guid '…' (Fridge / Counters)`, ô ẩn;
  `cafebabe…` trên Peach → `no KitchenObjectSO … (Peach / Vegetables)`, ô ẩn. `git status` sạch dưới
  `Assets/SO`/`Assets/Configs`.
- **Ô ẩn "dính" session — đã sửa 2026-10-21:** `UIShopPopup.ShowPopup` chỉ gọi `Refresh()` (items build
  một lần) nên ô bị ẩn không tự hiện lại (tab Restaurant còn 12/13). Nay `UIShopItem.Refresh()`
  `SetActive(true)` rồi `GetReward(configShopItem.Rewards)`; `SetItem()` chỉ gọi `Refresh()` một lần.
  Kiểm: ẩn `Clear Counter` bằng guid rác → mở lại shop → active, 0 error mới. Popup giữ 44 ô, 35
  active; 9 ô inactive là template (`configShopItem == null`).
- Perf: mở shop chỉ +0.4–0.7 ms main thread so S2 → phần perf không đáng, phần `LogError` mới là giá trị.

### 13 — Profiler baseline · 🟡

Rủi ro 0 (chỉ đo; riêng lượt S8 2026-10-25 có sửa `Player.prefab`). Trước đó các tối ưu boxing,
`ToList()`, `Camera.main` 2 class, closure `GetPlayerStatsById`, hash `Animator` (commit `4089f0584`,
`bcb3d4b6f`, `288f03fd6`) đều suy luận, không đo.

**Kịch bản**

| # | Kịch bản | Kiểm cho mục |
|---|---|---|
| S1 | MainMenu đứng yên | baseline rỗng |
| S2 | Vào game 1 player, đứng yên 30 s | `PlayerStateMachine.Update` (06) |
| S3 | Đi lại + click counter | `HandleMovement`/`HandleInteractions`, `Camera.main` click (05) |
| S4 | Kéo building ghost 30 s | `BuildingGhost.Update` (05) |
| S5 | Mở shop, lướt mọi tab | linear `Find` (12) |
| S6 | Menu popup + bấm mọi category 10 lần | Instantiate/Destroy (04) |
| S7 | Chạy bếp 3 phút (nấu, cắt, phục vụ bot) | `KitchenObject`, `CookingTool`, `BotCustomerController` |
| S8 | 4 player (MPPM) | JSON join (02), RPC (01, 03) |
| S9 | Late join | `SyncDataToNewClientServerRpc` — thời gian + `chunkCount` |
| S10 | `ShakeCamera` + rung | `UtilsClass.cs:603` (05) |

**Ngưỡng quyết định (chốt trước khi đo)**

| Nếu thấy | Thì |
|---|---|
| p95 > 33 ms ở S3/S7 | ưu tiên 05 + 04 |
| GC > 1 MB/s ở S6 | làm 04 |
| `FindGameObjectsWithTag` trong S4 | làm 05 |
| Join (S9) > 1 s hoặc `chunkCount` > 5 | làm 02 |
| Bandwidth S8 > 100 KB/s/client | làm 01 + 03 |
| p95 < 16 ms, GC < 100 KB/s | **không làm 01–06** |
| `List<T>.Find` ở S5 < 0.1 ms | bỏ phần perf của 12, giữ log lỗi |

**Số đo chính**

| Kịch bản (ngày) | Kết quả |
|---|---|
| S1 (10-12) | CPU frame 8.36 ms, main **1.30**, render 0.89 |
| S2 (10-12) | frame 8.86–10.86, main **2.92–3.55**, render 1.23–1.71 |
| S5 (10-12) | frame 10.46–13.10, main **3.57–3.99**, render 1.49–2.32 |
| S6 (10-12) | 10 lần mở + 55 click: frame 9.77–13.26, main **3.27–4.59**, render 1.49–2.13 |
| S10 (10-12) | frame 10.59–14.85, main **3.33–4.51**, render 0.22–2.12 |
| S3/S4-thử computer-control (10-13) | ❌ không hợp lệ: player không di chuyển; không xác nhận `isDragging` |
| S4 (10-20) | kéo ghost bằng inject pointer PASS (mục 16) — kiểm logic, không có số frame |
| S3 (10-21) | inject `W`: `(-3,0,-3)` → `(-3,0,7.91)` (10.91 m); click `(690,538)` → `PlateTablewareCounter(Clone)`, `plate(Clone)` vào tay |
| S7-rút gọn (10-21) | 60.0 s / 7689 frames, p50 **7.50**, p95 **8.45**, p99 10.73, max 1582.11 ms (1 stall), GC 3722.4 KB/s, 5 bot; không có nấu/cắt/phục vụ |
| S7 đầy đủ (10-22) | 60.0 s / 6223 frames, p50 **9.48**, p95 **11.70**, p99 14.51, max 78.35 ms, GC 3090.1 KB/s, 5 bot, 5/5 vòng nấu/cắt/phục vụ/thu |
| S8/S9 2-player (10-23) | late join **3.77 s** (sign-in cached 0.37 + join 3.40); snapshot 760 chars / 449 B / 1 chunk |
| S8 4-player (10-23) | snapshot 990/1139/1288 chars, 496/523/549 B, 1 chunk; phase cũ 1.251 s; bandwidth không thao tác ~1.9 KB/s/client, ~8 pps |
| S8 4-player movement (10-26) | sai lệch vị trí lớn nhất **0.005 m** giữa 4 editor |
| Bandwidth movement (10-26) | client **18.6–20.9 KB/s** tổng hai chiều; host 50.87/13.98 Tx/Rx |
| Bandwidth cooking (10-26) | client **3.4–5.1 KB/s**; host 10.14/2.51 |
| S9 tách phase (10-26) | data sẵn sàng **0.541 / 0.578 s**; JSON+decompress+replace **53–62 ms**; `OnDataSync` **0.995 / 1.066 s**; 4 chunk OK |

GC của S7 là **Editor + probe** (`ProfilerRecorder` `GC Allocated In Frame` gồm allocation Editor; probe
tạo `KeyboardState`/`List<Key>` mỗi frame) → không dùng để kết luận. S7 là 60 s chứ không phải 3 phút,
thao tác bằng code.

**Kết luận theo mục:** 05 — S10 không khác S2 nhưng đo S10 chứ không phải S4, Editor che chi phí
10–50 µs, GC chưa đo; ưu tiên thấp nhưng đã làm vì rẻ (`BuildingGhost.HandleDraging():130` return sớm khi
không kéo). 04 — mở Shop/Menu chỉ +0.4–1.0 ms; cần số GC trước khi làm tiếp. 12 — bỏ phần perf, giữ
`LogError`. 06 — KHÔNG LÀM. 02 — KHÔNG LÀM. 01, 03 — chưa sửa, không vượt ngưỡng.

**Phát hiện phụ:** `Tools/Profiling/Enter Game (new host)` đi đúng đường production (`NewGame` →
`StartHostSessionAsync` → `Loader.LoadNetwork` → `SwitchState(InGameState)`, như `UIMainMenuPopup.cs:20-39`;
vào `GameScene` ~20 s). S6: `ShowPopup` 10 lần trong 1 frame sinh 55 `UIMenuCategoryItem` (Destroy hoãn
cuối frame) — đã sửa ở mục 04. `MultiplayerManager.StartSinglePlayer()` (`:245`) không được gọi ở đâu —
**code chết**. Warning `Look rotation…` — đã sửa ở mục 18.

**Chưa đo:** GC KB/s trên Player build (cần Development Build + Profiler UI hoặc đọc `.raw`);
bandwidth và S9 trên Android/Player build. Số Editor luôn đẹp hơn máy thật.

**Cạm bẫy đo/automation (giữ để lần sau khỏi vấp):**

1. `enable_callstacks=true` + ghi liên tục = **5.5 GB trong ~40 s** → chỉ bật callstacks cửa sổ ngắn.
2. `get_counters` qua MCP hỏng khi truyền mảng `counters`; không truyền thì payload hàng MB → dùng
   `get_frame_timing`. Category hợp lệ (không có `CPU`): `Render, Scripts, Memory, Physics, Physics2D,
   Animation, Audio, Lighting, Network, Gui, UI, Ai, Video, Loading, Input, Vr, Internal, Particles,
   FileIO, VirtualTexturing`.
3. Unity hoãn recompile khi đang Play → phải `stop` rồi mới `refresh_unity`.
4. `find_gameobjects` `by_name`/`by_path` trả rỗng, chỉ `by_component` chạy; resource components
   throw; không với tới `DontDestroyOnLoad`.
5. Toạ độ khớp ≠ click đã ăn — kiểm bằng tín hiệu quan sát được (text, transform, log).
6. Chưa xong tutorial (`GameMachanic`, 8 bước) thì mọi lượt đặt bị từ chối: `NewGame()` tạo
   `widthMin = heightMin = 0`; ô chỉ mở ở `TutorialManager.cs:112` → `UnlockGrid()`.
7. `EditorApplication.update` không tick trong play mode; gamepad giả chập chờn → nếu teleport bằng
   `transform.position` + `Physics.SyncTransforms()` thì phải ghi rõ.
8. Gọi `StartHostSessionAsync()` quá sớm race với auto sign-in (`SessionManager.cs:82-93` nuốt
   exception) — không phải lỗi Netcode.
9. `FindFirstObjectByType<CookingToolCounterController>()` có thể trả controller của
   `BuildingGhostPreview` (layer 7) → lọc `NetworkObject.IsSpawned`, kiểm `hit.collider`.
10. Bot đứng giữa tia raycast → log tên collider trúng trước khi kết luận.
11. Menu item có `/` bị chuẩn hoá khoảng trắng qua MCP → đừng dùng làm khoá chính xác.
12. InputAction không actuate khi editor mất focus → công thức ở mục 16.
13. Đọc `Pointer.current.position` từ `execute_code` giữa 2 frame thấy giá trị native cũ → log trong
    `Update()`.
14. Prefab có component MISSING: đừng tin class name trong YAML; kiểm
    `AssetDatabase.GUIDToAssetPath(guid)` + load prefab.

### 14 — Runtime ghi vào `GridShaderMaterial.mat` · ✅

- `GridBuildingSystem.cs:26` `[SerializeField] Material gridGuideMaterial` → `Assets/Shaders/
  GridShaderMaterial.mat` (guid `3077f760661a789459e2a175cc9eac66`, object `GridGuide` trong
  `GameScene` và `TestScene`); `GridVisualizer.cs:21-29` `SetFloat/SetVector` lên **chính asset** →
  dirty → `SaveAssets`/`Ctrl+S` ghi xuống repo. Đã thấy `5/5.01` → `0/0.01`; hai scene ghi đè nhau.
- **Sửa:** `GridVisualizer` dùng `new Material(gridGuideMaterial)` gán cho renderer, implement
  `IDisposable` (`Destroy` khi play, `DestroyImmediate` khi không); `GridBuildingSystem.OnDestroy()`
  gọi `Dispose()`. Không đổi tên field, asset, prefab/GUID, property shader.
- **Kiểm:** 0/0, 49/49, 110 assemblies; renderer dùng material `isAsset=False`; `UnlockGrid(3, 4)` →
  instance `3/4`, `3.01/4.01`, asset vẫn `5/5.01`, `IsDirty = False`; SHA256 `.mat` sau play
  `A2F9F5F418951676BE07FF8187A28EF4B0E38688B3C55512CAA8D98A17229899` giống trước.
- Chưa kiểm hình trên build Android. Rủi ro: nếu field trỏ material khác renderer thì nay renderer hiển
  thị theo field (chấp nhận).

### 15 — Cắt trên thớt không bấm được · ✅

- `PlayerStateMachine.cs:196-217`: chỉ khi `SelectedInteractableObject as IHasProgress` khác null và
  `GetProgress()` ≠ -1, chưa `IsDone()` thì mới `RequestInteract(…, true)` → `InteractAlternateEvent`
  (đường duy nhất, `:473-474`). `CookingToolCounterController.cs:5` có `GetProgress()` (`:163`),
  `IsDone()` (`:169`) nhưng **không khai báo** `IHasProgress` → click lần 2 nhấc tomato trả lại tay
  (`:146-150`) → không có slices → không nấu/phục vụ → **không lên Level 2**.
- Hồi quy từ refactor MVC `9e3c5fe4e` (`CuttingCounterController`/`StoveCounterController` cũ đều có
  `IHasProgress`).
- **Sửa (`caddb06fe`):** thêm `IHasProgress`; `GetProgress()` trả **-1** khi
  `!TryResolveTool() || !_cookingTool.HasKitchenObject() || !_cookingTool.IsCuttingActive` (nếu không,
  nồi/chảo có đồ trả 0 → đi alternate → không lấy lại được đồ). `IsDone()` giữ nguyên.
- **Kiểm (đi đúng thứ tự mở khoá):** tutorial 8 bước → mua Container (10) + Clear (30) + Cutting Board
  (40) + Tomatoes (15), coin 200 → 105 → đặt 3 counter → click 1 đặt tomato; click 2–4 cắt
  (`CookingTimer` 3/4/5, `GetProgress` 0.6/0.78/—, `cuttingProgressMax = 5`); click 5 (`-1`) nhấc
  `food_ingredient_tomato_slice`. 0/0, 49/49, 110 assemblies. Mua/đặt/spawn tomato bằng API, click
  phát qua reflection nhưng raycast từ toạ độ collider thật.
- **Còn lại:** `InteractAlternateEvent` của `TablewareKitchenObject.cs:113` và `PostBox.cs:65` vẫn
  không tới được (chưa xác định hành vi mong muốn). Tool vừa cắt vừa nấu sau này: `IsCuttingActive` ưu
  tiên cắt — cần xem lại.

### 16 — Tự động hoá input bằng Input System · ✅

Không đổi code runtime. **Nguyên nhân** inject trước đây thất bại: editor mất focus →
`Application.isFocused == false` → `InputManager.defaultUpdateType` = `Editor` → device state đổi
nhưng InputAction không actuate (`pan=(0,0) activeCtrl=null ctrlRead=(400,300)`). Theo source
`com.unity.inputsystem`: `gameHasFocus => runPlayerUpdatesInEditMode || applicationHasFocus ||
gameShouldGetInputRegardlessOfFocus || …`; `gameShouldGetInputRegardlessOfFocus` cần `IgnoreFocus`
**và** `AllDeviceInputAlwaysGoesToGameView`.

**Công thức (còn hiệu lực cho mọi probe input):**

```csharp
[DefaultExecutionOrder(-1000)] // Update chạy trước BuildingGhost / GameInput / PlayerStateMachine
class TmpProbe : MonoBehaviour {
  void Awake() {
    InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
    InputSystem.settings.editorInputBehaviorInPlayMode =
        InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    var im = typeof(InputSystem).GetProperty("manager",
        BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
    im.GetType().GetProperty("runPlayerUpdatesInEditMode").SetValue(im, true);
  }
  void Update() {
    InputState.Change(Mouse.current, new MouseState { position = Position, buttons = Press ? (ushort)1 : (ushort)0 });
    InputSystem.Update(); // action phát `performed` ngay trong frame này
  }
}
```

- Click tổng hợp (S3): `Pointer.press` một mình không đủ → rebind runtime action `Click` sang
  `<Mouse>/leftButton` (`ApplyBindingOverride`). Action có thể kẹt phase `Performed`/`Waiting` → release
  + `InputSystem.Update()` (hoặc reset action) trước click kế. Đừng suy "đã click" từ
  `LastClickPosition` (pan cũng cập nhật).
- Toạ độ ghost: dùng `Collider.bounds.center` của preview, không dùng `visual.position` (góc dưới model).
  `GetMouseWorldSnappedPosition()` clamp theo vùng ô mở khoá (target xa đều về `(8,0,8)`).
- Probe (`TmpPointerProbe.cs`, `TmpInputProbe.cs`) chỉ tồn tại lúc kiểm, **xoá kèm `.meta`**.
- **Kết quả kéo ghost (2026-10-20, host thật):** bấm tại collider → `ghost.drag=True`,
  `LastClickPosition=(1230.00, 927.70)`; ô (2,2) → `visual=(4.00, 1.00, 4.00)`; ô (0,0) →
  `(0.00, 1.00, 0.00)`; thả → `drag=False`; Confirm qua `EventSystem` → `ClearCounter(Clone)`
  spawned ở `(0,0,0)`. Chuỗi: `MouseState → Pan/Click → GameInput.OnMouseClickPerformed →
  BuildingGhost.OnPanStarted → HandleDraging → GetMouseWorldSnappedPosition → OnClickConfirm →
  PlaceObjectServerRpc → spawn`.
- Giới hạn: pointer tổng hợp, không phải tay người; host đơn; touch kiểm riêng ở mục 20.

### 17 — Build APK + 2/4 client · 🟡

Không đổi runtime ngoài fix `Player.prefab` (§17.5).

**17.1 Bộ kiểm tiêu chuẩn:** refresh force 32 s OK; console 0/0; EditMode **49/49** (`0.236 s`; chạy lại
2026-10-26 sau fix prefab + cleanup: 49/49, `1.16 s`); `[BuildChecks] Android player scripts compiled:
110 assemblies.`

**17.2 APK — chưa có APK nào (`size=0.0 MB`):**

| Lần | Cấu hình | Kết quả |
|---|---|---|
| 1 | custom keystore như repo (`androidUseCustomKeystore: 1`, `D:/Unity/KeyStore/MyRestaurant.keystore`, alias `dev`) | `Unable to sign the Android application` — không có mật khẩu trong repo (đúng, secret không nằm trong git) |
| 2 | tạm debug keystore (đã khôi phục `true`) | qua bước ký, dừng sau 6:07 — `Required API level(s): 35.` (thiếu Android SDK platform 35) |

Cần: cài SDK platform 35 + mật khẩu keystore/alias (hoặc chấp nhận debug-signed). Build ghi đè
`ProjectSettings.asset`, `GraphicsSettings.asset`, `UniversalRenderPipelineGlobalSettings.asset`,
6 `Assets/URPDefaultResources/*.asset`, `Roboto-Bold SDF.asset`, sinh
`Assets/Resources/PerformanceTestRun*.json` — **đã revert/xoá hết**.

**17.3–17.6 Multiplayer (Editor + MPPM virtual Editor, Relay thật):**

| Ngày | Lượt | Kết quả |
|---|---|---|
| 10-23 | 2 player, `Player 2` clone `Library/VP/mppm4214af7c`, save `S8PostReset` | host `clients=2/players=2`; late join **3.77 s**; snapshot 760 chars / 449 B / 1 chunk; P2 0/0 |
| 10-23 | 4 player (`mppm4214af7c`, `mppm5d14303d`, `mppmd382a7e5`, profile `s8_player2/3/4`), save `S8Four` | `clients=4/players=4`; snapshot 496/523/549 B, 1 chunk; phase 1.251 s; host Tx/Rx 1.12/0.81 KB/s, 8.50/7.16 pps; P2 1.07/0.81, P3 0.34/0.41, P4 0.36/0.46 KB/s |
| 10-25 | 4 player join/visual, save `S8Verify3` | `clients=4/players=4`, `UIMainMenuPopup(Clone)` hide, 3 client 0/0 |
| 10-25 | 2 player sau fix sync, save `S8Move` | host→P2 `(1.500,0,-3.000)` thấy `(1.495,0,-3.000)`; P2→host `(0,0,0.500)` thấy `(-0.004,0,0.496)`; joystick tay hai bên chạy theo |
| 10-26 | 4 player movement (probe vòng tròn 8 s), join code `LKNP6R` | đích owner 0–3 `(-3,0,-3)`, `(-3,0,-1)`, `(-3,0,1)`, `(-3,0,3)`; sai lệch ≤ **0.005 m** |
| 10-26 | snapshot probe cũ (chưa tách event) | P2/P3/P4 **0.994 / 1.226 / 1.285 s**, TB 1.168, trung vị 1.226 |

Bandwidth 2026-10-26 (UTP `NetworkDriver.GetStatistics()`, Tx/Rx KB/s):

| Editor | Di chuyển (12 s) | Nấu (10 s, 8 chu kỳ `PotatoSlices`, restart 5.5 s < 6 s) |
|---|---|---|
| Host | 50.87 / 13.98 (297.9 / 98.6 pps) | 10.14 / 2.51 (59.6 / 20.0 pps) |
| P2 | 7.93 / 12.54 | 0.82 / 2.57 |
| P3 | 8.36 / 12.57 | 0.88 / 3.75 |
| P4 | 7.39 / 11.23 | 0.93 / 4.17 |

Workload nấu là spawn/despawn + NetworkVariable, chưa phải full serve/cắt. Tất cả dưới 100 KB/s/client.

**17.6.5 Tách snapshot phase (instrumentation tạm trong `MultiplayerManager.cs`, không commit):**

| Mốc | P2 — thường | P3 — 601 menu |
|---|---:|---:|
| Compressed / chunks | 483 B / 1 | 12,685 B / 4 |
| `GameScene` → `GameData != null` | **540.988 ms** | **577.582 ms** |
| JSON deserialize | 49.489 | 55.385 |
| Giải nén | 2.570 | 4.517 |
| Ghép chunk | 0.018 | 0.035 |
| `ReplaceGameDataFromHost` | 1.062 | 1.871 |
| `OnDataSyncToNewClient` | **994.972** | **1,066.094** |
| `GameScene` → sau event | 1,535.980 | 1,643.697 |

P2 connected ở scene + 228.489 ms, first chunk + 486.567 ms, first chunk → `GameData` 54.421 ms.
P3 `jsonChars=22090`, `gzip=12685`, `chunks=4`; server `jsonMs=0.459`, `compressMs=0.884`,
`sendMs=0.520`; sentinel `S8_MULTI_CHUNK_SENTINEL_9f3a2c`.

**Bug phát hiện + fix:**
- **Sync vị trí:** `Player.prefab` tham chiếu `ClientNetworkTransform` GUID
  `da52bf8bbc1de48cfb221a6ff30f7972` không còn asset → component MISSING, player chỉ di chuyển local.
  Thay bằng `Unity.Netcode.Components.NetworkTransform` `AuthorityMode=Owner`, sync position `X/Y/Z`,
  rotation `Y`, `Interpolate=true`; giữ `NetworkObject`, `PlayerStateMachine`, `PlayerIKHandler`,
  `NetworkAnimator`. Đừng tạo lại script mẫu đã mất.
- **Rejoin:** `ShutdownAndReset()` trên client → `playerDataNetworkList.Clear()` báo
  `Write permissions (Server)`; fix `f2240ca4d` (chỉ host clear); flow leave chuẩn
  (`ShutdownAndReset()` + `Loader.Load(MainMenuScene)`) 0/0. Bỏ số rejoin 1.082 / 2.976 s (flow không
  chuẩn).
- NRE `GameInput.IsMouseOverUI` (`Touchscreen.current == null`) đã thêm null guard. Lỗi read-only/shader
  do cache `Library` lệch → reset `Library`.
- Host có NRE nội bộ MPPM `MainPlayerSystems.Listen` (`UpdateCloneLogCountsMessage`) — không phải game code.

**Tutorial khi test S8:** `GameMechanicPanel` chặn, 8 bước, `closeButton: 0`. Gọi `CloseTutorial()`
**trước** (để `TutorialManager_OnTutorialGameMechanicClosed` thấy `HasPlayedFirstTime == false` →
`UnlockGrid()` + set flag), rồi `UIHUDManager.ShowAllUIElement()` + `GameManager.ShowJoyStick()`.
**Không set `HasPlayedFirstTime=true` trước `CloseTutorial()`.** Production vẫn 8 lần Next.

**Số không dùng:** 5.45 s / 909 chars / 484 B (clone `mppm159b293e` trước reset `Library`, sign-in lạnh)
chỉ tham chiếu; 7.85 s / 449–463 B (clone tay `S8Test`) bỏ.

**Còn:** APK; GC + bandwidth trên Android/Player build; joystick tay mới chỉ 2 player.

### 18 — Warning `Look rotation viewing vector is zero` · ✅ (2026-10-21)

- `PlayerStateMachine.HandleMovement()` luôn gán `transform.forward = Vector3.Slerp(transform.forward,
  moveDir, rotateSpeed * Time.deltaTime)`. Khi đứng yên `moveDir == zero`, `Slerp` trả đúng zero khi
  `t >= 1` (`Time.deltaTime >= 0.1f`, frame ≥ 100 ms) → `LookRotation(zero)` → warning theo từng hitch.
- **Sửa:** bọc trong `if (Context.IsWalking)` (đã tính `moveDir != Vector3.zero`; bị tường chặn vẫn quay).
- Kiểm: đứng yên 8 s → 0/0; gọi biểu thức cũ với `t = 1` → warning bắn đúng; 49/49; 110 assemblies.
  Chưa dựng lại hitch ≥ 100 ms trong lúc đi theo đường production.

### 19 — 3 subscriber `OnMouseClickPerformed` · ✅ Không sửa

| Đăng ký | Việc | Cắt? |
|---|---|---|
| `BuildingGhost.cs:31` → `OnPanStarted` | bắt đầu kéo ghost (thoát nếu không đặt object) | Không |
| `GridBuildingSystem.cs:161` → `GameInput_OnMouseClickPerformed` | chọn/bỏ chọn object đã đặt (thoát nếu không build mode) | Không |
| `PlayerStateMachine.cs:104` → `PlayerStateMachine_OnMouseClickPerformed` | raycast, `RequestInteract` 1 lần (`:462` → `InteractServerRpc`) | **Chỉ đường này** |

- Đo (probe đếm event, công thức mục 16): mỗi lần nhấn = **1** event; 3 nhát cắt → `CookingTimer`
  0→1→2→3, mỗi nhát 1 event. "3 nhát cắt" trước đây = 3 click.
- **Quy tắc:** đếm nhát cắt bằng số click hoặc `CookingTimer`, đừng suy từ số subscriber.
- Giới hạn: host đơn, input inject; chưa đo 2 client.

### 20 — Kéo ghost bằng touch · ✅ (2026-10-21)

- Trên mobile `Pointer.current` là `Touchscreen`; một điểm chạm vào qua state event `TouchState`
  (`Touchscreen.OnStateEvent` ghép vào `touches[i]` + `primaryTouch`), không phải `TouchscreenState`.
- **Cách dựng:** `Tools/Profiling/Enter Game (new host)` + xong tutorial; `InputSystem.AddDevice<Touchscreen>()`;
  mỗi frame `QueueStateEvent(screen, new TouchState { touchId, position, phase })` `Began` → `Moved` →
  `Ended` rồi `InputSystem.Update()`; probe `[DefaultExecutionOrder(-1000)]` + 3 setting của mục 16;
  điểm chạm = `bounds.center` collider ghost chiếu ra screen.
- **Kết quả:** `Began` → `IsDragging = true`, `Pointer.current = FastTouchscreen`, đúng 1
  `OnMouseClickPerformed`; ô (2,2) → `(4.00, 1.00, 4.00)`; ô (4,1) → `(8.00, 1.00, 2.00)`; `Ended` →
  `false`. Ô (0,0) không dùng được vì tia trúng `Bot_Customer 3(Clone)` (vật cản kịch bản).
- Giới hạn: touch inject, không phải ngón tay thật; chưa multi-touch/pinch, `fingerId` trên thiết bị;
  Confirm bằng touch chưa bấm (đã kiểm bằng chuột ở mục 16); host đơn.

### 21 — `StewSO.kitchenObjectSOList` trỏ sai · ✅

- `Assets/SO/FoodSO/StewSO.asset` liệt kê `[Potato Chopped, Carrot Slices]`, trong khi `bowl.prefab`
  chỉ nhận `StewCookedSO` (`validKitchenObjectSOList`) và `plate.prefab` không nhận nguyên liệu sống;
  `BotCustomerController.IsServerCorrectFood()` (`BotCustomerController.cs:211-222`) so đúng số lượng +
  từng SO → **không thể phục vụ Stew**.
- Đổi list thành `[StewCookedSO]`; giữ `price=35`, `exp=40`, `_guid`. Serve + collect lại sau reimport:
  **54 coins / 40 XP**.

---

## 3. Từ `optimization_plan.md`

Nhãn gốc: ✅ Fixed (đã kiểm console sạch + EditMode xanh) · ⏸ Deferred (**đề xuất của agent, chưa được
chủ dự án xác nhận**) · ➖ Stale (báo cáo gốc không còn khớp code, không làm lại).

### 3.1 ✅ Fixed

**GC/CPU mỗi frame**

| Vấn đề | Vị trí | Sửa |
|---|---|---|
| Enum boxing | `StateManager.cs:27` | `nextStateKey.Equals(...)` → `EqualityComparer<T>.Default.Equals(...)` |
| Capsule overlap cấp phát | `PlayerStateMachine.cs` | `OverlapCapsule` → `OverlapCapsuleNonAlloc` + `highlightHitBuffer` |
| `async void Update()` | `InGameState.cs` | đổi sang coroutine |
| Lifecycle rỗng | `KitchenGameManager.cs` | bỏ `Update()` rỗng |
| Closure mỗi frame | `GameData.GetPlayerStatsById` | `Find(lambda)` → vòng `for` chỉ mục |
| `Camera.main` per-frame | `LookAtCamera.cs`, `Window_QuestPointer.cs`, `CameraController.cs` | cache lười; `CameraController` dùng `_camera.nearClipPlane` |
| Enum `ToString()` mỗi lần đổi state | `BotCustomerController.PlayAnimation` | `Animator.Play(BotAnimation.StateHash(state))`, hash memoize |

**Cấp phát khi gọi lặp**

| Vấn đề | Vị trí | Sửa |
|---|---|---|
| Copy list để duyệt | `BotManager` (`GameManager_OnStateChanged`, `StopSpawnBot`, `KickAllBots`) | `for` ngược trên list sống; `ReturnBotToPool` xoá tại index hiện tại |
| Tra dictionary 2 lần | `UIPopupManager` | 1 `TryGetValue`; `RemoveFromeVisibleList` → 1 `List.Remove` |
| Test kiểu 2 lần | `KitchenObject.TryGetTableware` | `is` + `as` → 1 `as` + null check |
| Quét recipe O(n) | `RecipeDatabaseSO` | dictionary cache dựng 1 lần trong `Initialize()` |

**Bug đúng-sai được báo như perf**

| Vấn đề | Vị trí | Trạng thái |
|---|---|---|
| Guard update của bot | `BotCustomerController.cs`, `BotStateMachine.cs` | `if (!IsServer) return;` |
| `StopCoroutine` hỏng | `BotManager.cs` | lưu `Coroutine` trả về rồi truyền lại |
| `Table.cs` `Destroy(this)` | `Table.cs` | không còn |
| Sai toán tử | `UICharacterPopup.cs` | `unfittables != null \|\| …Count > 0` → `&&` |
| Interstitial không bao giờ hiện | `InGameState.cs` | khôi phục `ShowInterstitialAds("Break_Time")` + null guard |
| Mật khẩu plaintext trong save | `SavedData` / `LoadGame` | SHA-256 + salt mỗi save (`PasswordHasher`, `v1$salt$hash`); plaintext cũ vẫn load và được migrate khi ghi |
| Consent quảng cáo mặc định cho phép | `AdsManager` | deny-by-default (`IsGdprConsentGranted` false, `IsCcpaOptOut` true) + hook `SetGdprConsent`/`SetCcpaOptOut` cho CMP sau này |

### 3.2 ➖ Stale — không làm lại

- `BuildingGhost` NetworkObject churn (`BuildingGhost.cs:127-153`): preview đã là bản **local, không
  networked** (`CreateLocalPreview`, `:197`), MonoBehaviour + `NavMeshObstacle` tắt; `RefreshVisual` chỉ
  chạy khi chọn item, không per-drag → không có gì để pool.
- `CuttingCounterController` `ToList().IndexOf` — không còn (dùng cache `RecipeDatabaseSO`).
- `PanCookingTool`/`PotCookingTool` quét tuyến tính — đều qua `RecipeResolver` đọc dictionary cache.
- `Renderer.materials` rò ở `BotCustomerController` — bản sao do `HighlightMaterials` sở hữu, giải phóng
  trong `OnDestroy`.
- `popupType.ToString()` và tra dictionary 2 lần ở `UIPopupManager` — đã tra theo enum key, 1 `TryGetValue`.
- LINQ trong UI (`UIUpgradePopup`, `UICharacterPopup`, `UIMenuPopup`) — chạy khi mở popup/đổi tab, không
  per-frame; chỉ đáng xem lại cùng UI pooling.

### 3.3 ⏸ Deferred — đề xuất của agent, chưa có xác nhận của chủ dự án

Nguồn: commit `92a5da3cc`; không có record sign-off, ngày hay trích dẫn nào. Đọc "Deferred" là *agent
đề xuất chưa bắt đầu*, **không** phải quyết định. Không mục nào bị bỏ; chủ dự án có thể kéo lại bất cứ
lúc nào. Lý do kỹ thuật: mỗi mục đổi hành vi trên dây hoặc save → cần plan + lượt test multiplayer.

| # | Mục | Trạng thái |
|---|---|---|
| 1 | Chuỗi `ServerRpc` → `ClientRpc` | mô tả gốc sai, xem mục 01 |
| 2 | JSON game-state sync | **KHÔNG LÀM** (đo 2026-10-26), xem mục 02 |
| 3 | GUID string → int / `NetworkGuid` | cần bảng GUID↔index ổn định khi reorder config, không đổi save format (`GameData.SaveVersion` bump) — xem mục 03 |
| 4 | UI pooling (`UICharacterPopup`, `UIUpgradePopup`, `UIMenuPopup`) | giá trị thấp riêng lẻ, là tiền đề bỏ LINQ ở §3.2 — xem mục 04 |
| 5 | `Camera.main` | ✅ **DONE** (kiểm lại 2026-11-14) — xem mục 05 |
| 6 | Chuỗi singleton trong `PlayerStateMachine.HandleMovement` | đã 0 alloc; cache cần invalidate ở `ReplaceGameDataFromHost` + `OnPlayerStatsAdded` — xem mục 06 |

### 3.4 Mobile frame rate (2026-11-26)

- **Đo** (Editor, PC, bếp đông: 13 cooking tool, 9 khách, host đơn, 1,697 frames): script < 0.5 ms/frame,
  GC ≈ 1.4 KB/frame (≈ 1.1 KB IMGUI `GUI.Repaint`, chỉ Editor), ≈ 168k tam giác, 239 shadow caster,
  78 skinned mesh, 33 SetPass. Code game không phải nút thắt trên điện thoại; nút thắt là setting:
  Android chạy **Ultra** (shadow map 4096, shadow 150 m, soft shadow, depth texture) và không có
  `targetFrameRate` (Android mặc định cap 30 FPS).
- **Đã làm** (design `docs/superpowers/specs/2026-11-26-mobile-framerate-design.md`):
  - 3 level Android `Mobile Low/Medium/High`, mặc định `Mobile Medium`; mục tiêu 60 FPS với
    `vSyncCount = 0`.
  - Settings popup: Graphics `Auto | Low | Medium | High`, FPS `30 | 60`. Auto = chọn theo máy + nhớ lần
    hạ cấp + governor lúc chạy; chọn tay level hoặc FPS thì cố định.
  - Renderer `KitchenObject` không đổ bóng ở Mobile Low/Medium (Editor: 14 → 12 shadow caster với 1 đĩa
    cầm tay).
  - Rác mỗi frame: `QualityStarsOverlay` và `CloseUpOccluderHider` duyệt `KitchenObject.Spawned` /
    `PlayerStateMachine.All` theo index (không enumerator interface); hider cache renderer của món đang
    cầm thay vì `GetComponentsInChildren` mỗi frame. A/B Editor: median `GC Allocated In Frame` bằng nhau
    (28,952 B cả hai; phần còn lại là Editor/tutorial).
- **Còn phải làm trên máy thật** (build Android đang chặn: mật khẩu keystore, SDK platform 35):
  - Chỉnh ngưỡng `DeviceTierPicker` (RAM 6 GB / 3 GB, 8 / 6 core, 2 GB bộ nhớ đồ hoạ) trên máy yếu/trung/
    mạnh; đọc dòng `[Perf] launch` từ `adb logcat`.
  - Kiểm governor không hạ cấp trên máy giữ được 60 FPS (chú ý thermal throttling sau 10+ phút), và
    Mobile Low @ 30 FPS chấp nhận được trên máy yếu nhất.
  - Đo GC bytes/giây trên Player build (số Editor gồm IMGUI).
  - Nếu skinned mesh là chi phí GPU (≈ 8 phần customization mỗi nhân vật) thì gộp mesh nhân vật là ứng
    viên kế tiếp.

### 3.5 Verification (của §3.1)

1. `refresh` + force recompile → console không error/warning.
2. EditMode **49/49** (save system gồm `PasswordHasher`, grid placement, economy, data classes, `GameData`
   lookup, hash state `BotAnimation`).

Các sửa §3.1 **không được profile** — suy luận, không đo. Baseline 2026-10-12 (Editor Play Mode, 1 client,
Relay host thật): S1/S2/S5/S6/S10 main thread không vượt **4.59 ms**; S3/S4/S7/S8/S9 đã đo sau đó trên
Editor/MPPM (mục 13). Còn thiếu GC bytes/giây và bandwidth trên Android/Player build — lý do §3.3 chưa
đóng hẳn.

---

## 4. Từ `plans/README.md` — phân nhóm & phân loại

Nguồn gốc của 13 mục đầu: `docs/optimization_plan.md` §3, `docs/datamanagement.md` §4. Lần phân tích này
đã sửa một số mô tả cũ trong `optimization_plan` cho khớp code.

### 4.1 Nhóm

- **A. Mạng / tối ưu — đề xuất hoãn vì rủi ro cao (CHƯA được xác nhận):** 01 (chỉ bước 1, bước 2 giao 02),
  02 (KHÔNG LÀM 2026-10-26), 03 (không), 04 (✅ bước 1 2026-10-21), 05 (✅ đã làm), 06 (không). Đây là
  đề xuất của agent, có thể đưa vào làm lại bất cứ lúc nào.
- **B. Data cần chủ dự án quyết — ✅ đã chốt 2026-10-21:** 07 giữ `35 / 40` (+ sửa `kitchenObjectSOList`),
  08 giữ `Stew Burned`, 09 cách ghim số + quy tắc append, `None = 9999`.
- **C. Kiến trúc cần chốt:** 10 không chuẩn hoá, thêm guard + quy ước; 11 hoãn tới Addressables;
  12 xong (test neg đã chạy, ô ẩn "dính" đã sửa).
- **D. Chưa làm vì thiếu dữ liệu:** 13 profiler — ưu tiên cao nhất; GC trên Player build vẫn chờ.
- **Phát sinh thêm:** 14–21 (xem bảng §1).

### 4.2 Mức nghiêm trọng × ảnh hưởng khi sửa

| Mục | Nghiêm trọng nếu để đó | Ảnh hưởng khi sửa |
|---|---|---|
| 07 Stew `price=0, exp=0` | 🔴 Cao — trừ 60 coins thật (`GameManager.Economy.cs:144`), trả 0 (`:416-417`), 3 chỗ UI hiện `$0` | 🟢 Rất thấp (sửa 2 field) |
| 12 shop reward fail im lặng | 🟠 TB — ô biến mất, Console sạch | 🟡 Thấp (dict) / 🟢 Rất thấp (`LogError` +2 dòng) |
| 09 `ShopItemCategory` chưa ghim | 🟠 TB (phòng ngừa; đã từng dính với `ShopItemType`) | 🟢 Rất thấp — không `.asset` nào đổi nếu giữ số |
| 05 `Camera.main` | 🟡 Thấp–TB (2 lookup/frame khi kéo ghost) | 🟡 Thấp |
| 04 `UIMenuPopup` rebuild | 🟡 Thấp–TB | 🟠 TB — listener trùng, item sai/thừa; rủi ro regression UI cao nhất nhóm A |
| 10 guard null Tableware | 🟡 Thấp | 🟢 Rất thấp (+5 dòng) |
| 01 RPC chain | 🟡 Thấp (vài chục B/s) | 🟡 Thấp (bước 1) / 🔴 Cao (bước 2: `GameManager.Data.cs` 13 cặp + `Economy.cs` 11 + join + save) |
| 08 `StewBurnedSO.objectName` | 🟢 Rất thấp | 🟢 Rất thấp |
| 03 GUID → int | 🟢 Rất thấp (~28 B/RPC) | 🔴 Cao nếu chọn B (va chạm hash) |
| 02 JSON join | 🟢 Rất thấp (1 lần/join) | 🔴 Cao nhất — join + serialiser chung với save (`MultiplayerManager.cs:112,159` → `FileDataHandler`) |
| 06 PlayerStats cache | ⚪ Không còn lỗi | 🟠 TB — stale reference |
| 11 SO↔prefab cycle | ⚪ Không phải lỗi hôm nay | 🟠 TB — audit mọi prefab, đổi contract spawn |
| 13 Profiler | ⚪ Công cụ đo | ⚪ 0 |

Thứ tự hành động đã đề xuất: **13** đo trước → **Gói 1** (làm ngay) `07` → `12 (LogError)` → `08` →
`09` → `10 (guard)` → **Gói 2** `05` → `12 (dict)` → **Gói 3** (chỉ khi profiler chứng minh) `04` bước 1 →
**Gói 4** (đề xuất không làm trừ khi đổi protocol/Addressables, chờ chủ dự án xác nhận) `01`, `02`, `03`,
`06`, `11`.

---

## 5. Nhật ký kiểm chứng theo ngày

- **2026-10-12 — Kiểm bằng mắt Gói 1** (play mode, Relay host thật, probe `Tools/Profiling/Enter Game
  (new host)`): 07 tile Stew `$35` / `40 XP`; 08 `Stew Cooked` / `Stew Burned`; 12 ba tab shop **0
  `LogError`**, đủ tên/giá/icon; 09 nhóm `None` vẫn hiện. Baseline profiler S1/S2/S5/S6/S10. Ảnh chụp ra
  `Assets/Screenshots/` rồi dời khỏi project — không commit PNG.
- **2026-10-13 — Tự động hoá S3/S4 bằng computer-control: THẤT BẠI.** Mapping toạ độ khớp 0.5 px (bấm
  `(1070,340)` → đọc `(1019.92, 569.29)`, dự kiến `(1019.9, 569.8)`); chuột vào Input System; bàn phím
  `w=False` dưới cả 3 cách; player đứng yên `(-3,0,-3)`; đã loại `GetOwnerStats()` null (`moveSpeed=5`) và
  `IsDisableInput` (mọi `DisableInput(...)` bị comment, `BuildingPlacementManager.cs:139,150`,
  `UIOptionMenuPopup.cs:130,137`); joystick `OnScreenStick` → `<Gamepad>/leftStick` (ở
  `MainMenuScene.unity`, `Canvas_Joystick`; `UIJoyStick.cs` là singleton rỗng 8 dòng) bấm đúng tâm
  `(250.32, 231.10)` nhưng không nhúc nhích; vào placement từ code OK (`Selected Changed:
  BowlTablewareCounter`); không xác nhận được `isDragging`.
- **2026-10-20 — Kéo ghost bằng chuột PASS** (inject qua Input System, mục 16); test neg shop (mục 12);
  `ShakeCamera` kiểm tách rời (mục 05); quy ước counter dán vào `datamanagement.md` §4.7. Chạy lại đầy đủ:
  0/0, **49/49**, **110 assemblies**; APK thử và thất bại (mục 17).
- **2026-10-21 — Sau nhóm 2 + S3/S7-rút gọn/Stew:** refresh 0/0, 49/49, 110 assemblies; warning quay hết
  (mục 18); 1 nhấn = 1 event (mục 19); touch kéo ghost OK (mục 20); menu popup 10 lần vẫn 5/20 (mục 04);
  ô shop ẩn hiện lại (mục 12); S3 `(-3,0,-3)` → `(-3,0,7.91)` + click chọn `PlateTablewareCounter(Clone)`;
  S7-rút gọn p50 7.50 / p95 8.45 ms, GC 3722.4 KB/s (Editor + probe); Stew nấu → cháy, phục vụ trong bát
  `Coins 200→254`, `Exp 0→40`; sửa `StewSO.kitchenObjectSOList` (mục 21). Probe `TmpInputProbe.cs` đã xoá
  kèm `.meta`; không commit PNG; `git status` chỉ còn 5 file code + tài liệu. Chưa commit
  `DailyCooking/Assets/GooglePlayGames/.../current-build/` (untracked).
- **2026-10-22 — S7 đầy đủ 60 s:** 6223 frames, p50 9.48 / p95 11.70 / p99 14.51 / max 78.35 ms, GC
  3090.1 KB/s (Editor + probe), 5 bot, 5/5 vòng (coins `259 → 318 → 577 → 621 → 665`; exp
  `40 → 80 → 0 → 40 → 80`); coroutine trong probe, không lệnh MCP nào chen vào; `TmpS7Probe.cs` đã xoá.
- **2026-10-23 — S8/S9 2 + 4 player, bandwidth nhẹ (sau reset `Library`):** 2 player late join 3.77 s,
  snapshot 760/449 B/1 chunk; 4 player `clients=4/players=4`, snapshot 496/523/549 B/1 chunk, phase cũ
  1.251 s (gồm event); bandwidth ~36.6 s không thao tác host 1.12/0.81 KB/s, 8.50/7.16 pps; client cao
  nhất 1.07/0.81 KB/s; 3 client 0/0; fix rejoin `f2240ca4d`.
- **2026-10-25 — 4 player join/visual + fix sync vị trí:** save `S8Verify3`, 3 client 0/0; thay
  `ClientNetworkTransform` MISSING bằng `NetworkTransform` owner; 2 player sync cả hai chiều (probe +
  joystick tay); flow tutorial test `CloseTutorial()` trước.
- **2026-10-26 — 4 player movement + bandwidth + snapshot + EditMode:** sai lệch ≤ 0.005 m; movement
  18.6–20.9 KB/s/client, cooking 3.4–5.1 KB/s/client; probe cũ 0.994/1.226/1.285 s; tách phase 0.541/0.578 s,
  JSON+decompress+replace 53–62 ms, `OnDataSyncToNewClient` 0.995/1.066 s, 4 chunk hash khớp → **02 KHÔNG
  LÀM**; xoá probe + revert instrumentation → 0/0 cả 4 editor, EditMode **49/49** (1.16 s).
- **2026-11-14 — Kiểm lại `Camera.main`:** chỉ còn cache helper + 1 comment (mục 05).
- **2026-11-26 — Mobile frame rate:** đo + 3 level Android + settings Graphics/FPS (§3.4).

---

## 6. Điều kiện kiểm chứng chung cho mọi mục

```
unityMCP_refresh_unity  {compile: request, mode: force, scope: all, wait_for_ready: true}
  → sleep 30s
unityMCP_read_console   {types: ["error","warning"]}   # phải 0/0
unityMCP_run_tests      {mode: EditMode}               # baseline 49/49 pass
Tools → Build Checks → Compile Android Player Scripts # DLL ở Temp/BuildChecks/AndroidPlayerScripts/
```

Lần chạy đầy đủ 2026-10-20: 0 error / 0 warning, **49/49**, **110 assemblies**. Build APK thật chưa kiểm
được (mục 17). Probe tạm luôn xoá kèm `.meta` trước khi commit; không commit PNG.

### Công cụ kiểm bằng mắt (editor-only, `Assets/Editor/PerfScenarios.cs`)

| Menu | Dùng để |
|---|---|
| `Tools/Profiling/Enter Game (new host)` | vào `GameScene` bằng đường production (`NewGame` → `StartHostSessionAsync` → `Loader.LoadNetwork` → `SwitchState(InGameState)`) |
| `Tools/Verify/Unlock all dishes + open Menu popup` | `RestaurantData.Level = 99` + `MenuData.AddUnlockedDish` mọi món → thấy giá thật thay vì overlay khoá |
| `Tools/Verify/Hide tutorial + show Stew tile` | tắt `TutorialPanel` + `Window_QuestPointer` (cửa sổ focus của tutorial che ô **và nuốt click**) |
| `Tools/Verify/Cooked vs burned stew in option menu` | mở `UIOptionMenuPopup` đúng cách — `ShowPopup(null)` tự `HidePopup()`, phải truyền `Param` kèm sender `IHasOptionalSO` |
| `Tools/Verify/Open Shop popup once` | mở shop 1 lần để bấm tay 3 tab |
| `Tools/Verify/Input diagnostics (hold input while it runs)` | log 8 mẫu / ~3 s: `Keyboard.current.wKey`, `Pointer.current.position/press`, `GetMovementVectorNormalized()`, toạ độ player, `GetOwnerStats()` |
| `Tools/Profiling/S4 - Start placement (ghost)` | `BuildingPlacementManager.SetPlacedObjectTypeSO` với object từ `PlacedObjectDatabase`, toạ độ = raycast tâm Game view → dựng ghost không cần mua |
| `Tools/Profiling/S4 - Cancel placement` | `SetPlacedObjectTypeSO(null, …)` → nhả selection |
| `Tools/Verify/S4 state (ghost / drag / mouse)` | đọc `BuildingGhost.IsDragging` / `StopMoving` / `IsMouseOverUI()` / toạ độ ghost |
| `Tools/Profiling/S10 - ShakeCamera 10s` | gọi `ShakeCamera` (chỗ duy nhất gọi hàm này) |
