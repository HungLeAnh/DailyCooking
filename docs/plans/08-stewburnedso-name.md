# 08 — Sửa `objectName` của `StewBurnedSO`

| | |
|---|---|
| **Nhóm** | B. Data cần người dùng quyết |
| **Nguồn** | `docs/datamanagement.md` §4.4 |
| **Trạng thái** | Chưa làm — đã xác minh |
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

1. Compile Android + 49/49 test (đảm bảo không gõ nhầm YAML).
2. Chạy game → nấu Stew cháy → cầm/hiển thị → tên hiện `Stew Burned`.
3. Vào option menu có món cháy → nhãn đúng.

## Rủi ro / rollback

- Gần bằng 0. Rollback: sửa lại chuỗi.
