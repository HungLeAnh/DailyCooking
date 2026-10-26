# 02 — Thay JSON snapshot khi join bằng sync có cấu trúc

| | |
|---|---|
| **Nhóm** | A. Mạng / tối ưu (đã hoãn) |
| **Nguồn** | `docs/optimization_plan.md` §3.2, `docs/dataflow.md` §2.4 |
| **Trạng thái** | **KHÔNG LÀM** — đo 2026-10-26; JSON/decompress không phải bottleneck |
| **Rủi ro** | **Cao nhất trong 13 mục** — đụng thẳng đường join/save |
| **Ưu tiên** | Làm sau cùng, hoặc không làm nếu profiler chưa chỉ ra bottleneck |

---

## Kết luận 2026-10-26 — KHÔNG LÀM

Probe tách giai đoạn trên Editor/MPPM (không phải Profiler/Player build):

- `GameScene` active → `GameData != null`: **0.541 s** (snapshot thường, 483 B / 1 chunk) và
  **0.578 s** (601 menu, 12,685 B / **4 chunk**).
- JSON deserialize + giải nén + `ReplaceGameDataFromHost`: **53–62 ms**.
- `OnDataSyncToNewClient`: **0.995–1.066 s** — đây mới là phần >1 s; subscriber hiện tại là
  `UIPlayerResources.Initialize()` cập nhật TMP, không phải JSON/decompress.
- Multi-chunk 4 chunk ghép đúng; JSON hash host == client `39A40EB1C19887EE4DC4D273`; `menu=601`;
  sentinel có trên cả hai. `chunkCount=4` vẫn dưới ngưỡng >5.

Vì vậy **không sửa wire format, không hạ `SNAPSHOT_CHUNK_SIZE`**. Nếu muốn giảm thời gian join UX,
điều tra `OnDataSyncToNewClient`/TMP. Chi tiết:
[13-profiler-baseline.md](13-profiler-baseline.md) và
[17-build-and-2client-status.md](17-build-and-2client-status.md) §6.5.

---

## Mục tiêu

Bớt CPU (JSON + gzip) và bớt message khi client vào phòng, thay cơ chế "gửi cả `GameData` làm
chuỗi JSON nén rồi cắt 4 KB" bằng sync từng phần.

## Hiện trạng

`MultiplayerManager.cs`:

- `:24` `SNAPSHOT_CHUNK_SIZE = 4000` byte.
- `:103` client vừa kết nối gọi `SyncDataToNewClientServerRpc()`.
- `:108-128` server: `DataHandler.ConvertGameDataToJson(GameData)` → `Encoding.UTF8.GetBytes` →
  `Compress()` (GZip `CompressionLevel.Optimal`) → cắt từng 4000 byte → lặp gọi
  `ReceiveGameDataChunkClientRpc(snapshotId, i, chunkCount, chunk, RpcTarget.Single(...))`.
  Có `Debug.Log` in ra độ dài ở `:119`.
- `:129-148` client gom chunk theo `snapshotId`, khi đủ thì `LoadGameDataFromHost`.
- `:149-168` giải nén → `FileDataHandler.LoadFromJson(jsonData)` →
  `GameManager.ReplaceGameDataFromHost(...)` → bắn `OnDataSyncToNewClient`.
- `:169-182` `Compress`/`Decompress` bằng `GZipStream`.

Điểm quan trọng: **cơ chế này đã hoạt động đúng** và có xử lý case "một nhà hàng đầy không vừa một
gói tin" (lý do tồn tại của chunking — `UnityTransport Max Payload Size`).

> Ràng buộc phải nhớ (đúng như `optimization_plan` §3 mục 2 ghi): **join snapshot và file save
> dùng chung 1 bộ serialise.** Bằng chứng: `MultiplayerManager.cs:112` gọi
> `GameManager.Instance.DataHandler.ConvertGameDataToJson(...)`, còn `:159` dùng chính
> `snapshotHandler.LoadFromJson(jsonData)` (cùng `FileDataHandler` /
> `SaveJson.CreateSettings()`). Đổi serialise cho đường join mà không đổi đường save → 2 đường
> **không đọc được dữ liệu của nhau**.

