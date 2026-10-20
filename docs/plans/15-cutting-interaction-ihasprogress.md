# 15 — Cắt trên thớt không bấm được (thiếu `IHasProgress`)

Mục phát sinh trong lúc chơi thật để đo S7 (không nằm trong 14 mục ban đầu).

## Hiện trạng

### Cổng vào `InteractAlternateEvent`

`DailyCooking/Assets/Scripts/Player/PlayerStateMachine.cs:196-217`:

```csharp
else if (interactableObject == Context.SelectedInteractableObject)
{
    IHasProgress progress = Context.SelectedInteractableObject as IHasProgress;
    if (progress == null)                       // -> InteractEvent (đặt / lấy)
    ...
    if (progress.IsDone() || progress.GetProgress() == -1)
        RequestInteract(Context.SelectedInteractableObject, false);
    else
        RequestInteract(Context.SelectedInteractableObject, true);   // -> InteractAlternateEvent
}
```

`RequestInteract(..., true)` là **đường duy nhất** trong toàn bộ `Assets/Scripts` gọi
`IInteractable.InteractAlternateEvent` (`PlayerStateMachine.cs:473-474` — kiểm bằng
`grep InteractAlternateEvent`).

### Bên nhận

`DailyCooking/Assets/Scripts/MVC/Controllers/CounterControllers/CookingToolCounterController.cs:5`

```csharp
public class CookingToolCounterController : ClearCounterController, IHasOptionalSO
```

Class **có sẵn** `GetProgress()` (`:163`) và `IsDone()` (`:169`) đúng chữ ký `IHasProgress`
(`Player/IHasProgress.cs`) nhưng **không khai báo interface** → `as IHasProgress` luôn trả `null`
cho counter này → nhánh alternate không bao giờ chạy → `CookingTool.Cut()` (`Counters/CookingTool.cs:161`)
không bao giờ được gọi.

### Đây là hồi quy, không phải thiết kế

`git show 9e3c5fe4e` (refactor MVC, gộp counter) cho thấy trước đó:

```csharp
public class CuttingCounterController : BaseCounterController, IHasProgress, IHasOptionalSO
public class StoveCounterController : BaseCounterController, IHasProgress
```

Khi gộp hai class thành `CookingToolCounterController`, hai method được giữ lại nhưng
`IHasProgress` bị rơi khỏi danh sách interface.

## Ảnh hưởng

- **Không cắt được** nguyên liệu: đặt tomato lên `CuttingBoard` xong bấm chuột thì
  `InteractEvent` chạy → vì thớt đang có đồ và tay đang trống nên nó **nhấc tomato trả lại**
  (`CookingToolCounterController.cs:146-150`). Thớt không bao giờ tiến triển.
- Không có `TomatoSlices`/`CarrotSlices`… → không nấu được món → không phục vụ được khách
  → **không lên được Level 2** (Level 2 mới mở Stove/Fridge/Double Table trong shop).
- Các `InteractAlternateEvent` khác (`TablewareKitchenObject.cs:113`, `PostBox.cs:65`) cũng
  không tới được, nhưng hiện chúng không phải đường chính của gameplay.
- `CookingTool` tự nó implement `IHasProgress` nhưng `CookingTool` **không** phải `IInteractable`,
  và click lên mesh của tool được forward sang counter (`PlayerStateMachine.cs:178-180`),
  nên không có đường vòng nào.

## Thay đổi

`DailyCooking/Assets/Scripts/MVC/Controllers/CounterControllers/CookingToolCounterController.cs`

1. Khai báo lại interface:

```csharp
public class CookingToolCounterController : ClearCounterController, IHasOptionalSO, IHasProgress
```

2. `GetProgress()` trả `-1` khi **không có** hành động alternate:

```csharp
public float GetProgress()
{
    if (!TryResolveTool() || !_cookingTool.HasKitchenObject() || !_cookingTool.IsCuttingActive)
        return -1f;
    return _cookingTool.GetProgress();
}
```

