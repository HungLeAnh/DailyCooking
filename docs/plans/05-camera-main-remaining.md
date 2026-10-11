# 05 — Cache 16 chỗ `Camera.main` còn lại

| | |
|---|---|
| **Nhóm** | A. Mạng / tối ưu (đã hoãn) |
| **Nguồn** | `docs/optimization_plan.md` §3.5 |
| **Trạng thái** | Chưa làm (mục 1-2 chỗ đầu đã làm trong commit `bcb3d4b6f`) |
| **Rủi ro** | **Thấp** — nhóm an toàn nhất trong 13 mục |
| **Ưu tiên** | Cao, nếu phải chọn một việc trong nhóm A để làm |

---

## Mục tiêu

`Camera.main` là tìm theo tag: duyệt toàn bộ `FindGameObjectsWithTag` rồi filter. Thay bằng cache
đã làm sẵn với 2 class kia.

## Hiện trạng

Đã làm rồi (commit `bcb3d4b6f`) — pattern chuẩn để copy:

```csharp
// LookAtCamera.cs:16-17 và Window_QuestPointer.cs:31-32
private Camera MainCamera => _mainCamera != null ? _mainCamera : _mainCamera = Camera.main;
```

Còn **16 chỗ gọi thật** (đã trừ comment):

### Nhóm 1 — chạy mỗi frame (quan trọng)

| File | Dòng | Bối cảnh |
|---|---|---|
| `BuildingPlacementManager.cs` | 107 | `GetMouseWorldSnappedPosition()` |
| `BuildingGhost.cs` | 132, 160 | `Update()` **gọi** `GetMouseWorldSnappedPosition()` → **2 lượt `Camera.main` mỗi frame khi kéo ghost** |

Đây là **đường duy nhất thật sự per-frame**. Lưu ý `BuildingGhost.cs:11` đã có sẵn
`[SerializeField] private Camera _camera` và `:99` đã dùng `_camera` — chỉ còn `:132/:160` đi qua
`BuildingPlacementManager` nên vẫn chạm `Camera.main`.

### Nhóm 2 — mỗi frame khi rung màn hình

| File | Dòng | Bối cảnh |
|---|---|---|
| `UtilsClass.cs` | 603 | `ShakeCamera()` — `FunctionUpdater` mỗi frame, **2 lần** (`Camera.main` đứng 2 phía của biểu thức) |

### Nhóm 3 — mỗi cú click / touch (không per-frame)

| File | Dòng | Handler |
|---|---|---|
| `UtilsClass.cs` | 226, 240, 249, 255 | `GetMouseWorldPosition*` (drag + click) |
| `UtilsClass.cs` | 561, 568, 581 | `GetWorldPositionFromUI*` |
| `UtilsClass.cs` | 1576, 1585 | `ScreenToWorldPoint` (mouse + touch) |
| `BuildingPlacementManager.cs` | 107 | (dẫn xuất từ `BuildingGhost.Update`) |
| `GridBuildingSystem.cs` | 188 | `GameInput_OnMouseClickPerformed` — mỗi click |
| `PlayerStateMachine.cs` | 170 | `PlayerStateMachine_OnMouseClickPerformed` — mỗi click |
| `GameInput.cs` | 188 | `CheckTouchOverBuildingGhost` — mỗi touch |
| `DOTweenHandClick.cs` | 93 | mỗi lần tween |
| `Button_Sprite.cs` | 155 | fallback 1 lần khi set callback |

> `optimization_plan` §3 mục 5 ghi **17 chỗ** và nói đúng là *"event-driven rather than
> per-frame"*. Đếm lại bằng grep: **16 chỗ gọi thật** (20 dòng khớp, trừ 2 dòng comment + 2 dòng
> đã sửa ở `LookAtCamera` / `Window_QuestPointer`). Không có mâu thuẫn với phân tích ở trên —
> nhưng nhóm 1+2 vẫn là per-frame thật, nên vẫn đáng làm trước.

## Thay đổi đề xuất

**Làm 1 lần, 2 bước:**

### Bước 1 — thêm cache dùng chung (5 dòng)

Ở `UtilsClass` (hoặc class mới `Common/MainCamera.cs`):

```csharp
private static Camera _mainCamera;
public static Camera MainCamera =>
    _mainCamera != null ? _mainCamera : _mainCamera = Camera.main;
```

Giữ đúng pattern đã dùng ở `LookAtCamera.cs:17` / `Window_QuestPointer.cs:32`.

### Bước 2 — thay 16 chỗ

- `UtilsClass.cs` 11 chỗ → dùng `MainCamera` (method nội bộ) / `UtilsClass.MainCamera` (nơi khác).
- `BuildingPlacementManager.cs:107` → `UtilsClass.MainCamera`, **hoặc** nhận `Camera` qua tham số
  từ `BuildingGhost` (vì nó đã có `_camera`) — cách này sạch hơn.
- `GridBuildingSystem.cs:188`, `PlayerStateMachine.cs:170`, `GameInput.cs:188`,
  `DOTweenHandClick.cs:93`, `Button_Sprite.cs:155` → `UtilsClass.MainCamera`.

**Không đổi chữ ký public** của `GetMouseWorldPosition()` / `GetWorldPositionFromUI()` /
`GetMouseWorldSnappedPosition()` → toàn bộ caller giữ nguyên.

### Bước 3 (tùy chọn) — gộp 2 lượt ở `UtilsClass.cs:603`

```csharp
Camera cam = MainCamera;
Vector3 p = cam.transform.position;
cam.transform.position = p - lastCameraMovement + randomMovement;
```

## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| Perf | Nhóm 1: bỏ được 2 lookup tag/frame đang kéo ghost. Nhóm 2: −2 lookup/frame khi rung. Nhóm 3: bỏ mỗi cú bấm. |
| Risk | **Thấp.** Không đổi hành vi, không đổi chữ ký, không đụng save/mạng/prefab. |
| Rủi ro duy nhất | Camera chính **đổi giữa chừng** (chuyển scene, đổi camera) → cache cũ. Xem mục "Bảo hiểm" bên dưới. |
| Deviation | Nếu `Camera.main` trả `null` (chưa tag MainCamera) thì `_mainCamera` cache `null` và mỗi lần vẫn lookup — không xấu hơn hiện tại. |

### Bảo hiểm với camera đổi giữa chừng

Hiện project có `CameraController.cs` (`_camera` serialise sẵn) và không có pattern đổi camera runtime.
Nếu sau này thêm switch camera, thêm 1 dòng invalidate:

```csharp
public static void InvalidateMainCameraCache() => _mainCamera = null;
```

gọi từ chỗ đổi camera. Chưa cần thiết lúc này.

## Kiểm chứng

1. Compile Android player + 49/49 EditMode test.
2. Play mode tay:
   - Kéo building ghost quanh map → ghost bám chuột mượt, vị trí đặt không đổi so với trước.
   - Click vào counter / cutting board / cooking tool → tương tác đúng.
   - Đặt và xoá object từ inventory (`UIInventoryPopup.cs:238`) → snap đúng ô.
   - Gọi `ShakeCamera` → vị trí camera vẫn đúng, không bị lệch dần (so sánh trước/sau 2s rung).
   - UI button `Button_Sprite` click vẫn hoạt động.
3. Bật Profiler → `FindGameObjectsWithTag` (tag: MainCamera) phải không còn hiện ở
   `BuildingGhost.Update`.

## Rủi ro / rollback

- Gần như zero. Nếu có lỗi chỉ là chỗ bị thay sai biến.
- Rollback: một commit duy nhất, revert là về nguyên trạng.
