# 19 — `OnMouseClickPerformed` có 3 subscriber: có phải 1 click ra 3 nhát cắt?

Ngày: 2026-10-21. Đây là **tài liệu điều tra**, không sửa code runtime.

| | |
|---|---|
| **Trạng thái** | ✅ Đã điều tra + kiểm bằng máy — **không có bug**, không sửa gì |
| **Nguồn** | Ghi nhận ở [05-camera-main-remaining.md](05-camera-main-remaining.md) và [13-profiler-baseline.md](13-profiler-baseline.md) ("một lần phát click cho ra 3 nhát cắt") |

## 3 subscriber là ai

| # | Nơi đăng ký | Xử lý khi click | Có cắt? |
|---|---|---|---|
| 1 | `BuildingGhost.cs:31` → `OnPanStarted` | Bắt đầu kéo ghost đang đặt | Không |
| 2 | `GridBuildingSystem.cs:161` → `GameInput_OnMouseClickPerformed` | Chọn/bỏ chọn object đã đặt | Không |
| 3 | `PlayerStateMachine.cs:104` → `PlayerStateMachine_OnMouseClickPerformed` | Raycast, chọn counter, gọi `RequestInteract` | **Chỉ đường này** |

Ba handler có việc khác nhau và đều có guard riêng (`BuildingGhost` thoát nếu không đặt object,
`GridBuildingSystem` thoát nếu không ở chế độ build). Không handler nào gọi `InteractAlternateEvent`
ngoài `PlayerStateMachine`, và mỗi lần click chỉ gọi `RequestInteract` **một** lần
(`PlayerStateMachine.cs:462` → `InteractServerRpc`).

## Kiểm bằng máy

Probe tạm (đã xoá trước khi commit) đăng ký thêm một listener vào
`GameInput.Instance.OnMouseClickPerformed` để đếm số lần event bắn ra cho **mỗi** lần nhấn chuột,
rồi inject `MouseState` + `InputSystem.Update()` (công thức ở
[16-input-automation-and-ghost-drag.md](16-input-automation-and-ghost-drag.md)).

| Lần nhấn | Số event đếm được |
|---|---|
| Nhấn 1 (nhả giữa hai lần) | **1** |
| Nhấn 2 | **1** |
| Nhấn 3 | **1** |

Sau đó dựng đúng kịch bản cắt: đặt `Clear Counter` ở ô (2,2), đặt `Cutting Board Tool` lên trên,
spawn 1 tomato vào tay player, click lần 1 để đặt tomato lên thớt, rồi click từng nhát:

| Click cắt | `OnMouseClickPerformed` | `CookingTool.CookingTimer` |
|---|---|---|
| Nhát 1 | 1 | 0 → **1** |
| Nhát 2 | 1 | 1 → **2** |
| Nhát 3 | 1 | 2 → **3** |

Tức **1 click = 1 event = 1 nhát cắt**. Con số "3 nhát" trước đây là 3 lần click (tomato cần nhiều
nhát), không phải 1 click bị nhân 3.

## Kết luận

- Không sửa code, không gộp/giảm subscriber: cả 3 là hợp lệ và tách trách nhiệm.
- Ghi nhận để lần sau không điều tra lại: muốn đếm "mấy nhát cắt" thì phải đếm **số click** hoặc
  đọc `CookingTimer`, đừng suy từ số subscriber.

## Giới hạn

- Đo trên **host đơn**, input **inject qua Input System**, không phải tay người trên điện thoại.
- Chưa kiểm trường hợp 2 client: mỗi client chỉ điều khiển avatar `IsOwner` của mình, nên số event
  trên một client vẫn là 1; chưa có bằng chứng đo cho nhiều client.