Lý do phải làm bước 2: nếu chỉ thêm interface mà giữ `return _cookingTool.GetProgress();` thì
mọi tool **không phải** thớt (nồi/chảo) đang có đồ sẽ trả `0` → `PlayerStateMachine` chọn
alternate → `Cut()` return sớm → người chơi **không lấy lại được đồ** và không đặt được
nguyên liệu lên chảo. `-1` là giá trị `PlayerStateMachine.cs:209` đã dành riêng cho
"không có alternate".

`IsDone()` giữ nguyên (`_cookingTool.IsDone()`).

## Ảnh hưởng sau khi sửa

- Thớt có đồ + có recipe cắt → `GetProgress()` ∈ `[0,1)` → mỗi click là **một nhát cắt**
  (`CookingTool.Cut`, cộng `CookingSpeed` vào `_netCookingTimer`); đủ `cuttingProgressMax` thì
  `Cut` tự destroy input và spawn output.
- Cắt xong, `IsCuttingActive` = false (output không còn recipe cắt) → `GetProgress()` = `-1`
  → click kế tiếp là `InteractEvent` → nhấc slices ra. Đúng như mong đợi.
- Nồi/chảo, counter trống, counter có đồ mà tay đang trống: `-1` → `InteractEvent` như cũ,
  **không đổi hành vi**.
- `IHasProgress` chỉ được dùng ở `PlayerStateMachine.cs:200`; không nơi nào khác bị ảnh hưởng.

## Kiểm chứng

Đã làm (play mode, trước khi sửa):

- Đặt tomato lên `CuttingBoard` bằng click thật → `CookingTool._slot.HasFood = True`,
  `Current = food_ingredient_tomato(Clone)`.
- Click lần 2 lên đúng thớt → tomato bị **nhấc lại vào tay** (đúng như phân tích), `GetProgress() = 0`.
- Gọi thẳng `CookingToolCounterController.InteractAlternateEvent(player)` (bỏ qua cổng
  `IHasProgress`) → `GetProgress()` nhảy `0.2` rồi `0.4989`, tức `CookingTool.Cut()` chạy đúng
  và `cuttingProgressMax = 5`. Vậy lỗi nằm đúng ở cổng interface, không nằm trong `Cut()`.

Sau khi sửa (build check, commit `caddb06fe`):

- `refresh_unity(force)` → `read_console(types=[error,warning])` = **0**.
- `run_tests(EditMode)` = 49/49 pass.
- `Tools/Build Checks/Compile Android Player Scripts` = OK (110 assemblies).

Sau khi sửa (play mode, **đi đúng thứ tự mở khoá**, không dùng đường tắt):

1. Panel tutorial `GameMachanic` (8 bước) hiện ra sau `UIMainMenuPopup` → NewRestaurant.
   Bấm qua hết 8 bước → `TutorialManager` đóng panel → `TutorialData.HasPlayedFirstTime = True`
   và `GridXZ.IsCellUnlocked(0,0) = True`, `IsCellUnlocked(2,2) = True`.
   Đây chính là bước mà lượt test trước đã bỏ qua: `NewGame()` một mình tạo nhà hàng
   `widthMin = heightMin = 0` nên `IsCellUnlocked` false với **mọi** ô →
   `CanPlace(..., requireUnlockedCells: true)` (`GridSystem/GridBuildingSystem.cs:284`)
   từ chối mọi lượt đặt. Đó là giới hạn của đường tắt test, **không phải** lỗi của bản sửa.
2. Mua trong shop: Container Counter (10) + Clear Counter (30) + Cutting Board Tool (40) +
   Tomatoes (15) → coin 200 → 105.
3. Đặt: `ContainerCounter@(0,0,0)`, `ClearCounter@(2,0,0)`, `CuttingBoardTool@(3,1,1)`;
   cả ba `NetworkObject.IsSpawned = True`, layer 6 (`Building`). Preview `BuildingGhostPreview`
   đã bị huỷ sau lượt đặt, nên tia click trúng `ClearCounter(Clone)` thật.
