# 12 — Tra cứu GUID của shop reward (3 nguồn dữ liệu + 1 chỗ linear)

| | |
|---|---|
| **Nhóm** | C. Kiến trúc cần chốt |
| **Nguồn** | `docs/datamanagement.md` §4.2 |
| **Trạng thái** | ✅ **Xong** — Bước 1 (dictionary) ở `217875b19`, Bước 2 (LogError) ở `572f37092`, Bước 4 (cache `GetById`) cũng ở `217875b19`. Chỉ **Bước 3 (hàm resolve chung)** là chưa làm và nó là tùy chọn. Bước 1 kiểm lại hôm nay: `UIShopItem.cs:108-109` đã dùng `PlacedObjectTypeSODictionary.GetValueOrDefault`, không còn `Find` tuyến tính nào trong `Assets/Scripts`. |
| **Rủi ro** | Thấp |
| **Ưu tiên** | Trung bình (perf thấp, **robustness mới là cái đáng làm**) |

---

## Mục tiêu

Mỗi `ShopReward` chỉ có một `Guid`, nhưng guid đó có thể trỏ vào **hai namespace khác nhau**
(đồ đặt trên lưới vs nguyên liệu nhà bếp). Xác định đúng nguồn, tra nhanh, và **báo lỗi khi
không thấy** thay vì xoá im lặng.

## Hiện trạng

### Nguồn dữ liệu

| Nguồn | Nội dung | Kiểu tra |
|---|---|---|
| `ConfigShop.ShopItems` | `List<ConfigShopItem>`, mỗi item có `Rewards[].Guid` | list |
| `GridBuildingSystem.PlacedObjectTypeSODictionary` (`:58`, dựng ở `:86-89`) | `Dictionary<string, PlacedObjectTypeSO>` từ `PlacedObjectDatabase.PlacedObjects` | **O(1)** |
| `KitchenGameManager.KitchenObjectSODic` (`:38,47,54-57`) | `Dictionary<string, KitchenObjectSO>` | **O(1)** |

Ba nguồn **không cùng namespace**: guid của counter/table/wall khác guid của rau/nhà bếp.
`ShopItemCategory` chính là cờ phân nhánh.

### Chỗ tra linear (client)

`UIShopItem.cs:96-133` `GetReward(...)` — chạy **mỗi lần render 1 ô shop**:

```csharp
case ShopItemCategory.Counters:
case ShopItemCategory.Tables:
case ShopItemCategory.Walls:
    var placedObject = GridBuildingSystem.Instance.PlacedObjectDatabase.PlacedObjects
        .Find(x => x.Guid == reward.Guid);      // ← linear, dù dictionary đã có sẵn
    if (placedObject == null) { gameObject.SetActive(false); break; }   // ← fail im lặng
    SetIcon(placedObject.icon);
    break;
case ShopItemCategory.Vegetables: ... case ShopItemCategory.Meats:
    var item = KitchenGameManager.Instance.KitchenObjectSODic.GetValueOrDefault(reward.Guid);
    if (item == null) { gameObject.SetActive(false); break; }           // ← fail im lặng
    SetIcon(item.Sprite);
    break;
```

Hai vấn đề:

1. **Linear `Find`** trên `PlacedObjectDatabase.PlacedObjects` dù
   `GridBuildingSystem.PlacedObjectTypeSODictionary` đã được dựng sẵn từ đúng list đó.
2. **Fail im lặng**: guid sai / asset bị xoá → ô shop biến mất, không log. Rất khó tìm khi QA báo
   "có item chợ bị mất".

### Server đã ổn

`GameManager.Economy.cs:84-115` `BuyShopItemServerRpc(int shopItemId)`:

- `:88` linear `Find` theo `Id` — **chỉ mỗi lần mua**, list ≤ vài chục → chấp nhận được
  (tuy nhiên có thể pre-cache `Dictionary<int, ConfigShopItem>`).
- `:110` `GetKitchenObjectSOByGuid(reward.Guid)` — đã **O(1)**.

