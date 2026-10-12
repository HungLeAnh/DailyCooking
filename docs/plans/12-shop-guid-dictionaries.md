# 12 — Tra cứu GUID của shop reward (3 nguồn dữ liệu + 1 chỗ linear)

| | |
|---|---|
| **Nhóm** | C. Kiến trúc cần chốt |
| **Nguồn** | `docs/datamanagement.md` §4.2 |
| **Trạng thái** | ✅ **Đã làm Bước 2 (LogError)** — 2 chỗ fail im lặng ở `UIShopItem.GetReward` giờ log guid + tên item + category. Compile Android + 49/49, console 0/0. Play mode: mở shop popup, bấm cả 3 tab (Resource / Restaurant / Ingredients) → **0 `LogError`**, mọi ô hiện đủ tên + giá + icon. **Bước 1 (đổi linear → dictionary) thuộc Gói 2, chưa làm** — linear `Find` còn nguyên. |
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
