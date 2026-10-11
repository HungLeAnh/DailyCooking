# 01 — Bỏ chuỗi ServerRpc → ClientRpc

| | |
|---|---|
| **Nhóm** | A. Mạng / tối ưu (đã hoãn) |
| **Nguồn** | `docs/optimization_plan.md` §3.1 |
| **Trạng thái hiện tại** | ⚠️ Mục như mô tả trong optimization_plan **đã lỗi thời** — cần đọc lại trước khi làm |
| **Rủi ro** | Cao |
| **Ưu tiên** | Không làm theo đúng mô tả gốc; làm theo "hướng thay thế" bên dưới nếu muốn |

---

## Mục tiêu

Giảm số RPC gửi đi khi server thay đổi state mà client tự cập nhật được.

## Hiện trạng

### Mô tả gốc sai

`optimization_plan.md` nói `ContainerCounterController.cs:154-168`, `Table.cs` và `KitchenObject.cs`
dùng pattern `ServerRpc` làm không gì trên server rồi gọi ngay `ClientRpc` để báo client, và gợi ý
thay bằng `NetworkVariable.OnValueChanged`.

Đối chiếu code:

- `Table.cs`, `KitchenObject.cs`, `BaseContainerCounterController.cs` — **không có bất kỳ RPC nào**.
- `ContainerCounterController.cs` / `OptionalContainerCounterController.cs` — chỉ có comment nhắc
  `PlayerStateMachine.InteractServerRpc`, không khai báo RPC.

Toàn bộ RPC trong project dùng attribute mới `[Rpc(SendTo.X)]` (NGO 2.x), không còn
`[ServerRpc]`/`[ClientRpc]`. Danh sách đầy đủ: 55 chỗ, tập trung ở
`GameManager.Data.cs`, `GameManager.Economy.cs`, `GridBuildingSystem.cs`, `MultiplayerManager.cs`,
`UIManager.cs`, `CookingTool.cs`, `PlayerStateMachine.cs`.

### Chuỗi RPC thật sự còn tồn tại

Chúng nằm ở `GameManager.Data.cs` và `GameManager.Economy.cs`, vd `GameManager.Data.cs:11-25`:

```csharp
[Rpc(SendTo.Server)]
public void UpdateRestaurantNameServerRpc(string name) {
    GameData.RestaurantData.UpdateRestaurantName(clean);
    UpdateRestaurantNameClientRpc(clean);          // mirror sang client khác
}
[Rpc(SendTo.NotServer)]
private void UpdateRestaurantNameClientRpc(string name) {
    GameData?.RestaurantData.UpdateRestaurantName(name);
}
```

Pattern này **không thừa**: `GameData` là object C# thuần (`Managers/GameData.cs`), không phải
`NetworkVariable`. Server ghi vào `GameData` của mình, client không tự biết, nên bắt buộc phải có
RPC mirror. Nếu bỏ `ClientRpc` mà không làm gì khác, client sẽ không bao giờ thấy đổi tên nhà hàng,
thêm món, trừ tiền, mở khoá...

Các RPC mirror cần giữ (13 cặp Server/Client trong 2 file trên): đổi tên nhà hàng, thêm/xoá món
trong menu, thêm/xoá inventory, gỡ PostBox, lưu customization, nhận thưởng, mua upgrade, trừ
currency, đổi exp/level.

## Thay đổi đề xuất (hướng thay thế)

Muốn thật sự bớt RPC thì phải đưa `GameData` lên network, **đây chính là mục 02**. Tách làm 2 bước:

1. **Bước 1 — gộp mirror, không đổi mô hình.** Với các thao tác hay xảy ra (inventory, exp/level,
   currency), gộp nhiều RPC nhỏ thành một RPC mang DTO, hoặc gom theo lô mỗi khung hình.
   - Sửa: `GameManager.Data.cs:62-100` (`ServerAddInventory`/`ServerRemoveInventory` gộp thành
     `ServerApplyInventoryDelta(guid, delta)`).
   - Ảnh hưởng: giảm số message, không đổi cách client nhận data.
2. **Bước 2 — chuyển sang `NetworkVariable`/`NetworkList`** cho các field có tần suất cao
   (`RestaurantData.Coins/Gems/Exp/Level`, `InventoryData`, `MenuData`). Xem mục 02.
   - Sửa: `GameData` giữ các wrapper networked; `GameManager.Data.cs` bỏ toàn bộ `*ClientRpc`.
   - Ảnh hưởng: **cực lớn** — save, join sync, UI binding, RPC hiện có đều phải đổi.

## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| Bước 1 | Nhẹ. Đổi nội dung RPC, giữ nguyên mô hình. Cần test chơi 2 client. |
| Bước 2 | Nặng. Đụng save/load, join snapshot, mọi UI đọc `GameManager.Instance.GameData`. |
| Bandwidth | Bước 2 giảm rõ; bước 1 giảm vừa. |
| Break save? | Bước 1 không. Bước 2 không (lưu file không đổi), nhưng **protocol join đổi** — host và client phải cùng bản. |

## Kiểm chứng

1. `Tools → Build Checks → Compile Android Player Scripts`.
2. EditMode tests: 49/49.
3. Chạy Multiplayer Play Mode: host + 1 client + 1 late joiner.
   - Host đổi tên nhà hàng → client thấy ngay.
   - Client mua item → host và client kia thấy inventory/exp đổi.
   - Late joiner thấy đúng trạng thái sau khi sync.

## Rủi ro / rollback

- Bước 2 rất dễ làm mất sync âm thầm (viết `NetworkVariable` từ client, hoặc ghi `GameData` cũ
  song song với `NetworkVariable` mới).
- Rollback: mỗi bước một commit riêng, revert commit tương ứng.