`ShopManager.OnPurchase` chỉ gửi `item.Id` (int) — không gửi guid.

## Thay đổi đề xuất

### 1 — Đổi linear → dictionary (dòng đơn, không đổi hành vi)

`UIShopItem.cs:106-107`:

```csharp
var placedObject = GridBuildingSystem.Instance.PlacedObjectTypeSODictionary
    .GetValueOrDefault(reward.Guid);
```

`GridBuildingSystem.cs:61` đã public sẵn → **không cần thêm API**.

### 2 — Bắt buộc log khi resolve fail

```csharp
if (placedObject == null)
{
    Debug.LogError($"UIShopItem: no PlacedObjectTypeSO for shop reward guid '{reward.Guid}' ({configShopItem.Name})");
    gameObject.SetActive(false);
    break;
}
```

Tương tự nhánh `KitchenObjectSODic`. Đây là **phần có giá trị nhất** của mục này.

### 3 (tùy chọn) — Một hàm resolve chung

```csharp
// ShopRewardResolver.cs
public static bool TryResolve(ShopReward reward, ShopItemCategory category,
    out PlacedObjectTypeSO placed, out KitchenObjectSO kitchen)
```

Gộp `switch (category)` về 1 chỗ, để sau này thêm category không phải sửa 2 nơi
(`UIShopItem.GetReward` + `ShopManager`/handler server).

### 4 (tùy chọn) — Pre-cache `Dictionary<int, ConfigShopItem>`

Ở `ConfigShop` (hoặc `ConfigManager` khi load):

```csharp
private Dictionary<int, ConfigShopItem> _byId;
public ConfigShopItem GetById(int id) => _byId.GetValueOrDefault(id);
```

Dùng lại ở `GameManager.Economy.cs:88`. Lợi ích nhỏ, làm cho trọn.

### Không cần làm

Gộp 2 dictionary thành 1 (`Dictionary<string, object>`) — **không nên**: hai namespace guid khác
nhau, gộp sẽ phải kiểm tra kiểu khi lấy và mất an toàn type.

## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| Perf | Bỏ 1 linear scan mỗi ô shop khi mở chợ. Số nhỏ (≤ vài chục) → không đo được, nhưng sạch. |
| Robustness | **Log lỗi rõ ràng** khi guid sai thay vì ô shop biến mất. |
| Gameplay | Không đổi (giá trị kết quả y hệt). |
| Save / mạng / prefab? | Không. |
| Code thêm | ~10 dòng; nếu làm mục 3, thêm 1 file ~25 dòng. |

## Kiểm chứng

1. Compile Android + 49/49 test.
2. Mở shop → mọi tab hiện đủ item, icon đúng.
3. **Test neg (quan trọng):** đổi 1 `reward.Guid` trong `ConfigShopSO.asset` thành guid rác →
   mở shop → Console hiện `LogError` với guid + tên item, ô item ẩn. Sửa lại → hiện lại.
4. Mua item loại `Item` và loại `Ingredient` → vào inventory / PostBox đúng.

## Rủi ro / rollback

- Gần 0. Nếu `PlacedObjectTypeSODictionary` chưa được dựng (script order) → `GetValueOrDefault`
  trả null → giờ sẽ log lỗi, **dễ phát hiện hơn hiện tại**.
- Rollback: 1 commit.

---

## Trạng thái kiểm chứng (cập nhật)

