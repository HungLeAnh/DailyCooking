# 20 — Kéo ghost bằng **touch** (Touchscreen), không phải chuột

Ngày: 2026-10-21. **Tài liệu kiểm chứng**, không sửa code runtime. Nối tiếp
[16-input-automation-and-ghost-drag.md](16-input-automation-and-ghost-drag.md) (đã kiểm bằng chuột).

## Vì sao trước đây chưa kiểm được

Lần trước chỉ inject `MouseState`, nên đường đi thực tế là `<Pointer>/press` + `<Pointer>/position`
của **Mouse**. Trên mobile, `Pointer.current` là `Touchscreen`, và Input System nhận **một** điểm
chạm qua state event kiểu `TouchState` (không phải `TouchscreenState`):

> `Touchscreen.OnStateEvent`: nếu `stateFormat == TouchState.Format` thì event được hiểu là
> **một touch đơn**, được ghép vào `touches[i]` và cập nhật `primaryTouch`.

Vì vậy probe lần này thêm một `Touchscreen` (máy editor không có) rồi queue
`TouchState { touchId, position, phase }` với `Began` → `Moved` → `Ended`.

## Cách dựng lại

1. Vào game bằng `Tools/Profiling/Enter Game (new host)`, hoàn tất tutorial 8 bước để mở khoá ô đất.
2. Thêm `Touchscreen` nếu chưa có: `InputSystem.AddDevice<Touchscreen>()`.
3. Mỗi frame, nếu đang "giữ":
   - frame đầu: `QueueStateEvent(screen, new TouchState { touchId = id, position = p, phase = Began })`
   - các frame sau: cùng `touchId`, `phase = Moved`, `position` mới
   - khi nhả: `phase = Ended`, `position` cuối
   rồi `InputSystem.Update()`.
4. Đặt một ghost: `BuildingGhost` + `BuildingPlacementManager.SetPlacedObjectTypeSO(so, cell)`.
5. Chiếu `bounds.center` của collider ghost ra screen để lấy điểm chạm.

Probe đặt trong `MonoBehaviour` có `[DefaultExecutionOrder(-1000)]` và bật 3 setting như doc 16
(`IgnoreFocus`, `AllDeviceInputAlwaysGoesToGameView`, `runPlayerUpdatesInEditMode`).

## Kết quả

| Bước | Đọc được |
|---|---|
| Trước khi chạm | `IsDragging = false`; `Pointer.current = null`/mouse |
| `Began` tại collider ghost | `IsDragging = **true**`; `Pointer.current = **FastTouchscreen**`; đúng **1** `OnMouseClickPerformed` |
| `Moved` → ô (2,2) | `BuildingGhost.visual.position = (4.00, 1.00, 4.00)` (đúng tâm ô) |
| `Moved` → ô (4,1) | `visual.position = (8.00, 1.00, 2.00)` |
| `Ended` | `IsDragging = **false**` |

Tức toàn bộ đường `Click` + `Pan` + `HandleDraging` chạy được với touch, không cần chuột.

## Ghi chú khi đọc kết quả

- **Không đọc `Pointer.current.position` / `touches[0].position` giữa 2 frame** — giá trị native
  ghi đè sau `InputSystem.Update()` nên có thể thấy toạ độ cũ hoặc `(0,0)`. Chỉ tin log/transform
  đọc **trong** `Update()` (hoặc đọc `visual.position` sau khi ghost đã Lerp xong).
- Ô (0,0) lần này **không dùng được** vì tia từ điểm screen đó trúng `Bot_Customer 3(Clone)` đang
  đứng trước mặt (raycast hit `(0, 2.19, -0.46)`), nên ghost không nhận target mới. Đó là vật cản
  của kịch bản, không phải lỗi touch — đổi sang ô (4,1) thì ghost bám đúng.

## Giới hạn

- Đây là touch **inject**, không phải ngón tay thật trên máy Android; chưa kiểm multi-touch / pinch
  (2 ngón) và chưa kiểm `EventSystem` với `fingerId` trên thiết bị thật.
- Chưa bấm nút Confirm bằng touch; bước Confirm đã kiểm bằng chuột ở doc 16
  (`ClearCounter(Clone)` spawn thật).
- Chỉ host đơn.
