# 09 — Ghim giá trị enum trong `ConfigShopSO` (serialize dạng số)

| | |
|---|---|
| **Nhóm** | B. Data cần người dùng quyết |
| **Nguồn** | `docs/datamanagement.md` §4.3 |
| **Trạng thái** | ✅ **Đã làm** — ghim số tường minh + `None` dời `8` → **`9999`**, migrate 3 dòng trong `ConfigShopSO.asset`. Compile Android + 49/49, console 0/0. **Bước 2 (`[InspectorName]`) và Bước 3 (guard lúc build) chưa làm.** |
| **Rủi ro** | Thấp (1 file enum), nhưng **lỗi khi xảy ra thì âm thầm** |
| **Ưu tiên** | Cao — chống đúng loại bug project đã từng dính |

---

## Mục tiêu

Unity serialize enum thành **số nguyên** trong asset/prefab/scene và cả trên mạng. Nếu ai đó
**chèn/xoá/reorder** một giá trị trong enum, toàn bộ dữ liệu đã ghi sẽ dịch số mà không báo lỗi.

## Hiện trạng

### Asset

`Assets/Configs/ConfigShop/ConfigShopSO.asset`:

```yaml
- id: 1
  name: Clear Counter
  type: 1          # ← ShopItemType, đã ghim số
  category: 0      # ← ShopItemCategory, CHƯA ghim số
```

### Hai enum, hai mức an toàn khác nhau

**`ShopItemType.cs` — ĐÃ an toàn** (có comment ghi rõ bug thật đã xảy ra):

```csharp
// Serialized as numbers in scenes, prefabs and config assets: never reorder or renumber,
// only append new values. (Inserting Ingredient in May 2026 silently shifted Gem/Coin data.)
public enum ShopItemType
{
    None = 0, Item = 1, Ingredient = 2, Gem = 3, Coin = 4,
}
```

**`ShopItemCategory.cs` — CHƯA an toàn** (không có giá trị tường minh):

```csharp
public enum ShopItemCategory
{
    Counters,    // 0
    Tables,      // 1
    Walls,       // 2
    Vegetables,  // 3
    Bakery,      // 4
    Dairy,       // 5
    Patties,     // 6
    Meats,       // 7
    None         // 8
}
```

Chèn `Drinks = 3` vào giữa → `Vegetables` thành 4, `Bakery` thành 5... → `ConfigShopSO.asset`
đang lưu `category: 8` sẽ hiểu sai. Không có exception nào ném ra.

### `ShopItemType` còn đi qua mạng

`GameManager.Economy.cs:118` `ExchangeCurrencyServerRpc(ShopItemType rewardType, ...)`,
`:260` `ClaimDailyFreeServerRpc(ShopItemType rewardType, ...)`, và
`:282` `IncreaseDailyFreeCountClientRpc(ShopItemType rewardType)`.

Ngoài ra còn được serialize trong prefab/scene:

- `ShopManager.cs:8` `[SerializeField] public ShopItemType RewardType`
- `UICurrencyItem.cs:8` `[SerializeField] private ShopItemType type`
- `UIDailyFreeItem.cs` (case theo `ShopItemType`)

→ Ghim số **cũng bảo vệ wire protocol** (host/client khác nhau về số sẽ hiểu sai).

### Nơi đọc `Category`

- `UIShopPopup.cs:67, 78` — `listItem.ToLookup(x => x.Category)` phân nhóm tab shop
- `UIShopPopup.cs:102, 121` — `ScrollTo` / `GetUIShopItem` (dùng trong `BuildingTutorial.cs:35,51`)
- `UIShopItem.cs:102-121` — `switch (itemCategory)` chọn icon thưởng

## Thay đổi đề xuất

### Bước 1 (bắt buộc) — ghim số cho `ShopItemCategory`

```csharp
public enum ShopItemCategory
{
    Counters = 0,
    Tables = 1,
    Walls = 2,
    Vegetables = 3,
    Bakery = 4,
    Dairy = 5,
    Patties = 6,
    Meats = 7,
    // Category mới: append tại đây, giữ số nhỏ hơn None
    None = 9999,
}
```

> **Đã làm, khác đề xuất ban đầu.** Ngoài ghim số tường minh, `None` còn được dời `8` → **`9999`**,
> và `ConfigShopSO.asset` migrate cùng lúc (3 dòng `category: 8` → `category: 9999`).
>
> Lý do: với `None = 8`, thêm category mới sẽ đẩy `None` xuống giữa dãy. `9999` giữ `None` luôn
> cuối cả về **khai báo lẫn số**.
>
> Không đụng gì khác: `ConfigShop.csv` lưu **tên** (`None`, `Counters`) chứ không lưu số, và
> `ShopItemCategory` **không đi qua RPC**. Nếu regenerate SO bằng
> `Tools/ConfigShop/Create ConfigShopSO` thì vẫn ra đúng số mới — nhưng cách đã làm là sửa tay
> 3 dòng để không mất chỉnh tay trong Inspector chưa có trong CSV.

### Bước 2 (tùy chọn) — làm nổi trong Inspector

Thêm `[InspectorName("Counters")]` cho từng giá trị để designer đọc được số trong Inspector.
Cosmetic, không đổi dữ liệu.

### Bước 3 (khuyến nghị làm thêm) — guard lúc build

Editor script kiểm tra: nếu giá trị tường minh của enum ≠ thứ tự tải từ asset thì `Debug.LogError`
ở build. Chống cả trường hợp `ShopItemType` bị renumber sau này.

### Không nên làm

Viết custom drawer serialize enum thành chuỗi — đổi format file, đụng mọi asset, lợi ích thấp.

## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| Asset hiện tại | **Không đổi** (giá trị mới == giá trị cũ). |
| Gameplay / UI | Không đổi. |
| Mạng | Không đổi — wire vẫn là int, nhưng giờ được "chốt". |
| Rủi ro | Gần 0. Đây là pure hardening. |
| Thêm giá trị enum mới | Append sau `Meats`, số nhỏ hơn `None = 9999` (xem lưu ý bên dưới). |

> Lưu ý: `None = 9999` được đặt ở cuối **cả khai báo lẫn số**, nên thêm category mới không bao giờ
> đẩy `None` khỏi cuối. Cách làm: thêm `Drinks = 8` ngay sau `Meats = 7`. Cấm dùng số ≥ 9999.

## Kiểm chứng

1. Compile Android + 49/49 test.
2. Mở shop → mọi tab (`Counters/Tables/Walls/Vegetables/Bakery/Dairy/Patties/Meats`) vẫn hiện
   đúng nhóm, icon đúng.
3. Tutorial xây dựng (`BuildingTutorial.cs:35,51`) vẫn `ScrollTo` đúng tab `Counters`.
4. `git diff` chỉ có 1 file `ShopItemCategory.cs` — **không có file `.asset` nào đổi** (điều kiện
   để biết ghim số không làm dịch dữ liệu).

## Rủi ro / rollback

- Nếu vô tình gán sai số → shop sai nhóm ngay lúc test.
- Rollback: revert 1 file.