| Hạng mục | Trạng thái |
|---|---|
| Bước 1 — dictionary thay `Find` | ✅ `217875b19`; kiểm lại 2026-10-20: `UIShopItem.cs:108-109`, không còn `PlacedObjects.Find` trong `Assets/Scripts` |
| Bước 2 — `LogError` khi resolve fail | ✅ `572f37092`; đã chơi: mở shop, bấm 3 tab → 0 `LogError`, đủ tên/giá/icon |
| Bước 4 — `ConfigShop.GetById` | ✅ `217875b19`; `GameManager.Economy.cs:88-89` dùng `GetById` thay `List.Find` |
| Bước 3 — hàm resolve chung | ❌ chưa làm (tùy chọn, không có lỗi nào đang chờ nó) |
| **Test neg** (guid rác → thấy `LogError`) | ✅ **đã chạy 2026-10-20**, play mode GameScene, **không sửa asset trên đĩa**. Xem mục "Test neg" bên dưới. |
| **Ô ẩn "dính" session** | ✅ **đã sửa 2026-10-21** — `Refresh()` resolve lại reward; kiểm: ẩn bằng guid rác → mở lại shop → ô hiện lại. Xem mục cuối file. |

### Test neg — kết quả

Vào game (NewGame + host + `GameScene`), mở shop từ HUD, chọn tab **Restaurant**, rồi gọi thẳng
`UIShopItem.GetReward(rewards)` với `rewards = [ShopReward("deadbeef…", 1)]` trên ô **Fridge**
(không sửa `ConfigShopSO.asset`):

```
[Error] UIShopItem: no PlacedObjectTypeSO for shop reward guid
        'deadbeefdeadbeefdeadbeefdeadbeef' (Fridge / Counters)
```

ô đó `activeSelf = false`. Làm tương tự trên ô **Peach** ở tab **Ingredients** với
`cafebabecafebabecafebabecafebabe`:

```
[Error] UIShopItem: no KitchenObjectSO for shop reward guid
        'cafebabecafebabecafebabecafebabe' (Peach / Vegetables)
```

ô đó cũng `activeSelf = false`. Đóng/mở lại shop: **0 error**, các ô còn lại vẫn active và đủ
tên/giá. `git status` sau khi thoát Play: không có thay đổi nào dưới `Assets/SO` hay `Assets/Configs`
— đúng như cam kết "chỉ sửa trong bộ nhớ".

### Ghi nhận: ô bị ẩn thì "dính" trong cùng một session

`UIShopPopup.ShowPopup` chỉ gọi `item.Refresh()` cho từng ô, **không** gọi lại `SetItem`
(comment trong code: *"Items are built once"*). Nên ô bị `SetActive(false)` vì guid sai **không tự
hiện lại** khi đóng/mở shop — chỉ hiện lại khi popup được dựng lại (đổi scene / khởi động lại).

Số đo: popup giữ **44** ô cho mọi tab (pool); sau khi ẩn `Fridge` + `Peach` rồi đóng/mở shop,
`Fridge` và `Peach` vẫn `activeSelf = false`, tab Restaurant chỉ còn **12** ô thay vì 13
(`FindObjectsByType` chỉ trả object đang active).

**Đã sửa 2026-10-21.** `UIShopItem.Refresh()` giờ `SetActive(true)` rồi resolve lại reward của
chính nó (`GetReward(configShopItem.Rewards)`). `SetItem()` chỉ gọi `Refresh()` một lần nên không
resolve trùng. Ô nào thật sự có guid hỏng vẫn bị ẩn lại + `LogError` như cũ, nhưng ô bị ẩn do một
lần resolve sai (test, config sửa lúc chạy) sẽ hiện lại ở lần mở shop kế tiếp.

Kiểm lại trong play mode (host đơn):

| Bước | Kết quả |
|---|---|
| Mở shop, chọn ô `Clear Counter` | active |
| Gọi `GetReward([ShopReward("deadbeef…", 1)])` trên chính ô đó | `activeSelf = **false**` + **1** `LogError` |
| Đóng + mở lại shop | `activeSelf = **true**`, **0** error mới (guid thật resolve được) |
| Tổng ô / ô active của popup | 44 ô, 35 active — 9 ô inactive còn lại là **template** (`configShopItem == null`, `Refresh` return sớm), không phải ô bị ẩn vì guid |

`git status` sau khi thoát Play: không có thay đổi dưới `Assets/SO` / `Assets/Configs`
— test chỉ sửa trong bộ nhớ.
