# 16 — Tự động hoá input bằng Input System + kiểm kéo ghost bằng "chuột"

Ngày: 2026-10-20. Đây là **tài liệu kiểm chứng**, không phải một mục sửa lỗi: không có dòng code
runtime nào thay đổi. Mục đích là đóng lại khoảng trống "kéo ghost bằng chuột thật vẫn chưa kiểm
được" (README § "Thử tự động hoá S3/S4 … THẤT BẠI", `05-camera-main-remaining.md`) và ghi lại
cách dựng lại.

## 1. Vì sao các lần trước thất bại

Trước đây `InputSystem.QueueStateEvent` / `InputState.Change` cập nhật được **device state**
(`Pointer.current.position`, `Mouse.current.leftButton.isPressed`) nhưng **InputAction không bao
giờ actuate**:

```
pan=(0.00, 0.00) activeCtrl=null ctrlRead=(400.00, 300.00)
```

Tức là control đọc ra đúng toạ độ đã inject, còn `InputAction` bind vào chính control đó vẫn
trả `(0,0)`. Một `InputAction` mới tạo trong lúc chạy cũng vậy → không phải lỗi của `GameInput`.

Nguyên nhân: **`Application.isFocused == false`** trong môi trường chạy tự động này (Unity Editor
không giữ focus), nên `InputManager` chạy ở **Editor update type** thay vì player update type.
Event inject vào bị xử lý theo nhánh editor (`editorInputBehaviorInPlayMode`) và bị bỏ/hoãn, nên
state-change monitor của action không bao giờ chạy.

Bằng chứng đối chiếu trong package `com.unity.inputsystem` (đọc source trong `Library/PackageCache`):

- `InputManager.defaultUpdateType` → `if (!runPlayerUpdatesInEditMode && (!gameIsPlaying ||
  !gameHasFocus)) return InputUpdateType.Editor;`
- `gameHasFocus => runPlayerUpdatesInEditMode || applicationHasFocus ||
  gameShouldGetInputRegardlessOfFocus || isEditorEventPassthroughActive;`
- `gameShouldGetInputRegardlessOfFocus` chỉ đúng khi `backgroundBehavior == IgnoreFocus` **và**
  `editorInputBehaviorInPlayMode == AllDeviceInputAlwaysGoesToGameView`.

## 2. Cách làm đúng (đã dùng để kiểm)

Tạo một `MonoBehaviour` tạm **chỉ trong lúc kiểm**, đặt `[DefaultExecutionOrder(-1000)]` để
`Update()` của nó chạy trước mọi script game:

```csharp
private void Awake()
{
    // buộc Input System coi như đang có player update dù editor mất focus
    InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
    InputSystem.settings.editorInputBehaviorInPlayMode =
        InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

    var im = typeof(InputSystem).GetProperty("manager",
        BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
    im.GetType().GetProperty("runPlayerUpdatesInEditMode").SetValue(im, true);
}

private void Update() // -1000: chạy trước BuildingGhost / GameInput / PlayerStateMachine
{
    var s = new MouseState { position = Position, buttons = Press ? (ushort)1 : (ushort)0 };
    InputState.Change(Mouse.current, s);
    InputSystem.Update();   // action + thay đổi `performed` được xử lý ngay trong frame này
}
```

Điều kiện bắt buộc:

| Điều | Vì sao |
|---|---|
| `runPlayerUpdatesInEditMode = true` | nếu không, `defaultUpdateType` = `Editor` và inject bị bỏ |
| `[DefaultExecutionOrder(-1000)]` | `BuildingGhost.Update()` phải đọc `GetClickPosition()` **sau** khi inject |
| gọi `InputSystem.Update()` ngay sau `InputState.Change` | action `Click` phát `performed` trong cùng frame → `GameInput.StartClick` chạy |
| đọc `Pointer.current.position` từ **trong game** | đọc từ `execute_code` (giữa 2 frame) có thể thấy giá trị native cũ, không phải giá trị đã inject |

Cạm bẫy phụ:

- **Không suy ra toạ độ từ transform của model.** `BuildingGhost`'s `visual.position` là **góc
  dưới** của model; raycast vào đó trượt collider (`anyHit=Plane`). Phải dùng
  `Collider.bounds.center` của preview.
- **`GetMouseWorldSnappedPosition()` clamp theo vùng ô đã mở khoá.** Kéo tới ô ngoài vùng mở khoá
  thì ghost dừng ở ô biên (ví dụ mọi target xa đều về `(8,0,8)`) — trông như "không bám chuột"
  nhưng là `ValidateGridPosition()` đang làm đúng việc.
