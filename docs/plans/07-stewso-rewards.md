# 07 — Đặt `price` / `exp` cho món Stew

| | |
|---|---|
| **Nhóm** | B. Data cần người dùng quyết |
| **Nguồn** | `docs/datamanagement.md` §4.4 |
| **Trạng thái** | ✅ **Đã làm + đã kiểm phục vụ/thu tiền (2026-10-21)** — `price: 0→35`, `exp: 0→40` (phương án A). **Chủ dự án đã chốt "giữ nguyên" 2 con số này ngày 2026-10-21**, không cần sửa lại asset. Compile Android + 49/49 test, console 0/0. Menu popup hiện **`Stew — $35 / 40 XP`**. Đã phục vụ Stew cho bot bằng **bát** (`bowl`), thu **54 coins** (35 + tip 19; tip 0.56 sau 1 bậc Sad) và **40 XP**; `Coins 200→254`, `Exp 0→40`. Phát hiện + sửa thêm `StewSO.kitchenObjectSOList`: `[Potato Chopped, Carrot Slices]` → `[Stew Cooked]` (bát chỉ nhận `StewCookedSO`, đĩa không nhận nguyên liệu sống). |
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

### Con số đề xuất (đã chốt)

**2026-10-21: chủ dự án xác nhận "giữ nguyên"** → dùng **phương án A: `price = 35`, `exp = 40`**
(asset hiện đang đúng 2 giá trị này, không phải sửa gì thêm).

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

### Sửa thêm (phát hiện khi kiểm phục vụ, 2026-10-21)

`StewSO.kitchenObjectSOList` đang là `[PotatoChoppedSO, CarrotSlicesSO]` — **không khớp** với
tableware thực tế:

- `bowl.prefab` có `validKitchenObjectSOList = [StewCookedSO]`.
- `plate.prefab` **không** nhận `Potato Chopped`, `Carrot Slices` hay `Stew Cooked`.

Vì `BotCustomerController.IsServerCorrectFood()` so **đúng số lượng + từng SO** với
`waitingFood.kitchenObjectSOList`, bot order Stew sẽ **không thể được phục vụ** với dữ liệu cũ.
Đã đổi list thành `[StewCookedSO]`; giữ nguyên `price = 35`, `exp = 40`, `_guid`.
Sau khi reimport asset, đã chạy lại một phiên play mode mới: order → serve → collect vẫn đúng
(`54 coins`, `40 XP`).

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
3. ✅ **2026-10-21 — phục vụ Stew cho bot (Editor play mode, host Relay thật):**
   bot order `Stew` → `waitingFood` có 1 món, `kitchenObjectSOList = [Stew Cooked]`.
   Player cầm `bowl(Clone)` chứa `Stew Cooked`, tương tác bot → bot sang `Eating`, bát
   `served=True`. Sau 5 s: bát `eaten=True`, `pendingCash=54`, `pendingExp=40`.
4. ✅ **2026-10-21 — thu tiền:** tương tác bát đã ăn → `RestaurantData.Coins 200→254`,
   `Exp 0→40`.

**Điều kiện / giới hạn của lần kiểm:**

- Bot được đặt thẳng vào ghế + set state `OrderFood` để tránh timeout 15 s/bậc cảm xúc; các bước
  order/serve/collect gọi đúng `InteractEvent` của game, **không phải click chuột**. Mục này kiểm
  **logic phục vụ + reward**, không kiểm đường click/raycast (S3 kiểm riêng 1 click thật).
- Stew phải phục vụ trong **bát**, không phải đĩa.
- `54 coins` = `35` giá món + `19` tip. Tip gốc `0.7`; bot bị 1 bậc Sad trong lúc chờ nên còn
  `0.56` → `(int)(35 × 0.56) = 19`.
- Đã chạy lại sau khi sửa `StewSO.kitchenObjectSOList` và reimport asset (phiên mới), kết quả
  `pendingCash=54`, `pendingExp=40`, `Coins 200→254`, `Exp 0→40`.

## Rủi ro / rollback

- Chỉ rủi ro cân bằng game. Rollback: đổi lại `0/0` (không nên) hoặc con số khác — không mất dữ liệu.
