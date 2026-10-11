# 04 — Giảm Instantiate/Destroy ở UI popup

| | |
|---|---|
| **Nhóm** | A. Mạng / tối ưu (đã hoãn) |
| **Nguồn** | `docs/optimization_plan.md` §3.4 |
| **Trạng thái** | Chưa làm — **một nửa việc đã được làm sẵn** |
| **Rủi ro** | Trung bình (UI regress: item mất listener, hiện sai, item "ma") |
| **Ưu tiên** | Trung bình — chỉ tốn khi **người dùng bấm mở popup**, không per-frame |

---

## Mục tiêu

Giảm GC spike khi mở popup / đổi tab / đổi category: `Destroy` + `Instantiate` hàng loạt và
các LINQ `.Select().Where().OrderBy().ToList()` sinh kèm.

## Hiện trạng

### Việc đã làm sẵn (không cần làm lại)

`UIPopupManager.cs:11,34-71`:

- `uiPopupDictionary` cache popup theo `UIPopupType`.
- `Instantiate(popupData.popupPrefab, ...)` ở `:60` chỉ chạy **lần đầu** mỗi loại popup.
- `SetupPopup()` (`:62`) chỉ gọi **một lần khi tạo**, không gọi lại khi mở lại.

→ **Instance popup root đã được cache sẵn.** `UIUpgradePopup.SetupPopup()` (destroy toàn bộ +
`Instantiate` + 2 chuỗi LINQ ở `UIUpgradePopup.cs:13-43`) chỉ tốn một lần mỗi phiên.

`UICharacterPopup.FillInvetoryItems()` (`UICharacterPopup.cs:169-213`) đã là pool theo kiểu
*grow-only*: chỉ `Instantiate` phần thiếu (`diffCount`), phần thừa dùng `SetInactiveItem()`
giữ lại. **Không cần pool lại.**

### Việc CHƯA làm — churn thật sự

| File | Dòng | Churn |
|---|---|---|
| `UIMenuPopup.cs` | 44-50 → 51-88 | `ShowPopup()` gọi `Initialize()`: `Destroy` toàn bộ category (`:53-56`) + menu item (`:80-83`), `Instantiate` lại từ đầu (`:63`, `:138`). **Mỗi lần mở popup** |
| `UIMenuPopup.cs` | 90-111 | `ShowMenuOfType`: `Destroy` toàn bộ food grid (`:92-95`) + `Instantiate` lại (`:116`). **Mỗi lần bấm category** |
| `UIMenuPopup.cs` | 99-107 | LINQ `.Where().ToList()` mỗi lần đổi category |
| `UICharacterPopup.cs` | 140-157 | badge "unfit" `Destroy` + `Instantiate` mỗi lần `FillInventory` (mỗi lần đổi tab) |
| `UIUpgradePopup.cs` | 22-25, 34-37 | 2 chuỗi LINQ (chỉ lúc tạo popup — chấp nhận được) |
| `UIUpgradePopup.cs` | 55 | `GetComponentsInChildren<UIUpgradeItem>(true)` — mảng mới **mỗi lần mở** |
| `UIOptionMenuPopup.cs` | 74, 97, 146 | `Instantiate` 2 nhánh, `Destroy` ở `:146` |
| `UILoadRestaurantPopup.cs` | 47, 54 | `Destroy` tất cả + `Instantiate` list save |
| `UIRewardPopup.cs` / `UILevelUpPopup.cs` | 36, 51 | `Instantiate` phần thưởng, `Destroy` phần thừa |
| `UIFoodDetailPopup.cs` | 60, 65 | `Destroy` tag + `Instantiate` lại tag nguyên liệu |

Lưu ý: **không có popup nào bị tạo lại root** — tất cả churn nằm ở *con* của popup.

## Thay đổi đề xuất

### Bước 1 — `UIMenuPopup` (nên làm, lợi ích rõ nhất)

1. `Initialize()` (`:51-88`) đổi thành **create-once + refresh**:
   - Lần đầu: build category + menu item, giữ `List<UIMenuCategoryItem>` / `List<UIMenuItem>`.
   - Các lần sau: `SetActive(false)` phần thừa, `Bind(dish)` lại phần đủ, chỉ `Instantiate` khi
     list tăng.
2. `ShowMenuOfType()` (`:90-111`): y hệt — pool `List<UIFoodItem>` cho food grid.
3. Bỏ LINQ `:105-107` — pre-cache `Dictionary<FoodType, List<FoodSO>>` trong `Start()`
   (xem mục 04b / `optimization_plan` §3.4 LINQ).

- Sửa: `UIMenuPopup.cs` (file duy nhất).
- Ảnh hưởng: mở popup / đổi category không còn alloc hàng trăm object; **rủi ro listener**: phải
  gỡ `onClick` cũ hoặc dùng `RemoveAllListeners()` khi `Bind`, nếu không listener cũ chồng lên.

### Bước 2 — `UICharacterPopup.FillInventory` badge

- Pool badge theo `UnfitPrefab`, `SetActive` thay `Destroy` (`:140-157`).
- Ảnh hưởng: chỉ tốn khi đổi tab, tần suất thấp. Có thể bỏ qua nếu bước 1 đã đủ.

### Bước 3 — Các popup mở ít lần

`UILoadRestaurantPopup`, `UIOptionMenuPopup`, `UIRewardPopup`, `UILevelUpPopup`,
`UIFoodDetailPopup` — tần suất thấp, **khuyến nghị bỏ qua** nếu đã đo profiler cho thấy không có spike.

### Bước 4 — `UIUpgradePopup.ShowPopup:55`

Cache mảng `UIUpgradeItem[]` vào field lúc `SetupPopup()`, `ShowPopup` dùng lại.
Sửa 1 dòng, không rủi ro.

## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| GC / spike khi mở popup | Bước 1 giảm rõ (M menu item + N food item mỗi lần mở/bấm). |
| Per-frame perf | **Không đổi** — popup không phải hot path mỗi frame. |
| Rủi ro UI | Trung bình: listener trùng, item hiển thị thừa/thiếu, focus category sai. |
| Đụng save / mạng? | Không. |
| Đụng prefab? | Không — sửa code thuần. |

> Trung thực: nếu profiler chưa chỉ ra `UI instantiate` nằm trong top thì **không nên làm**,
> vì đây là code có rủi ro regresion cao nhất trong nhóm A.

## Kiểm chứng

1. Compile Android + 49/49 test.
2. Chạy tay, checklist:
   - Mở Menu popup 10 lần liên tiếp → danh sách không nhân đôi, số card đúng
     `MenuData.menuDished.Count`.
   - Bấm qua mọi `FoodType` (All / từng loại) → grid đúng, card đã chọn hiện đúng state.
   - Thêm món → card mới hiện, `totalDish` tăng; xoá → card mất, count giảm.
   - Mở Character popup, đổi mọi tab → đủ item, item locked/unlock đúng.
   - Mở Upgrade popup → đủ item, level đọc đúng.
3. So sánh GC Alloc (Profiler → GC Alloc) trước/sau khi mở Menu popup.

## Rủi ro / rollback

- Dễ nhất là listener dính: `Instantiate` mới mà `onClick` vẫn còn handler cũ → bấm 1 cái chạy 2 lần.
  Fix: `RemoveAllListeners()` trước khi `AddListener`, hoặc `UnityEvent` gán thay vì cộng dồn.
- Rollback: một commit riêng cho `UIMenuPopup`, revert nếu checklist fail.