- **Đừng dùng timer ngắn khi đo `ShakeCamera`.** Mỗi lần `execute_code` chặn main thread, nên nếu
  các lần đọc cách nhau vài trăm ms thì `unscaledDeltaTime` phình to, timer 2 s hết sau 1–2 frame.

## 3. Kết quả kéo ghost (GameScene, host thật, 2026-10-20)

Thiết lập: vào game bằng `NewGame` + `StartHostSessionAsync` + `Loader.LoadNetwork(GameScene)`,
hoàn thành tutorial 8 bước (mở khoá ô đất), mua `Clear Counter`, thêm vào inventory,
`BuildingPlacementManager.SetPlacedObjectTypeSO(so, cell(4,4))`.

| Bước | Thao tác | Kết quả đọc được |
|---|---|---|
| 1 | Pointer tại `Collider.bounds.center` của preview, chưa bấm | `overUI=False`, `ghost.drag=False`, `visual=(8.00, 0.00, 8.00)` |
| 2 | Bấm chuột (`MouseState.buttons = 1`) | `GameInput.LastClickPosition=(1230.00, 927.70)`, `ghost.drag=True`, `visual=(8.00, 1.00, 8.00)` |
| 3 | Rê pointer tới ô (2,2) | `visual=(4.00, 1.00, 4.00)` — **khớp chính xác** |
| 4 | Rê pointer tới ô (0,0) | `visual=(0.00, 1.00, 0.00)` — **khớp chính xác** |
| 5 | Thả chuột (`buttons = 0`) | `ghost.drag=False` |
| 6 | Bấm nút Confirm (click thật qua `EventSystem`) | `ClearCounter(Clone)` **spawned=True** ở `(0,0,0)`, `btnActive=False`, `UIInventoryPopup` mở |

Nghĩa là toàn bộ chuỗi đã đi qua đúng đường thật, không phải gọi hàm nội bộ:

```
MouseState → InputAction "Pan"/"Click" → GameInput.OnMouseClickPerformed
  → BuildingGhost.OnPanStarted (raycast layer BuildingGhost)
  → BuildingGhost.HandleDraging → BuildingPlacementManager.GetMouseWorldSnappedPosition
  → BuildingGhost.OnClickConfirm → PlaceObjectServerRpc → NetworkObject spawn
```

Bước 6 là click UI thật (pointer → `InputSystemUIInputModule` → `Button.onClick`), không phải
`onClick.Invoke()`.

## 4. Giới hạn — đọc kỹ trước khi trích dẫn

- Đây là **pointer tổng hợp qua Input System**, không phải tay người thật. Nó chứng minh logic
  click/kéo/confirm đúng, **không** chứng minh cảm giác kéo mượt hay tốc độ phản hồi.
- Chỉ kiểm trên **host đơn**. Không kiểm client thứ hai / late join.
- Không kiểm kéo ghost trên **điện thoại thật** (touch path `Touch0Pos` / `Touch0Contact` không
  được chạy).
- Script probe `Assets/Scripts/TmpPointerProbe.cs` chỉ tồn tại trong lúc kiểm và **đã xoá**
  (kèm `.meta`) trước khi commit — repo không có file tạm nào.

## 5. Click chuột tổng hợp — caveat bổ sung (2026-10-21)

Khi kiểm S3 (click counter), inject `MouseState`/`Pointer` cập nhật được device nhưng **action
`Click` không tự actuate** — tương tự lý do focus ở §1. Cách chạy được:

1. Rebind runtime action `Click` sang `<Mouse>/leftButton`
   (`InputActionRebindingExtensions.ApplyBindingOverride`).
2. Inject `MouseState` (buttons = 1) + gọi `InputSystem.Update()` trong probe
   `[DefaultExecutionOrder(-1000)]`.

Kết quả: click `(690,538)` → `GameInput.LastClickPosition=(690,538)`,
`PlateTablewareCounter(Clone)` được chọn, `plate(Clone)` spawn vào tay player.

Cạm bẫy:

- **`Pointer.press` một mình không đủ** trong môi trường Editor mất focus; phải rebind action
  như trên. Đây là workaround automation, không phải sửa code game.
- **Action có thể kẹt phase `Performed`/`Waiting` sau lần inject đầu**; các click sau không bắn
  event dù device state đổi. Phải release + `InputSystem.Update()` (hoặc reset action) trước lần
  click kế tiếp.
- **Không suy ra "đã click" từ `LastClickPosition`**: pan camera cũng cập nhật vị trí này. Phải
  kiểm bằng object được chọn / item được spawn / log event.
- Probe `Assets/Scripts/TmpInputProbe.cs` chỉ tồn tại trong lúc đo S3/S7 và **đã xoá** kèm `.meta`.
