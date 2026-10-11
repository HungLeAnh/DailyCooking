# 13 — Chạy Unity Profiler lấy baseline trước khi tối ưu tiếp

| | |
|---|---|
| **Nhóm** | D. Chưa làm vì thiếu dữ liệu |
| **Nguồn** | `docs/optimization_plan.md` §4 ("chưa chạy profiler") |
| **Trạng thái** | Chưa làm |
| **Rủi ro** | **0** — chỉ đo, không sửa code |
| **Ưu tiên** | **Cao nhất** — làm trước nhóm A, vì nó quyết định nhóm A có đáng làm không |

---

## Mục tiêu

Mọi thay đổi nhóm A (mục 01–06) hiện **đều suy luận, không đo**. Mục này lấy baseline thật để
biết: cái nào đáng sửa, cái nào bỏ.

## Hiện trạng

- Chưa từng chạy Profiler trên project này.
- Đã làm (dựa trên reasoning, không đo): boxing, `ToList()` hot path, `Camera.main` ở 2 class,
  `GetPlayerStatsById` closure, hash `Animator` (commit `4089f0584`, `bcb3d4b6f`, `288f03fd6`).
- Kết quả test: 49/49 EditMode, console 0 error / 0 warning — **đúng nhưng không nói gì về perf**.

## Thay đổi đề xuất — không sửa code, chỉ đo

### Bước 1 — Chuẩn bị

1. **Editor Profiler** (nhanh, đủ cho CPU/GC):
   `Window → Analysis → Profiler`, kết nối `Play Mode`.
2. **Device Profiler** (bắt buộc nếu muốn số thật trên Android):
   - Build **Development Build** + **Autoconnect Profiler** (mục tiêu `windows64` cho nhanh,
     hoặc `android` nếu cần số trên máy thật).
   - Menu sẵn có: `Tools → Build Checks → Compile Android Player Scripts` để chắc script compile
     trước khi build.
3. Profiler areas cần bật: `CPU`, `Rendering`, `Memory`, `GC Alloc` (Allocation callstacks bật
   để biết dòng code allocate — **bắt buộc** nếu mục tiêu là GC).

> Tool MCP có sẵn: `profiler_start` / `profiler_stop` / `profiler_set_areas` /
> `get_counters` / `memory_take_snapshot` — dùng được từ phiên này, không cần thao tác tay.

### Bước 2 — Kịch bản đo (mỗi kịch bản 30–60 giây)

| # | Kịch bản | Mục đích kiểm chứng mục nào |
|---|---|---|
| S1 | **MainMenu đứng yên** (không bấm) | Baseline rỗng; phát hiện loop thừa ở UI |
| S2 | **Vào game 1 player, đứng yên 30s** | `PlayerStateMachine.Update`, singleton chain (mục 06) |
| S3 | **Đi lại + click qua lại counter** | `HandleMovement`, `HandleInteractions`, `Camera.main` nhóm click (mục 05) |
| S4 | **Kéo building ghost 30s** | `BuildingGhost.Update` → `Camera.main` 2 lần/frame (mục 05) |
| S5 | **Mở shop, lướt qua mọi tab** | linear `Find` (mục 12) |
| S6 | **Mở Menu popup + bấm mọi category 10 lần** | Instantiate/Destroy (mục 04) |
| S7 | **Chạy bếp 3 phút** (nấu, cắt, phục vụ khách bot) | `KitchenObject` spawn, `CookingTool`, `BotCustomerController` |
| S8 | **4 player (Multiplayer Play Mode)** | JSON join snapshot (mục 02), RPC (mục 01, 03) |
| S9 | **Late join** client vào giữa trận | `SyncDataToNewClientServerRpc` — đo thời gian + `chunkCount` |
| S10 | **Gọi `ShakeCamera` + rung** | `UtilsClass.cs:603` (mục 05) |

### Bước 3 — Chỉ đọc những marker này

| Marker / nhãn | Kết nối tới |
|---|---|
| `GC.Alloc` theo frame, **top 10 stack** | mọi mục nhóm A |
| `Camera.Render`, `Physics.BoxCast` | S3, S4 |
| `FindGameObjectsWithTag` | mục 05 (phải **không còn** sau khi sửa) |
| `Instantiate` / `Destroy` / `UIVertex` | mục 04, S6 |
| `JsonConvert` / `GZipStream` / `Newtonsoft` | mục 02, S9 |
| `String.Concat` / `Dictionary` / `List<T>.Find` | mục 12, S5 |
| `PlayerStateMachine.Update` | mục 06, S2 |
| Băng thông `NetworkTransport` (bytes/s, packet count) | mục 01, 03, S8 |
| Frame time p50 / p95 | tổng thể |

### Bước 4 — Ngưỡng quyết định (chốt trước khi đo, tránh "tự convince")

| Nếu thấy | Thì làm |
|---|---|
| Frame time p95 **> 33 ms** (dưới 30 fps) ở S3/S7 | Ưu tiên mục 05 + 04 trước |
| GC Alloc **> 1 MB/s** ở S6 | Mục 04 (UIMenuPopup) |
| `FindGameObjectsWithTag` xuất hiện trong S4 | Mục 05 |
| Join snapshot (S9) **> 1 giây** hoặc `chunkCount` **> 5** | Mục 02 |
| Bandwidth ở S8 **> 100 KB/s**/client | Mục 01 + 03 |
| Frame time p95 **< 16 ms**, GC **< 100 KB/s** | **Không làm mục 01–06**; ghi vào plan rồi dừng |
| `List<T>.Find` trong S5 chiếm **< 0.1 ms** | Bỏ mục 12 phần perf, chỉ giữ phần log lỗi |

### Bước 5 — Ghi kết quả vào docs

Tạo `docs/profiler_baseline.md`:

```markdown
# Profiler baseline — <ngày>
| Kịch bản | p50 | p95 | GC KB/s | Ghi chú |
|---|---|---|---|---|
| S1 MainMenu | | | | |
...
## Top 10 GC alloc stack
...
## Quyết định
- Mục 01: KHÔNG LÀM (ngưỡng không đạt)
- Mục 04: LÀM (S6 GC 1.8 MB/s)
```

Sau đó cập nhật `docs/optimization_plan.md` §3: mỗi mục thêm cột **"Có profiler chứng minh"**.

## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| Code | **Không đổi một dòng.** |
| Thời gian | 1 buổi (30–60 phút) cho editor, thêm thời gian nếu build Android. |
| Rủi ro | 0 (chỉ đọc số). |
| Lợi ích | Chấm được 6/13 mục ở nhóm A sang "không làm" → tránh rủi ro regresion không cần thiết. |

## Kiểm chứng

- Profiler có file `.raw`/số liệu lưu được, `docs/profiler_baseline.md` điền đủ bảng.
- Mỗi mục trong `optimization_plan.md` §3 mang nhãn **LÀM / KHÔNG LÀM** kèm số liệu.

## Rủi ro / rollback

- Không có gì để rollback.
- Cạm bẫy duy nhất: đo bằng Editor Play Mode rồi kết luận cho Android — **số Editor luôn đẹp
  hơn máy thật**. Nếu quyết định dựa trên số, phải đo thêm trên thiết bị.