Chi phí thật: mỗi lần join mới — 1 lần serialize JSON toàn bộ, 1 lần nén, `chunkCount` message
gửi, 1 lần giải nén, 1 lần deserialize. Chạy **một lần cho mỗi client join**, không phải mỗi frame.

## Thay đổi đề xuất

### Lựa chọn 1 — Giữ JSON, bỏ chunking (rủi ro thấp, lợi ích nhỏ)

Thay `byte[]` chunk bằng `ArraySegment<byte>`/`FastBufferWriter` gửi blob lớn một lần, hoặc nâng
`UnityTransport.MaxPayloadSize`.

- Sửa: `MultiplayerManager.cs:116-127`, `:129-148`.
- Ảnh hưởng: bớt `chunkCount - 1` message; vẫn còn JSON + gzip.

### Lựa chọn 2 — Gửi binary thay JSON (rủi ro trung bình)

Dùng `FastBufferWriter` ghi thẳng `GameData` (mỗi field kiểu nguyên/`FixedString` ghi trực tiếp,
list ghi length + phần tử), hoặc `MessagePack`/`MemoryPack`.

- Sửa: `MultiplayerManager.cs:112` (bỏ `ConvertGameDataToJson`), `:157-160` (bỏ
  `LoadFromJson`), thêm `GameDataSerializer`.
- Ảnh hưởng: bỏ được serialize/deserialize JSON; **bảo mật type binder (`SaveJson.CreateSettings`)
  phải viết lại cho binary** — đây là lớp chống đọc file lạ (`datamanagement` §7).

### Lựa chọn 3 — Sync bằng `NetworkVariable`/`NetworkList` từng phần (rủi ro cao nhất)

Không gửi snapshot; host giữ state trong `NetworkVariable`, client tự đồng bộ. Snapshot chỉ còn
dùng khi có thay đổi giữa lúc connect và lúc ObjectManager spawn xong.

- Sửa: toàn bộ `GameManager.Data.cs` + `GameManager.Economy.cs` (bỏ `*ClientRpc`),
  `GameData` chuyển sang wrapper networked.
- Ảnh hưởng: **đây là mục 01 bước 2.** Join path, save, UI binding đổi hết.

### Đề xuất

**Không chọn.** Đã đo mục 13 ngày 2026-10-26: JSON/decompress không phải điểm nghẽn; giữ nguyên.

## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| CPU mỗi lần join | Lựa chọn 1: giảm ít. Lựa chọn 2: giảm rõ. Lựa chọn 3: giảm nhiều. |
| Số message | Lựa chọn 1: giảm. 2: ~1. 3: ~0 snapshot. |
| Bảo mật type binder | Lựa chọn 1: giữ nguyên. 2, 3: phải viết lại. |
| Break save? | Không — snapshot là bản sao runtime, không phải file lưu. |
| Break host/client khác bản? | Có, nếu đổi wire format. Cả host lẫn client phải cập nhật cùng lúc. |

## Kiểm chứng

1. Compile Android player + EditMode 49/49.
2. Multiplayer Play Mode: host + client + **late joiner** (vào sau khi game đã chạy nhiều phút).
3. So sánh: `GameData` trên host == `GameData` trên client sau join (coins, gems, exp, inventory,
   menu, grid, upgrade, shop, cosmetic, tutorial).
4. Thử join với nhà hàng đầy để chắc không vỡ giới hạn gói tin.
5. Log `chunkCount`/thời gian join trước–sau để có số liệu thật.

## Rủi ro / rollback

- Rủi ro cao nhất: mất dữ liệu im lặng khi join (client có state thiếu phần, UI không báo lỗi).
- Bắt buộc làm nhánh riêng, commit theo từng lựa chọn, không sửa trực tiếp `main`.
