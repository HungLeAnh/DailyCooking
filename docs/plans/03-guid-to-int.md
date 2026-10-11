# 03 — Đổi GUID string trong payload RPC thành số / 16 byte

| | |
|---|---|
| **Nhóm** | A. Mạng / tối ưu (đã hoãn) |
| **Nguồn** | `docs/optimization_plan.md` §3.3 |
| **Trạng thái** | Chưa làm |
| **Rủi ro** | Trung bình–cao (đụng save & place logic) |
| **Ưu tiên** | Thấp — lợi ích mỗi gói tin chỉ vài chục byte |

---

## Mục tiêu

Mỗi lần client gửi yêu cầu (đặt đồ, mua, mở khoá, thêm món) đều kèm GUID dạng **chuỗi 32 ký tự
hex**. Thay bằng 4 hoặc 16 byte để giảm kích thước gói tin.

## Hiện trạng

### GUID là gì trong project

`Assets/Scripts/UI/UIPopup/Popups/InventoryPopup/SerializableScriptableObject.cs`:

```csharp
[SerializeField, ReadOnly] private string _guid;
public string Guid => _guid;
// OnValidate (editor): _guid = AssetDatabase.AssetPathToGUID(path);
```

→ `.Guid` **chính là Unity asset GUID**, 32 ký tự hex, được ghi thẳng vào asset YAML
(`_guid: bc571c2f81ae71245b19e8a3748b8b72`) và **được lưu trong file save**.

> Sửa số liệu cũ: `optimization_plan` §3 mục 3 ghi *"36-character content GUIDs"* — thực tế là
> **32 ký tự** (`System.Guid` kiểu `8-4-4-4-12` mới là 36). Ảnh hưởng: số byte tiết kiệm được
> nhỏ hơn một chút so với tính theo 36.

### Các RPC đang chở GUID

| File | Dòng | RPC |
|---|---|---|
| `GridBuildingSystem.cs` | 364-365 | `PlaceObjectServerRpc(string placedObjectTypeSOGuid, Vector2Int, Dir)` |
| `GameManager.Data.cs` | 28, 45 | `AddDishToMenuServerRpc` / `RemoveDishFromMenuServerRpc` (`string foodGuid`) |
| `GameManager.Data.cs` | 66, 73, 93 | `AddInventoryDataClientRpc` / `RemoveInventoryDataClientRpc` / `RemovePostBoxDataClientRpc` |
| `GameManager.Economy.cs` | 134, 160 | `UnlockDishServerRpc` / `PurchaseUpgradeServerRpc` |
| `UIManager.cs` | 57 | `ShowOptionMenuRpc(..., string optionGuids, ...)` — **chuỗi GUID ghép bằng separator**, nặng nhất |

Mỗi GUID ≈ 33 byte (32 UTF-8 + 1 byte length prefix) thay vì 4 byte.

### Lưu ý NetworkVariable đã đúng chỗ

`PlacedObjectView.cs:12` và `RefillerKitchenObject.cs:7` đã dùng
`NetworkVariable<FixedString64Bytes>` — đúng pattern cho state được giữ; **không cần đụng**.

## Thay đổi đề xuất

### Lựa chọn A — Băm 32 hex → 2 `ulong` (không cần registry)

32 hex = 128 bit = 2 `ulong`. Gửi `(ulong hi, ulong lo)` hoặc `System.Guid` qua
`FastBufferWriter.WriteValueSafe(byte[16])`.

- Sửa: thêm extension serialize/deserialize cho `Guid`; đổi chữ ký 6 RPC ở bảng trên;
  `UIManager.ShowOptionMenuRpc` đổi `string optionGuids` → `Guid[]`.
- Ảnh hưởng: −15 byte/GUID so với chuỗi, **không đổi định danh**, không cần map.
- Code đọc GUID trong handler giữ nguyên (chỉ bước nhận/gửi đổi).

### Lựa chọn B — GUID → `uint` hash (nhỏ nhất, có rủi ro va chạm)

Hash chuỗi GUID thành 4 byte.

- Ảnh hưởng: −29 byte/GUID; **có thể va chạm** giữa 2 asset. Phải kiểm tra toàn bộ asset không
  trùng hash trước khi nhận (build-time validation).
- Sửa: một registry static sinh lúc build, ném exception nếu phát sinh va chạm.

### Lựa chọn C — Không làm

Như đã ghi ở `optimization_plan` §4: các RPC này **không phải hot path mỗi frame**. Chơi 4 người
thì mỗi giây có vài RPC, mỗi RPC tiết kiệm ~28 byte → vài trăm byte/giây.

**Khuyến nghị: chọn C**, trừ khi profiler cho thấy packet size thật sự là vấn đề.

## Ảnh hưởng

| Hạng mục | A | B | C |
|---|---|---|---|
| Kích thước gói tin | −15 B/GUID | −29 B/GUID | 0 |
| Đụng save? | **Không** (chỉ đổi wire) | Không | Không |
| Đụng place logic? | Có — sửa `PlaceObjectServerRpc` | Có | Không |
| Rủi ro dữ liệu | Thấp | Va chạm hash | Không |
| Host/client khác bản? | Có (wire đổi) | Có | Không |

> Quan trọng: **không đổi `_guid` trong asset hay save file.** Chỉ đổi cách mang qua mạng.

## Kiểm chứng

1. Compile Android player + 49/49 test.
2. Multiplayer Play Mode 2 client:
   - Đặt counter/placed object từ inventory → object hiện đúng loại ở client kia.
   - Mua shop item, mở khoá dish, thêm/xoá dish trong menu.
   - Đổi option menu từ server (`UIManager.ShowOptionMenuRpc`).
3. Nếu chọn B: chạy editor script validate toàn bộ `.asset` không trùng hash.

## Rủi ro / rollback

- Lựa chọn B: va chạm hash → server gán nhầm món cho cầu thủ. Phàm trần bắt buộc.
- Lựa chọn A: sai serialise 16 byte → lỗi hard khi join, phát hiện ngay khi test.
- Rollback: một commit riêng cho từng lựa chọn.
