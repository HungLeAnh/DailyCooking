# 08 — Sửa `objectName` của `StewBurnedSO`

| | |
|---|---|
| **Nhóm** | B. Data cần người dùng quyết |
| **Nguồn** | `docs/datamanagement.md` §4.4 |
| **Trạng thái** | ✅ **Đã làm + đã kiểm nấu đến cháy (2026-10-21)** — `objectName: Stew` → `Stew Burned`. **Chủ dự án đã chốt "giữ nguyên" tên này ngày 2026-10-21**, không cần sửa lại asset. Compile Android + 49/49 test, console 0/0. Option menu hiện **`Stew Cooked` / `Stew Burned`**. Đã nấu thật trong nồi: `Cooking` → `Cooked` (`Stew Cooked`) → `Burned` (`Stew Burned`, `stew_bowlBurned(Clone)`, burn timer `5.00`). |
| **Rủi ro** | **Rất thấp** (chỉ hiển thị) |
| **Ưu tiên** | Cao — sửa 1 dòng, hết ngay |

---

## Mục tiêu

`StewBurnedSO.objectName = "Stew"` — món **đã cháy** hiện tên là "Stew", trùng với tên món ăn
`FoodSO.recipeName = "Stew"`.

## Hiện trạng

### Asset

```
StewCookedSO   objectName: "Stew Cooked"   ← đúng
StewBurnedSO   objectName: "Stew"          ← sai
FoodSO "Stew"  recipeName:  "Stew"          ← (không phải objectName, nhưng trùng chữ)
```

### Quy ước của project

Tất cả KitchenObjectSO khác đều có tên mô tả đúng bản chất:

| Asset | objectName |
|---|---|
| `BurgerCookedSO` | `Burger Cooked` |
| `BurgerUncookedSO` | `Burger Uncooked` |
| `BurgerTrashSO` | **`Burger Trash`** ← tiền lệ cho "bản hỏng" |
| `VegetableBurgerCookedSO` | `Vegetable Burger Cooked` |
| `CorndogCooked` | `Corn Dog (Cooked)` |

→ `StewBurnedSO` nên là **`Stew Burned`** để khớp (đã có tiền lệ `Burger Trash`).

### `objectName` được dùng ở đâu — chỉ hiển thị

Tìm toàn bộ project, đúng 2 chỗ, đều gán vào Text:

1. `UIFoodDetailPopup.cs:70` — `tagText.text = ingredient.objectName;` (tag nguyên liệu)
2. `OptionMenuItemUI.cs:19` — `_kitchenObjectName.text = kitchenObjectSO.objectName;`
   (danh sách chọn trong option menu)

**Không có chỗ nào dùng `objectName` làm khoá tra cứu, so sánh hay logic.**

## Thay đổi đề xuất

1. Sửa `StewBurnedSO.asset`: `objectName: Stew` → `objectName: Stew Burned`
   (hoặc đổi trong Inspector).
2. **Không đổi** `_guid`, `prefab`, `Sprite`, `m_Name`.

> Chưa có cơ sở để chọn tên chính xác giữa `Stew Burned` / `Burnt Stew` — bạn chốt.
> Đề xuất: **`Stew Burned`** (đọc theo thứ tự giống `Burger Cooked` / `Burger Uncooked`).

## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| Logic / gameplay | **Không đổi** — chỉ có 2 chỗ gán chuỗi vào Text. |
| Hiển thị | Tên món cháy đúng thay vì đọc như món thường. |
| Save / mạng / prefab? | Không. `_guid` giữ nguyên nên mọi tham chiếu an toàn. |
| Test | Không cần test automation; xem bằng mắt là đủ. |

## Kiểm chứng

1. ✅ Compile Android + 49/49 test (đảm bảo không gõ nhầm YAML).
2. ✅ **Đã làm (2026-10-12, play mode)** — mở `UIOptionMenuPopup` với
   `Param.optionalList = [StewCookedSO, StewBurnedSO]` → hiện đúng **`Stew Cooked`** và **`Stew Burned`**.
   Dùng probe `Tools/Verify/Cooked vs burned stew in option menu` trong `Assets/Editor/PerfScenarios.cs`
   (`UIOptionMenuPopup.ShowPopup(null)` sẽ tự `HidePopup()` nên bắt buộc phải truyền `Param` kèm
   sender `IHasOptionalSO`). Đây chính đường code `OptionMenuItemUI.Setup(index, KitchenObjectSO)`.
3. ✅ **2026-10-21 — nấu thật đến lúc cháy (Editor play mode, host Relay thật):**
   cho `Potato Chopped` vào nồi → chọn option `Stew Cooked` → state `Cooking` (`cookMax = 5`).
   Sau ~5 s: state `Cooked`, food SO `Stew Cooked`. Sau thêm ~5 s: state `Burned`, food SO
   `Stew Burned`, GameObject `stew_bowlBurned(Clone)`, burn timer `5.00`.
   Đây là chuỗi thật của `CookingTool`; setup chỉ đưa nguyên liệu vào nồi bằng API spawn/interact,
   không set state tay. (Giá trị SO cũng được xác nhận thêm bằng log
   `[Perf] cooked='Stew Cooked' burned='Stew Burned'`.)

## Rủi ro / rollback

- Gần bằng 0. Rollback: sửa lại chuỗi.
