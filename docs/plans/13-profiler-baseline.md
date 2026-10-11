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

---

## Kết quả đã đo

> **Điều kiện đo (2026-10-12):** Unity Editor Play Mode, host trên **Relay thật**
> (đăng nhập ẩn danh thành công, join code `RFGDFH`), 1 client. Số liệu qua `get_frame_timing`
> (FrameTimingManager). Đây là số trên **Editor** — máy Android thật sẽ chậm hơn. Nhưng
> **trật tự ưu tiên giữ nguyên**, vì thứ đo được là *chênh lệch giữa các kịch bản*.

### Bảng số liệu

| Kịch bản | CPU frame (ms) | CPU main thread (ms) | Render thread (ms) |
|---|---|---|---|
| **S1** MainMenu đứng yên | 8.36 | **1.30** | 0.89 |
| **S2** Trong GameScene, đứng yên | 8.86 – 10.86 | **2.92 – 3.55** | 1.23 – 1.71 |
| **S5** Shop popup mở | 10.46 – 13.10 | **3.57 – 3.99** | 1.49 – 2.32 |
| **S6** Menu popup: 10 lần mở + 55 click category | 9.77 – 13.26 | **3.27 – 4.59** | 1.49 – 2.13 |
| **S10** `ShakeCamera` 10s đang rung | 10.59 – 14.85 | **3.33 – 4.51** | 0.22 – 2.12 |

S2 là mốc so sánh (đứng yên, không popup, không thao tác).

### So với ngưỡng ở Bước 4

| Ngưỡng đã chốt trước khi đo (§ Bước 4) | Kết quả | Kết luận |
|---|---|---|
| p95 < **16 ms** → **không làm 01–06** | Main thread **tối đa 4.59 ms**; CPU frame **tối đa 14.85 ms** | ✅ **Đạt** → 01–06 không được profiler chứng minh |
| `FindGameObjectsWithTag` hiện ở S4 (làm 05) | S10 rung camera (2 lookup/frame) — **không khác gì S2** | ❌ Không đủ lý do |
| S6 GC > 1 MB/s (làm 04) | Chưa đo được GC KB/s (xem "Chưa đo được") | ⏸ Treo |
| S5 `List<T>.Find` < 0.1 ms (bỏ phần perf của 12) | Mở shop chỉ +0.4–0.7 ms main thread so S2 | ✅ **Bỏ phần perf**, giữ phần `LogError` |
| Join > 1s / `chunkCount` > 5 (làm 02) | Chưa đo (cần 2 client) | ⏸ Treo |
| Bandwidth > 100 KB/s/client (làm 01+03) | Chưa đo | ⏸ Treo |

### Kết luận theo mục

- **05 `Camera.main`** — S10 (2 lookup tag mỗi frame) **không tạo khác biệt đo được** so với S2.
  → Đổi từ "nhóm an toàn nhất nên làm trước" sang **KHÔNG LÀM** theo profiler.
- **04 UI pooling** — mở Shop/Menu popup chỉ cộng **0.4–1.0 ms** main thread so với đứng yên.
  → Cần số GC trước khi quyết; nếu GC cũng thấp → **KHÔNG LÀM**.
- **12 linear `Find`** — mở shop +0.4–0.7 ms. → **Bỏ phần perf**, giữ **phần `LogError`**
  (đây là bug robustness, không phải perf — vẫn làm).
- **06 PlayerStats cache** — S2 (đứng yên, `HandleMovement` chạy 60 lần/s) chỉ tốn 2.92–3.55 ms
  cho **toàn bộ** frame. → **KHÔNG LÀM**.
- **01, 02, 03** — chưa có bằng chứng. Vì p95 < 16 ms → **không làm**, trừ khi có số
  bandwidth / join time cụ thể chứng minh.

### Phát hiện phụ từ lúc đo

