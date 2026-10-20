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

Sau khi sửa:

- `refresh_unity(force)` → `read_console(types=[error,warning])` = **0**.
- `run_tests(EditMode)` = 49/49 pass.
- `Tools/Build Checks/Compile Android Player Scripts` = OK (110 assemblies).
- **Chưa** kiểm được bằng click thật sau khi sửa. Đã thử dựng lại cảnh chơi nhưng bị chặn bởi
  chính môi trường test, không phải bởi thay đổi này:
  1. `GameManager.NewGame()` tạo nhà hàng mới với `widthMin = heightMin = 0` →
     `GridXZ.IsCellUnlocked()` (`Grid/GridXZ.cs:145-150`) trả `false` cho **mọi** ô →
     `CanPlace(..., requireUnlockedCells: true)` (`GridSystem/GridBuildingSystem.cs:284`)
     từ chối mọi lượt đặt. Bản chơi thật mở khoá ô qua tutorial
     (`TutorialManager.cs:112` → `GridBuildingSystem.UnlockGrid()`), còn đường tắt vào game
     thì bỏ qua tutorial. Đã gọi tay `UnlockGrid()` để đặt được counter.
  2. Sau khi đặt, `BuildingGhost` vẫn để lại một bản preview trong scene
     (`BuildingGhostPreview`) nằm **đúng chỗ** counter vừa đặt và có đủ component của prefab
     (kể cả `CookingToolCounterController`). Raycast từ toạ độ màn hình trúng preview
     (`hit=BuildingGhostPreview`) chứ không tới counter thật. Nguy hiểm hơn:
     `FindFirstObjectByType<CookingToolCounterController>()` trả về **controller của preview**,
     nên mọi phép đo đọc trên nó (layer, `_slot`, `GetProgress`) không phải của counter thật —
     một lần đo trước đó đã cho "counter ở layer 7" và suýt bị ghi thành lỗi prefab.
     Layer 7 đúng là `BuildingGhost` trong `TagManager.asset`, tức đúng là preview.

Vì vậy kết luận hiện tại dựa trên: (a) probe gọi thẳng `InteractAlternateEvent` **trước** khi
sửa cho thấy `Cut()` chạy đúng; (b) nhánh quyết định `PlayerStateMachine.cs:200-216` cộng với
`GetProgress()` mới: có `IHasProgress`, thớt có đồ + `IsCuttingActive` → trả `[0,1)` → nhánh
alternate. **Cần một lượt chơi tay** (mua counter → đặt → lấy tomato → 2 click) để xác nhận.

## Rủi ro

- Thấp. Một interface + một nhánh giá trị trả về, không đổi prefab/GUID/tên field.
- Nếu sau này thêm tool **vừa cắt vừa nấu** (vd. thớt + chảo), điều kiện `IsCuttingActive`
  sẽ chọn nhánh cắt trước — cần xem lại lúc đó.
- `InteractAlternateEvent` của `TablewareKitchenObject` và `PostBox` vẫn không tới được
  (cùng nguyên nhân gốc: không implement `IHasProgress`). Chưa sửa vì chưa xác định được
  hành vi mong muốn của hai chỗ đó.
