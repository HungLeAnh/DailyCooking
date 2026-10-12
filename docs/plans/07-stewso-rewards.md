# 07 — Đặt `price` / `exp` cho món Stew

| | |
|---|---|
| **Nhóm** | B. Data cần người dùng quyết |
| **Nguồn** | `docs/datamanagement.md` §4.4 |
| **Trạng thái** | ✅ **Đã làm + kiểm bằng mắt khi chơi** — `price: 0→35`, `exp: 0→40` (phương án A). Compile Android + 49/49 test, console 0/0. Menu popup hiện **`Stew — $35 / 40 XP`**. **Chưa kiểm: phục vụ Stew cho khách bot rồi thu tiền/XP.** |
| **Rủi ro** | Thấp về kỹ thuật, **cần bạn quyết con số** |
| **Ưu tiên** | Cao — đây là bug gameplay thật (món cao cấp cho 0 tiền) |

---

## Mục tiêu

`StewSO.asset` đang có `price: 0`, `exp: 0`. Món này **có thể mở khoá, có thể bán**, nhưng phục vụ
cho khách bot **không mang lại đồng xu hay exp nào**.

## Hiện trạng

### Asset

`Assets/Configs/.../StewSO.asset`:

```
_guid:        62129fd5079c12a43a0ee71c1e752c79
recipeName:   Stew
price:        0          ← sai
exp:          0          ← sai
unlockPrice:  60         ← cao nhất trong nhóm so sánh
unlockLevel:  6          ← cao nhất trong nhóm so sánh
```

Đã kiểm tra: **Stew là FoodSO duy nhất** trong toàn bộ asset có `price=0` hoặc `exp=0`.

Stew **có trong `ConfigFoodSO.asset`** (tìm thấy `_guid` ở trên) → hiện trong UI menu/shop,
khách bot có thể gọi.

### So sánh với các món khác

| Món | price | exp | unlockPrice | unlockLevel |
|---|---|---|---|---|
| Simple Salad | 5 | 10 | 10 | 1 |
| Onion Salad | 10 | 15 | 20 | 2 |
| Cheese Hamburger | 20 | 25 | 30 | 3 |
| Hamburger Full | 30 | 30 | 40 | 4 |
| Vegetable Hamburger Full | 30 | 35 | 50 | 5 |
| **Stew** | **0** | **0** | **60** | **6** |

Món unlock level 6 (mở khoá đắt nhất trong bảng) trả 0 → gần chắc là chưa điền dữ liệu.

### Dòng tiền bị ảnh hưởng

`BotCustomerController.cs:409-423` — `FinishEating()` chạy **trên server**:

```csharp
foreach (var food in waitingFood) {
    cash += (int)food.price;
    exp  += food.exp;
}
cash += (int)(cash * tipPercentage.Value);
if (targetTable != null) targetTable.SetEatenViual(..., cash, exp);
```

→ Với Stew: `cash = 0`, tip `0 * % = 0`, `exp = 0`. Khách ăn xong, đĩa hiện **$0 / 0 XP**.

Các chỗ hiển thị cũng nói dối người chơi:
- `UIFoodItem.cs:41-42` → `foodPriceText.text = $"${item.price}"` → hiển thị **`$0`**
- `UIMenuItem.cs:19-20` → như trên
- `UIFoodDetailPopup.cs:52-53` → `$0` / `0 XP`

## Thay đổi đề xuất

**Chỉ sửa 2 field trong `StewSO.asset`.** Không sửa code.

### Con số đề xuất (bạn chốt)

Mục tiêu: Stew phải là món **hơn mặt bằng chung ở level 5-6**, vì unlockPrice cao hơn 20% và
unlockLevel cao hơn.

| Phương án | price | exp | Ghi chú |
|---|---|---|---|
| A — Bằng món level 5 rồi tăng nhẹ | **35** | **40** | An toàn, không phá cân bằng |
| B — Tăng rõ theo bậc unlock | **40** | **50** | Stew là "món khó", xứng đáng hơn |
| C — Bạn tự đặt | ? | ? | Nhập đúng giá trị bạn muốn |

Đề xuất **A (35 / 40)** nếu chưa có số liệu; chỉnh sau khi test chơi.

### Cách sửa

1. Mở `StewSO.asset` trong Inspector (Unity) → sửa `Price = 35`, `Exp = 40`.
2. Hoặc sửa trực tiếp YAML: đổi `price: 0` → `price: 35`, `exp: 0` → `exp: 40`.
3. **Không đổi** `_guid` — mọi nơi (grid, save, RPC) tham chiếu theo guid này.

## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| Gameplay | Stew giờ trả tiền/exp. Cân bằng thay đổi nhẹ (người chơi farm Stew sẽ mạnh hơn trước). |
| Hiển thị UI | `$0`/`0 XP` → `$35`/`40 XP` ở 3 chỗ UI. |
| Save? | Không đổi schema. File save cũ đã có Stew thì đọc bình thường, số liệu lấy từ SO. |
| Mạng? | Không — `price`/`exp` chỉ đọc trên server lúc `FinishEating`, không truyền mạng. |
| Test | Cần test thêm: phục vụ Stew cho khách → đĩa hiện đúng tiền. |

## Kiểm chứng

1. ✅ Compile Android + 49/49 test.
2. ✅ **Đã làm (2026-10-12, play mode + Relay host thật)** — mở menu popup thấy tile
   **`Stew — $35 / 40 XP`**. Dùng 2 probe editor trong `Assets/Editor/PerfScenarios.cs`:
   `Tools/Verify/Unlock all dishes + open Menu popup` rồi `Tools/Verify/Hide tutorial + show Stew tile`
   (unlock thẳng `MenuData.AddUnlockedDish` để khỏi phải farm 60 coins). Console runtime cũng log
   `[Perf] stew price=$35 exp=40 unlockLevel=6`.
3. ⬜ Gọi Stew cho khách bot → ăn xong → đĩa hiện `$35 + tip` / `40 XP`. **Cần bạn chơi tay.**
4. ⬜ Thu thập → `RestaurantData.Coins` và `Exp` tăng đúng. **Cần bạn chơi tay.**

## Rủi ro / rollback

- Chỉ rủi ro cân bằng game. Rollback: đổi lại `0/0` (không nên) hoặc con số khác — không mất dữ liệu.