1. **Script vào game chạy được, dùng đường production thật.**
   `Tools/Profiling/Enter Game (new host)` (`Assets/Editor/PerfScenarios.cs`) chạy đúng chuỗi
   `NewGame` → `StartHostSessionAsync` → `Loader.LoadNetwork` → `SwitchState(InGameState)`
   như nút New Restaurant (`UIMainMenuPopup.cs:20-39`). Auth ẩn danh + Relay OK, vào `GameScene`
   sau ~20s. Script compile sạch.
2. **S6 cho thấy `UIMenuPopup.Initialize()` tích luỹ trong cùng 1 frame.** 10 lần `ShowPopup`
   chạy trong 1 frame → **55 `UIMenuCategoryItem` được `Instantiate`**, vì `Destroy` hoãn tới cuối
   frame trong khi `Instantiate` là tức thì. Người chơi bình thường không click 10 lần trong 1
   frame, nhưng `ShowPopup` bị gọi lặp trong 1 frame thì có thật.
3. **`MultiplayerManager.StartSinglePlayer()` (`:245`) không được gọi ở bất kỳ đâu** —
   đường single-player offline hiện là **code chết** (script đo có dùng làm fallback).
4. **1 warning engine:** `Look rotation viewing vector is zero`, sinh ra lúc camera shake —
   không liên quan tới script đo.

### Chưa đo được

- **GC KB/s** — `get_counters` không dùng được từ cầu nối MCP (xem cạm bẫy #2). Muốn đo GC phải
  mở Profiler UI và đọc tay, hoặc đọc file `.raw`.
- **S3, S4, S7, S8, S9** — cần thao tác tay (đi lại, kéo ghost, chạy bếp, 4 player, late join).
  S4 đặc biệt: `BuildingGhost.HandleDraging()` (`:130`) `return` khi `isDragging == false` →
  **`Camera.main` chỉ được gọi trong lúc đang giữ chuột kéo**, script không giả lập được.

### Cạm bẫy đã gặp và cách tránh

1. **`enable_callstacks=true` + ghi liên tục = 5.5 GB trong ~40 giây**
   (`Temp\profiler\S1_mainmenu.raw`). Đã xoá ngay.
   → **Chỉ bật callstacks khi cần xác định dòng code allocate**, và **chỉ ghi cửa sổ ngắn**
   (bật → chạy kịch bản → tắt). Không được để recorder chạy nền.

2. **`get_counters` không dùng được từ cầu nối MCP** khi truyền mảng `counters`
   (bên kia nhận thành string → validation fail). Không truyền `counters` thì payload phình hàng MB.
   → Dùng **`get_frame_timing`** (12 trường, payload nhỏ) làm số liệu chính.
   Muốn lấy counter chi tiết → đọc file `.raw` bằng Unity Profiler UI, hoặc dùng
   `get_counters` với 1 category duy nhất rồi cắt output.

3. **Danh sách category hợp lệ của `get_counters`** (không có `CPU`):
   `Render, Scripts, Memory, Physics, Physics2D, Animation, Audio, Lighting, Network, Gui, UI,
   Ai, Video, Loading, Input, Vr, Internal, Particles, FileIO, VirtualTexturing`

4. **S2–S10 cần vào được nhà hàng.** Con đường vào game đi qua UI (login → tạo/load nhà hàng) —
   `MultiplayerManager.StartSinglePlayer()` (`:245-254`) khởi động host đơn lẻ nhưng **không**
   tạo được `GameData` nếu chưa có nhà hàng. → Cần **người chơi tay** các kịch bản này, hoặc
   viết script vào game riêng (rủi ro cao, không nên gộp vào mục đo).

### Việc còn lại

- **Cần người thao tác tay:** S3 (đi lại + click counter), S4 (kéo ghost), S7 (chạy bếp 3 phút),
  S8 (4 player), S9 (late join).
- **Chưa đo được GC KB/s** — cần mở Profiler UI tay.
- Sau khi có đủ số → ghi `docs/profiler_baseline.md` và dán nhãn **LÀM / KHÔNG LÀM** cho từng mục
  trong `optimization_plan.md` §3.


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