4. Kết quả từng click (tia phóng từ toạ độ màn hình của collider thật):

   | click | `_slot.Current` | `CookingTimer` | `GetProgress()` | nhánh đã chạy |
   | --- | --- | --- | --- | --- |
   | 1 | `TomatoSO` | 0 | 0 | `InteractEvent` — đặt tomato lên thớt |
   | 2 | `TomatoSO` | 3 | 0.6 | `InteractAlternateEvent` → `Cut()` |
   | 3 | `TomatoSO` | 4 | 0.78 | `Cut()` |
   | 4 | `TomatoSO` | 5 | — | `Cut()` đủ `cuttingProgressMax = 5` |
   | 5 | `TomatoSliceSO` | 0 (reset) | -1 | `InteractEvent` — nhấc `food_ingredient_tomato_slice` vào tay |

   Tức là: `IHasProgress` đã có tác dụng (click 2–4 cắt thật), `Cut()` spawn đúng output, và
   nhánh `-1` mới giữ được hành vi "cắt xong thì nhấc ra" thay vì alternate vô ích.
5. Console sau lượt chơi: **0 error**, 1 warning `Look rotation viewing vector is zero`
   (`PlayerStateMachine.cs:334`) — warning có sẵn từ trước, đã ghi ở `13-profiler-baseline.md`.

### Những phần vẫn là thao tác test, không phải chơi tay 100%

Ghi rõ để không ai đọc phần trên thành "đã chơi tay toàn bộ":

- **Mua**: bấm `ShopButton.onClick` để mở shop, rồi gọi thẳng
  `ShopManager.Instance.OnPurchase(config, config.Rewards)` — **bỏ qua** nút Buy và popup
  xác nhận (nút Buy chỉ mở popup, không mua ngay — xem `UIShopItem.cs:82-94`).
- **Đặt counter**: gọi `BuildingPlacementManager.SetPlacedObjectTypeSO(so, target)` +
  `TryPlaceBuildingObject(target)` — **không** kéo ghost bằng chuột (pointer của Input System
  không bám con trỏ trong môi trường này).
- **Tomato**: `KitchenObject.SpawnKitchenObject(TomatoSO, player)`. PostBox có trong scene ở
  `(1,0,-1)` nhưng lượt này không đi chuỗi PostBox → package → Container Counter.
- **Click**: phát lại `GameInput.OnMouseClickPerformed` qua reflection, không phải chuột thật.
  Bù lại, toạ độ là toạ độ màn hình của collider **thật** và tia raycast trúng
  `ClearCounter(Clone)`, nên chuỗi
  `raycast → Context.SelectedInteractableObject → as IHasProgress → InteractAlternateEvent`
  là chuỗi production, không phải gọi tắt method.

Ảnh kiểm chứng: `%TEMP%\opencode\dailycooking-verify\cutting-after-fix-tomato-slices.png`
(slice trong tay player, hierarchy `food_ingredient_tomato_slice(Clone)`) — **không commit**.

## Kết luận

Fix đã được xác nhận bằng vòng lặp đầy đủ trong play mode: đặt → cắt 3 nhát → ra slices →
nhấc slices. Trước khi sửa, cùng cảnh đó click thứ 2 chỉ nhấc tomato trả lại tay.

## Rủi ro

- Thấp. Một interface + một nhánh giá trị trả về, không đổi prefab/GUID/tên field.
- Nếu sau này thêm tool **vừa cắt vừa nấu** (vd. thớt + chảo), điều kiện `IsCuttingActive`
  sẽ chọn nhánh cắt trước — cần xem lại lúc đó.
- `InteractAlternateEvent` của `TablewareKitchenObject` và `PostBox` vẫn không tới được
  (cùng nguyên nhân gốc: không implement `IHasProgress`). Chưa sửa vì chưa xác định được
  hành vi mong muốn của hai chỗ đó.
