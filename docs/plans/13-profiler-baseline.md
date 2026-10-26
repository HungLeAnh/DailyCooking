# 13 — Chạy Unity Profiler lấy baseline trước khi tối ưu tiếp

| | |
|---|---|
| **Nhóm** | D. Chưa làm vì thiếu dữ liệu |
| **Nguồn** | `docs/optimization_plan.md` §4 ("chưa chạy profiler") |
| **Trạng thái** | 🟡 **Đã đo S3, S7-rút gọn (2026-10-21), S7 đầy đủ 60 s, S8/S9 2-player + late join, S8 4-player + băng thông nhẹ (2026-10-23), 4-player join/visual + fix sync vị trí (2026-10-25), và 4-player movement + bandwidth movement/cooking + 3 mẫu snapshot + EditMode 49/49 (2026-10-26)** trên Editor/MPPM; GC trên Player build vẫn chưa. |
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

### 2026-10-13 — thử tự động hoá S3/S4 bằng computer-control (mouse/keyboard injection)

Mục tiêu: dùng computer-control MCP để **lá tay** thay vì chờ người thao tác. Kết quả: **không
làm được**, và có số đo mới nhưng **không hợp lệ cho S3/S4** (xem cột Ghi chú).

| Kịch bản (thử) | CPU frame (ms) | CPU main thread (ms) | Ghi chú |
|---|---|---|---|
| **S3-thử** — giữ joystick kéo 3 đợt, 24 mẫu | p50 8.87 / p95 9.82 | p50 **2.87** / p95 **3.11** / max 3.56 | ❌ **Không phải S3.** Player không di chuyển → về bản chất là thêm một mẫu **đứng yên** (≈ S2) |
| **S4-thử** — đã vào placement, kéo chuột 21 mẫu | p50 8.57 / p95 9.64 / max 13.52 | p50 **2.90** / p95 **3.43** / max 5.54 | ⚠️ **Không xác nhận được `isDragging == true`** → không được ghi nhận cho `HandleDraging` |

**Kết luận: S3 và S4 vẫn CHƯA đo được.** Hai con số trên chỉ có giá trị như dữ kiện "mọi thứ
đều dưới 16 ms ở trạng thái tĩnh".

#### Tại sao không lái được (đo bằng probe, không phải suy đoán)

Thêm `Tools/Verify/Input diagnostics (hold input while it runs)` — log mỗi 350 ms:
`Keyboard.current.wKey.isPressed`, `Pointer.current.position/press`,
`GameInput.GetMovementVectorNormalized()`, toạ độ `Player(Clone)`, `GetOwnerStats()`.

- ✅ **Chuột vào được game**: `Pointer.current.position` cập nhật đúng. Bấm screen `(1070, 340)`
  → probe đọc `(1019.92, 569.29)`, tôi tính trước là `(1019.9, 569.8)` — **khớ 0.5 px**.
  `press=True` bắt được khi giữ chuột.
- ❌ **Bàn phím không vào bao giờ**: `w=False` dưới cả 3 cách (`key_down`, `key_up`,
  `press_keys`), có/không `activate_window` trước.
- ❌ **Player không nhúc nhích**: `Player(Clone)` đứng yên tại `(-3, 0, -3)` qua mọi thử;
  `GetMovementVectorNormalized()` luôn `(0, 0)`.
- ❌ **Đã loại hai nguyên nhân dễ đổ oan**: `ownerStats=ok moveSpeed=5` (không phải
  `GetOwnerStats()` null) và `Context.IsDisableInput=false` (mọi lời gọi `DisableInput(...)`
  trong dự án đều **bị comment**, `BuildingPlacementManager.cs:139,150`,
  `UIOptionMenuPopup.cs:130,137`).
- ❌ **Bấm vào khách hàng / nút ▶ của tutorial không đổi trạng thái** (text "Click on customer"
  không đổi sau nhiều lần bấm).

#### Wiring joystick on-screen — tìm ra nhưng vẫn không chạy

Toàn bộ nằm ở `MainMenuScene.unity` (persistent), không phải `GameScene`:

| Object | Component |
|---|---|
| `Canvas_Joystick` | Canvas, CanvasScaler, GraphicRaycaster, `UIJoyStick` |
| `JoyStick` (base, 300×300) | RectTransform + CanvasRenderer + **Image** — *không có script* |
| `Image` (tay cầm, 100×100) | Image (`m_RaycastTarget: 1`) + **`OnScreenStick`** |

`OnScreenStick` (package `com.unity.inputsystem`): `m_MovementRange: 100`,
`m_ControlPath: <Gamepad>/leftStick` → đẩy vào action `Player.Moving`, vốn chỉ có
`2DVector` composite **W/A/S/D** + `<Gamepad>/leftStick` (không có binding trỏ chuột).

Toạ độ tâm tay cầm tính được là canvas `(250, 230)`; probe bắt được
`pointerPos=(250.32, 231.10) press=True` — **đúng tâm**, `raycastTarget=1`, script enabled —
nhưng tay cầm **không nhúc nhích** và `Moving` vẫn `(0,0)`. ⇒ Sự kiện không tới được
`EventSystem`/`OnScreenStick`, dù thiết bị `Pointer` thì có.

`UIJoyStick.cs` là **class singleton rỗng** (8 dòng) — nó không xử lý gì.

### 2026-10-21 — S3 (đi lại + click) và S7-rút gọn (60 s, 5 bot)

**Điều kiện đo:** Unity 6000.6.2f1, Editor Play Mode, host Relay thật (1 client), GameScene.
Dùng probe tạm `Assets/Scripts/TmpInputProbe.cs` để inject input + lấy frame/GC
(`ProfilerRecorder` cho `GC Allocated In Frame` / `GC Allocation In Frame Count`). Probe đã xoá
kèm `.meta` sau khi đo; repo không còn file tạm. **Đây là số Editor, không phải Player build.**

#### S3 — đi lại + click

| Thao tác | Kết quả đo |
|---|---|
| Giữ `W` (inject `KeyboardState`) ~2 s | Player `(-3.00, 0.00, -3.00)` → `(-3.00, 0.00, 7.91)` — đi **10.91 m**, đúng hướng |
| Click `(690, 538)` | `PlateTablewareCounter(Clone)` được chọn; player nhận `plate(Clone)`; `GameInput.LastClickPosition=(690,538)` |

Click đi qua đúng chuỗi `MouseState → InputAction Click → GameInput.OnMouseClickPerformed →
raycast → InteractEvent`. Để action `Click` actuate với chuột tổng hợp, phải **rebind runtime**
`Click` sang `<Mouse>/leftButton`; `Pointer.press` một mình không cập nhật action trong môi trường
Editor mất focus. Đây là **workaround automation**, không sửa code game. Các lần click thử sau đó
không bắn event (action bị kẹt phase sau lần inject trước); không dùng `LastClickPosition` để suy
ra click. Chi tiết ở [16-input-automation-and-ghost-drag.md](16-input-automation-and-ghost-drag.md) §5.

#### S7-rút gọn — 60 s đi lại + 5 bot

Kịch bản: player đi theo plan inject (tiến/lùi/ngang, 2 s mỗi bước) + `StartSpawnBot()`; 5 bot
active roaming trong lúc đo. **Đây không phải S7 đầy đủ**: định nghĩa S7 ở Bước 2 yêu cầu
nấu, cắt, phục vụ khách trong 3 phút; cửa sổ này **không chứa** các bước đó. Vòng
cook→burn→serve→collect đã kiểm riêng trước đó (doc 07/08), nhưng **không nằm trong 60 s đo**.
Vì vậy các số dưới đây chỉ gán cho **S7-rút gọn**, không gán cho S7 đầy đủ.

| Thời lượng | Frames | p50 | p95 | p99 | max | GC | Tổng alloc |
|---|---|---|---|---|---|---|---|
| 60.0 s | 7689 | **7.50 ms** | **8.45 ms** | 10.73 ms | **1582.11 ms** | **3722.4 KB/s** | 3,457,753 |

- `p95 = 8.45 ms` < 16 ms → **không có bằng chứng frame-time** để làm mục 01–06.
- `max = 1582 ms` là **một stall đơn lẻ** (p99 vẫn 10.73 ms); không đủ để kết luận frame pacing.
- `GC = 3722.4 KB/s` **vượt xa ngưỡng 100 KB/s**, nhưng đây là số **Editor + probe**:
  `GC Allocated In Frame` trong Editor gồm cả allocation của Editor, và probe tạo
  `KeyboardState`/`List<Key>` mỗi frame. **Không dùng số này để kết luận mục 05/04**; cần
  Development Build + Profiler trên Player mới có số sạch.
- 5 bot active + player di chuyển nên đây là mẫu **có hoạt động**, không phải đứng yên.

### 2026-10-22 — S7 đầy đủ (60 s, 5 vòng nấu + cắt + phục vụ)

**Điều kiện đo:** Unity 6000.6.2f1, Editor Play Mode, host Relay thật (1 client), GameScene,
5 bot active. Pot và thớt được spawn bằng code trong scene (không đặt qua UI), rồi cửa sổ đo
được điều khiển bằng **coroutine trong probe tạm** — không có lệnh MCP nào chen vào giữa 60 s.
Probe `Assets/Scripts/TmpS7Probe.cs` đã xoá kèm `.meta`; repo không còn file tạm.

Kịch bản thực sự nằm trong cửa sổ đo (log `[TmpS7]`):

| Thời điểm | Hoạt động |
|---|---|
| `t=0.0` | 5 bot active; bắt đầu cửa sổ đo |
| `t=0.1 → 4.9` | nấu: `Potato Chopped` → `Stew Cooked` (5.0 s) |
| `t=4.9` | cắt: 5 nhát tomato → `Tomato Slice` (timer về 0) |
| `t=4.9` | phục vụ: lấy đúng stew từ nồi cho vào bát → bot `Eating`, `served=True` |
| `t=10.3` | ăn xong: `eaten=True`, thu tiền; lặp lại vòng 2–5 |
| `t=52.6` | vòng 5 xong; 5 bot tiếp tục chạy tới `t=60.0` |

Cả **5/5 vòng** đều `usedPot=True`, `correct=True`, `eaten=True`, collect thành công
(coins `259 → 318 → 577 → 621 → 665`; exp `40 → 80 → 0 → 40 → 80`, có lần lên level nên exp reset).
Đây là **S7 đầy đủ** theo nghĩa cửa sổ đo có nấu, cắt hoàn tất và phục vụ/ăn/thu tiền; vẫn là
**60 s**, không phải 3 phút như bảng kịch bản gốc, và thao tác do code automation chứ không phải
tay người chơi.

| Thời lượng | Frames | p50 | p95 | p99 | max | GC | Tổng alloc |
|---|---|---|---|---|---|---|---|
| 60.0 s | 6223 | **9.48 ms** | **11.70 ms** | 14.51 ms | **78.35 ms** | **3090.1 KB/s** | 2,832,077 |

- `p95 = 11.70 ms` < 16 ms → **không có bằng chứng frame-time** để làm mục 01–06.
- `max = 78.35 ms` là một hitch đơn lẻ (p99 vẫn 14.51 ms), **không phải stall do gọi tool** vì
  không có lệnh MCP nào trong cửa sổ đo.
- `GC = 3090.1 KB/s` vẫn là **Editor + probe**, không phải Player build. So với S7-rút gọn
  (`3722.4 KB/s`) thấp hơn, nhưng **không dùng để kết luận** vì khác kịch bản và vẫn có allocation
  của Editor.
- S7-rút gọn (chỉ đi lại + 5 bot) có p50/p95 thấp hơn (7.50/8.45 ms), phù hợp với việc ít
  `KitchenObject`/bot state hơn; đây là **hai kịch bản khác nhau**, không phải regression.

### 2026-10-23 — S8/S9 rerun sau reset `Library` (MPPM `Player 2`)

**Điều kiện đo:** host chạy trong Editor chính; client là virtual Editor `Player 2`
(`Library/VP/mppm4214af7c`), tạo lại sau khi xoá `Library`; save test mới `S8PostReset`.
Host ở `GameScene` trước, client join sau; Relay thật; tổng cộng 2 player.

| Hạng mục | Kết quả đo được |
|---|---|
| S8 — host | `clients=2`, `players=2`, scene `GameScene`, `IsServer=True` |
| S8 — `Player 2` | scene `GameScene`, `connected=True`, `players=2` |
| S9 — join tổng | **3.77 s** (sign-in **0.37 s** + join **3.40 s**) |
| S9 — snapshot | host log: **760 chars, 449 bytes compressed, 1 chunk** (`SNAPSHOT_CHUNK_SIZE=4000`) |

- **3.77 s** đo từ trước `SignInAnonymouslyAsync` đến khi client `connected=True`, scene
  `GameScene` và `GameData != null`; sampler chạy trên main thread. Sign-in dùng token cache
  nên chỉ 0.37 s — **không so trực tiếp** với 5.45 s cũ (sign-in lạnh).
- Snapshot 760/449 nhỏ hơn 909/484 cũ vì save test `S8PostReset` mới, không phải regression.
- `chunkCount=1` < ngưỡng `> 5`: lần join này **không tạo nhiều chunk**, nên chưa chứng minh
  được phần ghép nhiều chunk.
- Console `Player 2`: **0 error / 0 warning**. Trước đó lỗi read-only/shader do cache `Library`
  lệch, đã xử lý bằng reset `Library`; NRE `GameInput.IsMouseOverUI`
  (`Touchscreen.current == null`) đã fix null guard. Host có 2 NRE nội bộ MPPM
  (`MainPlayerSystems.Listen`) khi clone được launch qua API nội bộ; không phải game code,
  và console đã được clear.
- **Đã đo thêm ở mục 2-player:** snapshot phase tách riêng ngày 2026-10-26 (P2 snapshot thường:
  `GameScene` → `GameData != null` **0.541 s**; JSON + decompress + replace **53 ms**;
  `OnDataSync` **0.995 s**). S8 4-player + bandwidth ở mục dưới.
- **Số trước reset (2026-10-22, chỉ tham chiếu):** 5.45 s / 909 chars / 484 bytes / 1 chunk
  trên clone cũ `mppm159b293e`; không dùng làm số hiện tại. Số 7.85 s / 449–463 bytes thuộc
  clone tạo tay `S8Test` và cũng **không dùng**.

### 2026-10-23 — S8 4 player + băng thông nhẹ (MPPM)

**Điều kiện đo:** host Editor chính + 3 virtual Editor `Player 2`/`Player 3`/`Player 4`
(`Library/VP/mppm4214af7c`, `mppm5d14303d`, `mppmd382a7e5`), save test mới `S8Four`,
Relay thật. Mỗi clone dùng profile riêng (`s8_player2/3/4`) nên `PlayerId` khác nhau:
`RBtYtVjIDJZapqgE3O62y4MF1IXL`, `U3csivFsWtHGMz3pdKRFo3RC6Ids`, `NclGuHFazkHkSorHrLmxsbh2os5R`.

| Hạng mục | Kết quả đo được |
|---|---|
| S8 — host | `clients=4`, `players=4`, cả 4 `PlayerObject != null`, scene `GameScene` |
| S8 — 3 client | `connected=True`, scene `GameScene`, `players=4` |
| Join snapshot | host log 3 lần: **990 / 1139 / 1288 chars**, **496 / 523 / 549 bytes**, **1 chunk** mỗi client |
| S9 — snapshot phase (`Player 2`, first join) | **1.251 s** từ connected/scene → `GameData != null` |
| Bandwidth host (không thao tác, ~36.6 s) | Tx **1.12 KB/s**, Rx **0.81 KB/s**, Tx **8.50 pps**, Rx **7.16 pps** |
| Bandwidth client cao nhất | Tx **1.07 KB/s**, Rx **0.81 KB/s**, Tx **8.14 pps**, Rx **7.12 pps** |

Chi tiết client (Tx/Rx KB/s, Tx/Rx pps):

- `Player 2`: 1.07 / 0.81, 8.14 / 7.12
- `Player 3`: 0.34 / 0.41, 3.28 / 3.50
- `Player 4`: 0.36 / 0.46, 3.39 / 3.85

- Snapshot tăng dần vì host state thay đổi giữa các lần join (bot/thời gian), không phải nhiều
  chunk; cả 3 vẫn `chunkCount=1`.
- **1.251 s** là mẫu first join cũ của `Player 2`, đo từ lúc `connected=True`/`GameScene` đến khi
  `GameData != null`; probe cũ **gồm cả `OnDataSyncToNewClient`**, nên chỉ dùng tham chiếu, không
  dùng để kết luận JSON/decompress. Bỏ số rejoin `1.082 s` / `2.976 s` vì đó là flow gọi
  `ShutdownAndReset()` trực tiếp, không chuẩn. Đã tách lại ngày 2026-10-26 (mục dưới).
- Bandwidth đo bằng `NetworkDriver.GetStatistics()` (UTP transport) ở lần 4 client trước khi tách
  profile; `PlayerId` không ảnh hưởng transport. Cửa sổ ~36.6 s, **không thao tác người chơi**;
  cao nhất ~1.9 KB/s tổng hai chiều mỗi client, **thấp hơn nhiều** ngưỡng 100 KB/s/client,
  nhưng lượt này **chưa đo lúc di chuyển/nấu**; đã đo ngày 2026-10-26 (mục dưới).
- Console: 3 client **0 error / 0 warning** sau clear. Khi thử rejoin (không phải flow thường),
  `ShutdownAndReset()` trên client làm lộ bug thật: `playerDataNetworkList.Clear()` báo
  `Write permissions (Server)`. Đã fix `f2240ca4d` (chỉ host clear) và verify flow leave chuẩn
  (`ShutdownAndReset()` + `Loader.Load(MainMenuScene)`): **0 error / 0 warning**. Host có NRE
  nội bộ MPPM lặp lại trong `MainPlayerSystems+<>c__DisplayClass8_0.<Listen>b__1`
  (`UpdateCloneLogCountsMessage`), không phải game code.
- **Lượt này chưa đo:** bandwidth lúc gameplay nặng, snapshot phase lặp lại nhiều mẫu, GC trên
  Player build. Đã đo lại ngày 2026-10-26: bandwidth movement/cooking, tách snapshot phase
  (P2/P3) và multi-chunk 4 chunk; GC/Player build vẫn chờ (mục dưới).

### 2026-10-25 — S8 4 player join/visual + fix sync vị trí

**Điều kiện:** host Editor chính + 3 MPPM virtual Editor `Player 2`/`Player 3`/`Player 4`
(`mppm4214af7c`, `mppm5d14303d`, `mppmd382a7e5`), profile riêng `s8_player2/3/4`, save test
`S8Verify3`, Relay thật.

| Hạng mục | Kết quả |
|---|---|
| Host | scene `GameScene`, `IsServer=True`, `clients=4`, `players=4` |
| 3 client | `connected=True`, scene `GameScene`, `players=4` |
| UI | `UIMainMenuPopup(Clone)` đã hide; không còn popup che scene game |
| Console | 3 client **0 error / 0 warning**; host chỉ NRE nội bộ MPPM |

Đây là kiểm **join/visual**, không phải kiểm sync vị trí.

**Bug sync vị trí phát hiện sau lượt 4 player:** `Player.prefab` reference script GUID
`da52bf8bbc1de48cfb221a6ff30f7972`, class
`Unity.Multiplayer.Center.NetcodeForGameObjectsExample.ClientNetworkTransform`. GUID này
**không còn asset** (`AssetDatabase.GUIDToAssetPath` trả rỗng), và prefab có component
`MISSING` lúc load. Vì vậy player chỉ di chuyển local, không replicate transform.

**Fix:** xoá component missing; thêm `Unity.Netcode.Components.NetworkTransform` với
`AuthorityMode=Owner`, sync position `X/Y/Z`, rotation `Y`, `Interpolate=true`; giữ nguyên
`NetworkObject`, `PlayerStateMachine`, `PlayerIKHandler`, `NetworkAnimator`. Không đổi GUID,
class name hay field name của script game.

**Kiểm 2 player sau fix:** host + `Player 2`, save `S8Move`, Relay thật.

| Chiều | Kết quả |
|---|---|
| Host → P2 | host đặt owner 0 ở `(1.500, 0, -3.000)`; P2 thấy owner 0 ở `(1.495, 0, -3.000)` |
| P2 → host | P2 đặt owner 1 ở `(0, 0, 0.500)`; host thấy owner 1 ở `(-0.004, 0, 0.496)` |
| Tay — joystick | người dùng cầm joystick di chuyển host + P2; hai bên thấy player của nhau chạy theo |

- Hai dòng transform là **probe bằng `execute_code`**, không phải số đo profiler; chênh lệch
  nhỏ do interpolation.
- Dòng joystick là **kiểm tay**, không phải log tự động.
- Console P2 sau test: **0 error / 0 warning**; host chỉ NRE nội bộ MPPM.
- **4 player sau fix:** lượt 2026-10-25 này chưa kiểm sync vị trí (bỏ qua P3/P4); đã kiểm
  ngày 2026-10-26 (mục dưới). Không suy ra từ lượt 4-player visual ở trên.

**Tutorial khi test S8:** `GameMechanicPanel` là panel chặn, 8 bước, không có nút close
(`closeButton: 0`). Để test nhanh, gọi `CloseTutorial()` trước: callback
`TutorialManager_OnTutorialGameMechanicClosed` mới thấy `HasPlayedFirstTime == false` để
`UnlockGrid()` và set flag. Sau đó gọi `UIHUDManager.ShowAllUIElement()` +
`GameManager.ShowJoyStick()`. **Không set `HasPlayedFirstTime=true` trước `CloseTutorial()`**,
vì khi đó `TutorialManager` sẽ bỏ qua `UnlockGrid()`. Đây là flow test, không phải flow
production (production vẫn 8 lần Next).

### 2026-10-26 — S8 4 player movement + bandwidth movement/cooking + snapshot lặp lại + EditMode

**Điều kiện:** tiếp nối lượt 2026-10-25; host Editor chính + 3 MPPM virtual Editor
`Player 2`/`Player 3`/`Player 4` (`mppm4214af7c`, `mppm5d14303d`, `mppmd382a7e5`),
profile riêng `s8_player2/3/4`, save `S8Move`, Relay thật, join code `LKNP6R`.
Probe tạm `Assets/Scripts/TmpS8Probe.cs` dùng để đo; đã xoá cùng `.meta` trước khi chạy test.

#### 4-player movement sau fix — **probe, không phải joystick tay**

Cho cả 4 owner chạy vòng tròn bằng `transform.position` trong 8 s (owner-authoritative
`NetworkTransform`), rồi để dừng 1 s và đọc vị trí trên **cả 4 editor**:

| Owner | Vị trí đích | Sai lệch lớn nhất giữa 4 editor |
|---|---|---|
| 0 (host) | `(-3.000, 0, -3.000)` | **0.005 m** |
| 1 (P2) | `(-3.000, 0, -1.000)` | **0.005 m** |
| 2 (P3) | `(-3.000, 0, 1.000)` | **0.005 m** |
| 3 (P4) | `(-3.000, 0, 3.000)` | **0.005 m** |

- Cả 4 editor đều thấy `players=4`; mỗi owner thấy đúng `isOwner` của mình.
- Đây là **probe/automation** (teleport theo path), không phải người chơi cầm joystick.
  Kiểm joystick tay vẫn chỉ có 2 player (mục 2026-10-25).
- Sai lệch ~0.005 m là do interpolation, cùng mức với phép thử 2 player.

#### Snapshot phase — 3 mẫu first join (probe cũ)

Đo từ lúc `GameScene` active đến khi `GameData != null`; **không gồm** sign-in trước `GameScene`.
Probe cũ không tách connect/first chunk/event:

| Client | Snapshot phase |
|---|---|
| `Player 2` | **0.994 s** |
| `Player 3` | **1.226 s** |
| `Player 4` | **1.285 s** |

- Trung bình **1.168 s**, trung vị **1.226 s**; 2/3 mẫu > 1 s.
- Đây là **probe cũ** trên Editor/MPPM, không phải profiler. Probe cũ **không tách**
  `OnDataSyncToNewClient`; vì vậy các số này **không dùng để kết luận JSON/decompress**.
- So với mẫu 1.251 s ngày 2026-10-23: cùng bậc, không có regression rõ ràng.

#### Tách snapshot phase + multi-chunk — 2026-10-26 (probe)

Bật instrumentation tạm trong `MultiplayerManager.cs` để đo từng mốc; **không commit**
instrumentation. Đo trên Editor/MPPM, host + `Player 2` (snapshot thường) và `Player 3`
(snapshot 601 menu, 4 chunk). Đây là **probe**, không phải Profiler/Player build.

| Mốc | P2 — snapshot thường | P3 — snapshot 601 menu |
|---|---:|---:|
| Compressed bytes / chunks | 483 B / **1** | 12,685 B / **4** |
| `GameScene` active → `GameData != null` | **540.988 ms** | **577.582 ms** |
| JSON deserialize (`jsonMs`) | 49.489 ms | 55.385 ms |
| Giải nén (`decompressMs`) | 2.570 ms | 4.517 ms |
| Ghép chunk (`assembleMs`) | 0.018 ms | 0.035 ms |
| `ReplaceGameDataFromHost` | 1.062 ms | 1.871 ms |
| `OnDataSyncToNewClient` | **994.972 ms** | **1,066.094 ms** |
| `GameScene` active → sau event | 1,535.980 ms | 1,643.697 ms |

- P2: client connected ở `scene + 228.489 ms`; first chunk ở `scene + 486.567 ms`. Từ first
  chunk đến `GameData != null` chỉ còn **54.421 ms** — đúng bằng JSON + decompress + ghép +
  replace. Phần lớn 0.541 s là connect/chờ chunk đầu (first chunk ở 486.567 ms), không phải parse.
- P3: server log `jsonChars=22090`, `gzip=12685`, `chunks=4`; client log
  `compressedBytes=12685`, `rawBytes=22090`, `jsonChars=22090`, `chunks=4`. Server
  `jsonMs=0.459`, `compressMs=0.884`, `sendMs=0.520`.
- Ghép 4 chunk hoạt động đúng: JSON hash trên host và client **giống hệt**
  `39A40EB1C19887EE4DC4D273`; `menu=601`; sentinel `S8_MULTI_CHUNK_SENTINEL_9f3a2c` có trên cả hai.
- **Kết luận:** JSON + decompress + replace chỉ **~53–62 ms** cho cả snapshot thường và
  4 chunk. Con số >1 s (nếu đo sau event) là **`OnDataSyncToNewClient`**, hiện do
  `UIPlayerResources.Initialize()` cập nhật TMP; **không phải JSON/decompress**.
- Vì vậy **mục 02 KHÔNG LÀM**. `chunkCount=4` vẫn dưới ngưỡng >5 và đường multi-chunk đã
  chạy đúng; không cần hạ `SNAPSHOT_CHUNK_SIZE`. Nếu muốn giảm thời gian join UX, phải điều tra
  riêng event/TMP, không phải đổi wire format.

#### Bandwidth — UTP `NetworkDriver.GetStatistics()`

Đo tổng byte/packet của driver trong cửa sổ, rồi chia cho thời lượng thật. Đây là số
**Editor/MPPM**, không phải Player build.

**Di chuyển:** cả 4 owner chạy vòng tròn liên tục; cửa sổ **12.00 s** mỗi editor.

| Editor | Tx KB/s | Rx KB/s | Tx pps | Rx pps |
|---|---:|---:|---:|---:|
| Host | **50.87** | **13.98** | 297.9 | 98.6 |
| `Player 2` | **7.93** | **12.54** | 53.9 | 76.2 |
| `Player 3` | **8.36** | **12.57** | 56.2 | 75.3 |
| `Player 4` | **7.39** | **11.23** | 49.0 | 66.6 |

- Host Tx cao vì server gửi cho 3 client; client chỉ gửi lên server.
- Tổng hai chiều mỗi client **18.6–20.9 KB/s**, vẫn dưới ngưỡng **100 KB/s/client**.

**Nấu:** spawn `PotCookingTool` + `PotatoSlices`, lặp 8 chu kỳ `Cooking` (restart mỗi 5.5 s,
cooking time 6 s) để có tải liên tục; cửa sổ **10.00 s** mỗi editor.

| Editor | Tx KB/s | Rx KB/s | Tx pps | Rx pps |
|---|---:|---:|---:|---:|
| Host | **10.14** | **2.51** | 59.6 | 20.0 |
| `Player 2` | **0.82** | **2.57** | 6.6 | 15.4 |
| `Player 3` | **0.88** | **3.75** | 7.1 | 22.4 |
| `Player 4` | **0.93** | **4.17** | 7.5 | 24.5 |

- Tổng hai chiều mỗi client **3.4–5.1 KB/s**; host **12.65 KB/s**.
- Workload là **spawn/despawn + NetworkVariable cooking**, chưa phải full vòng serve/cắt;
  restart 5.5 s < 6 s nên mỗi chu kỳ chưa đi tới `Cooked`.
- Cả hai workload đều **thấp hơn nhiều** ngưỡng 100 KB/s/client.

#### EditMode sau fix prefab

- Xoá probe + revert instrumentation, `refresh_unity(force/all)` → compile sạch, console
  **0 error / 0 warning** trên cả 4 editor.
- `run_tests(EditMode)`: **49/49 passed**, 0 failed, 0 skipped, **1.16 s** (lần cuối sau
  cleanup; lần trước 1.109 s).
- Đây là lần chạy lại sau khi thay `ClientNetworkTransform` missing bằng `NetworkTransform`
  owner authority; không có test nào bị ảnh hưởng.

#### Console

- 3 client: **0 error / 0 warning**.
- Host: 1 NRE nội bộ MPPM trong lúc join (`MainPlayerSystems.Listen`), không phải game code;
  clear console rồi không lặp lại. Không có error từ probe/cooking.

### So với ngưỡng ở Bước 4

| Ngưỡng đã chốt trước khi đo (§ Bước 4) | Kết quả | Kết luận |
|---|---|---|
| p95 < **16 ms** → **không làm 01–06** | Main thread **tối đa 4.59 ms**; CPU frame **tối đa 14.85 ms** | ✅ **Đạt** → 01–06 không được profiler chứng minh |
| S7-rút gọn p95 < 16 ms (2026-10-21) | p95 **8.45 ms** (Editor + probe) | ✅ Đạt về frame time; GC chưa kết luận |
| S7 đầy đủ p95 < 16 ms (2026-10-22) | p95 **11.70 ms** (Editor + probe, 5 vòng nấu/cắt/phục vụ) | ✅ Đạt về frame time; GC vẫn chưa kết luận |
| `FindGameObjectsWithTag` hiện ở S4 (làm 05) | S10 rung camera (2 lookup/frame) — **không khác gì S2** | ❌ Không đủ lý do |
| S6 GC > 1 MB/s (làm 04) | Chưa đo được GC KB/s (xem "Chưa đo được") | ⏸ Treo |
| S5 `List<T>.Find` < 0.1 ms (bỏ phần perf của 12) | Mở shop chỉ +0.4–0.7 ms main thread so S2 | ✅ **Bỏ phần perf**, giữ phần `LogError` |
| Join > 1s / `chunkCount` > 5 (làm 02) | Probe cũ: **0.994 / 1.226 / 1.285 s** (3 mẫu, chưa tách event). Probe tách 2026-10-26: `GameScene` → `GameData != null` **0.541 / 0.578 s**; JSON + decompress + replace **53–62 ms**; `OnDataSyncToNewClient` **0.995 / 1.066 s**; multi-chunk **4 chunk**, hash host == client | ✅ Ngưỡng >1 s đến từ event/TMP, **không phải JSON/decompress** → **KHÔNG LÀM 02**; `chunkCount=4` < 5 |
| Bandwidth > 100 KB/s/client (làm 01+03) | 4 player: di chuyển **18.6–20.9 KB/s/client** tổng hai chiều; nấu **3.4–5.1 KB/s/client**; host aggregate **64.9 / 12.65 KB/s** (UTP, 12 s / 10 s) | ✅ **Không vượt ngưỡng** trên Editor/MPPM; chưa có Player build |

### Kết luận theo mục

- **05 `Camera.main`** — **ƯU TIÊN THẤP, CHƯA ĐỦ BẰNG CHỨNG.** S10 (2 lookup tag mỗi frame ở
  `UtilsClass.cs:603`) không tạo khác biệt đo được so với S2, **nhưng đó không phải bằng chứng để
  bỏ**: (a) tôi đo **S10** chứ không phải **S4** — hai đường code khác nhau (`HandleDraging` còn có
  raycast + 2 `Lerp` transform); (b) overhead của Editor che mất một chi phí 10–50 µs; (c) **GC vẫn
  chưa đo được** (`FindGameObjectsWithTag` trả `GameObject[]` mới mỗi lần gọi). Ưu tiên thấp vì
  `BuildingGhost.HandleDraging():130` return sớm khi `isDragging == false` → chi phí chỉ phát sinh
  **trong lúc người chơi giữ chuột kéo**, nhưng vẫn nên làm vì ~15 phút, rủi ro gần 0.
- **04 UI pooling** — mở Shop/Menu popup chỉ cộng **0.4–1.0 ms** main thread so với đứng yên.
  → Cần số GC trước khi quyết; nếu GC cũng thấp → **KHÔNG LÀM**.
- **12 linear `Find`** — mở shop +0.4–0.7 ms. → **Bỏ phần perf**, giữ **phần `LogError`**
  (đây là bug robustness, không phải perf — vẫn làm).
- **06 PlayerStats cache** — S2 (đứng yên, `HandleMovement` chạy 60 lần/s) chỉ tốn 2.92–3.55 ms
  cho **toàn bộ** frame. → **KHÔNG LÀM**.
- **02 JSON join sync — KHÔNG LÀM (2026-10-26).** Probe tách giai đoạn cho thấy `GameData` sẵn
  sàng sau **0.541 s** (snapshot thường) và **0.578 s** (601 menu / 4 chunk); JSON + decompress +
  replace chỉ **53–62 ms**. Con số >1 s là `OnDataSyncToNewClient` (**0.995–1.066 s**, hiện do
  `UIPlayerResources.Initialize()` cập nhật TMP), không phải JSON/decompress. Multi-chunk 4 chunk
  ghép đúng, hash host == client. Không đổi wire format, không hạ `SNAPSHOT_CHUNK_SIZE`.
- **01, 03** — chưa sửa. Bandwidth 4 player: di chuyển **18.6–20.9 KB/s/client** tổng hai chiều,
  nấu **3.4–5.1 KB/s/client** — **không vượt ngưỡng 100 KB/s**; p95 < 16 ms. Các mục này vẫn
  chờ Player build nếu muốn tối ưu thêm, nhưng chưa có bằng chứng cấp thiết.

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

- **GC KB/s trên Player build** — S7 đã đo được `3722.4 KB/s` bằng `ProfilerRecorder` trong
  Editor (2026-10-21), nhưng số này gồm Editor + probe, **chưa dùng để kết luận**. `get_counters`
  vẫn không dùng được từ cầu nối MCP (xem cạm bẫy #2); muốn số sạch phải mở Profiler UI trên
  Development Build / đọc file `.raw`.
- **Băng thông lúc gameplay nặng trên Player build** — đã đo 4 player trên Editor/MPPM:
  di chuyển **18.6–20.9 KB/s/client** tổng hai chiều, nấu **3.4–5.1 KB/s/client** (2026-10-26);
  chưa đo trên máy Android/Player build.
- **S9 tách riêng thời gian snapshot** — đã tách ngày 2026-10-26: data sẵn sàng
  **0.541 / 0.578 s**; JSON + decompress + replace **53–62 ms**; `OnDataSyncToNewClient`
  **0.995–1.066 s**. Vẫn chưa đo trên Android/Player build.

### Đã đo (2026-10-20/21/22/23)

- **S3 + S7-rút gọn** — ngày 2026-10-21 (mục ở trên).
- **S7 đầy đủ** — ngày 2026-10-22: 60 s, 5/5 vòng nấu/cắt/phục vụ (mục ở trên).
- **S8/S9 2-player + late join** — ngày 2026-10-23 (sau reset `Library`): host + MPPM
  `Player 2`; join 3.77 s (sign-in 0.37 s + join 3.40 s); snapshot 449 bytes compressed /
  1 chunk (mục ở trên). Số 2026-10-22 trước reset chỉ tham chiếu.
- **S8 4-player + băng thông nhẹ** — ngày 2026-10-23: host + `Player 2`/`Player 3`/`Player 4`
  (profile riêng); `clients=4`/`players=4`; snapshot **496–549 bytes / 1 chunk** mỗi client;
  snapshot phase cũ **1.251 s** (1 mẫu, gồm cả event); bandwidth không thao tác cao nhất
  ~1.9 KB/s/client, ~8 pps; lượt đó chưa đo gameplay nặng, đã đo lại ngày 2026-10-26.
- **S8 4-player join/visual + fix sync vị trí** — ngày 2026-10-25: host + 3 MPPM,
  `clients=4`/`players=4`, cả 3 client ở `GameScene`, popup đã hide, client 0/0; phát hiện
  `ClientNetworkTransform` missing, thay bằng `NetworkTransform` owner authority; 2 player
  sync đúng cả hai chiều (transform + joystick tay). **4-player movement kiểm ngày 2026-10-26**
  (xem bullet dưới).
- **S8 4-player movement + bandwidth + snapshot + EditMode** — ngày 2026-10-26: 4 owner chạy
  vòng tròn 8 s, cả 4 editor thấy vị trí khớp trong **0.005 m**; bandwidth di chuyển
  **18.6–20.9 KB/s/client** tổng hai chiều, nấu **3.4–5.1 KB/s/client**; probe cũ 3 mẫu
  **0.994 / 1.226 / 1.285 s** (chưa tách event). Probe tách sau đó: data sẵn sàng
  **0.541 / 0.578 s**, JSON + decompress + replace **53–62 ms**, `OnDataSyncToNewClient`
  **0.995 / 1.066 s**; multi-chunk **4 chunk**, hash host == client. EditMode **49/49**.
  Tất cả là Editor/MPPM/probe, chưa phải Player build.
- **S4** — ngày 2026-10-20
  ([16-input-automation-and-ghost-drag.md](16-input-automation-and-ghost-drag.md)).
- Lịch sử: **đã thử tự động hoá S3/S4 bằng computer-control (2026-10-13) và thất bại** —
  chi tiết ở mục "2026-10-13" phía trên. Tóm tắt: chuột vào được Input System nhưng **bàn
  phím không**, và sự kiện không tới được `EventSystem`, nên không lái được player.
- Cách đã dùng để S3 chạy được (2026-10-21): inject `KeyboardState` trong probe
  `[DefaultExecutionOrder(-1000)]` + `InputSystem.Update()`; click cần rebind `Click` sang
  `<Mouse>/leftButton` (xem doc 16 §5).

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

5. **Unity hoãn recompile trong lúc đang Play.** Với `Script Changes While Playing =
   Recompile After Finished Playing`, `refresh_unity(force)` báo `compile_requested: true`
   nhưng **menu item mới vẫn "không tồn tại"** cho tới khi thoát Play. Gây mất một chu trình
   vì tưởng code hỏng. → **Phải `stop` trước rồi mới `refresh_unity`.**

6. **`find_gameobjects` với `by_name` / `by_path` trả rỗng** cho mọi tên tôi thử
   (`Tutorial`, `Ghost`, `TutorialCanvas`, `Hand`…); chỉ **`by_component`** trả kết quả
   (`BuildingGhost` → `56749`). Nhưng resource
   `mcpforunity://scene/gameobject/{id}/components` lại **throw** với chính object đó
   (`Exception has been thrown by the target of an invocation`) → không đọc được
   `IsDragging` từ ngoài. Tài nguyên cũng **không với tới được `DontDestroyOnLoad`**
   (tutorial nằm ở đó — thuộc `MainMenuScene`, không phải `GameScene`).

7. **Đừng tin "bấm vào game là ăn" chỉ vì toạ độ khớp.** Mapping toạ độ đúng tới 0.5 px
   (`pointerPos` xác nhận), nhưng **tutorial không tiến, joystick không nhúc nhích**.
   Phải kiểm bằng một tín hiệu **có quan sát được** (text đổi, transform đổi, log mới),
   không được suy ra từ việc click "có vẻ đã tới nơi".

8. **Chưa xong tutorial thì mọi lượt đặt đều bị từ chối.** `GameManager.NewGame()` tạo nhà hàng
   với `widthMin = heightMin = 0` → `GridXZ.IsCellUnlocked()` false với **toàn bộ** ô →
   `CanPlace(..., requireUnlockedCells: true)` từ chối sạch. Ô chỉ mở ở
   `TutorialManager.cs:112` → `GridBuildingSystem.UnlockGrid()`, tức **phải đóng panel tutorial**
   (panel `GameMachanic`, 8 bước). Đường tắt `NewGame` + `LoadNetwork` **bỏ qua** bước này —
   gọi tay `UnlockGrid()` thì đặt được nhưng kết quả không còn là "chơi thật".

9. **`EditorApplication.update` không tick trong play mode** → mọi "driver" dựa vào nó để bơm
   input giả đều im lặng. Bơm gamepad phải gọi tay `InputSystem.Update()` ngay sau
   `QueueStateEvent`. Kể cả vậy gamepad vẫn chập chờn → di chuyển player bằng
   `transform.position = target` + `Physics.SyncTransforms()`, và **nói rõ** trong doc là đã
   teleport chứ không đi bộ. `Gamepad.current` có thể `null` → `InputSystem.AddDevice<Gamepad>()`.

10. **Race với auto sign-in lúc boot.** Gọi `StartHostSessionAsync()` quá sớm → fail
    `Failed to start host with Relay` rồi `Failed to start single player session!`.
    `SignInAnonymouslyAsync` **nuốt exception** (`SessionManager.cs:82-93`) nên lần gọi thứ hai
    lại thành công — đừng kết luận là lỗi Netcode.

11. **`FindFirstObjectByType<CookingToolCounterController>()` có thể trả controller của
    `BuildingGhostPreview`, không phải counter thật.** Preview nằm đúng chỗ counter vừa đặt và
    mang đủ component của prefab (component bị `enabled = false` nhưng `GetComponent` vẫn thấy),
    layer 7 = `BuildingGhost`. Số đo đọc trên nó là số giả ("counter ở layer 7"). → Luôn lọc
    `NetworkObject.IsSpawned`, và kiểm `hit.collider` trước khi kết luận "click trúng counter".

12. **Khách (Bot) đứng giữa đường tia** → raycast trúng collider khách, không trúng counter.
    Log tên collider trúng rồi mới kết luận click hỏng.

13. **Menu item có `/` trong nhãn bị chuẩn hoá khoảng trắng** khi đi qua cầu nối MCP → đừng dùng
    chuỗi menu làm khoá chính xác để `execute_menu_item`.

14. **InputAction không actuate khi editor mất focus.** `Application.isFocused == false` →
    `InputManager.defaultUpdateType` trả `Editor`, event inject bị xử lý theo nhánh editor và
    action không bao giờ chạy (device state vẫn đổi — dễ kết luận sai là "inject hỏng"). Phải bật
    `backgroundBehavior=IgnoreFocus` + `editorInputBehaviorInPlayMode=AllDeviceInputAlwaysGoesToGameView`
    + `InputManager.runPlayerUpdatesInEditMode=true`, và inject bằng
    `InputState.Change` + `InputSystem.Update()` trong `MonoBehaviour` có
    `[DefaultExecutionOrder(-1000)]`. Công thức đầy đủ + kết quả kéo ghost:
    [16-input-automation-and-ghost-drag.md](16-input-automation-and-ghost-drag.md).

15. **Đọc `Pointer.current.position` từ `execute_code` giữa 2 frame có thể thấy giá trị native cũ**,
    không phải giá trị vừa inject. Chỉ tin số đọc bên trong `Update()` của game (hoặc log trong
    frame). Vì vậy test kéo ghost phải log `ptr/pan/visual` từ probe, không đọc lẻ từng lệnh.

16. **Prefab có component `MISSING` thì đừng tin class name trong YAML.** `Player.prefab` ghi
    `ClientNetworkTransform` nhưng GUID `da52bf8bbc1de48cfb221a6ff30f7972` không còn asset;
    `GetComponents<Component>()` trả một component `null` (MISSING), nên không có transform
    replication. Kiểm bằng `AssetDatabase.GUIDToAssetPath(guid)` + load prefab, không chỉ đọc
    YAML. Fix bằng `NetworkTransform` chuẩn, `AuthorityMode=Owner`; đừng tạo lại script mẫu đã mất.

### Việc còn lại

- **S3 + S7-rút gọn đã đo ngày 2026-10-21** (Editor + probe, 1 client, Relay host): S3 đi lại +
  1 click counter; S7-rút gọn 60 s, 5 bot active, chưa có nấu/cắt/phục vụ trong cửa sổ đo.
- **S7 đầy đủ đã đo ngày 2026-10-22** (Editor + probe, 1 client, Relay host): 60 s, 5/5 vòng
  nấu `Potato Chopped → Stew Cooked`, cắt 5 nhát `Tomato → Tomato Slice`, phục vụ trong bát →
  `Eating → eaten=True → collect`; 5 bot active. Số p50/p95/GC ở mục "S7 đầy đủ" phía trên.
  S4 đã kiểm ngày 2026-10-20 bằng inject pointer qua Input
  System — xem [16-input-automation-and-ghost-drag.md](16-input-automation-and-ghost-drag.md).
  Đường computer-control lái chuột OS vẫn đóng (2026-10-13): bàn phím không vào
  `Keyboard.current`, sự kiện OS không tới `EventSystem`.
- **S8/S9 đã kiểm một phần ngày 2026-10-23 (sau reset `Library`)**: 2 player host + MPPM
  `Player 2`, host `clients=2`/`players=2`; late join **3.77 s** (sign-in cached 0.37 s +
  join 3.40 s), snapshot 449 bytes compressed / 1 chunk. 4 player host + `Player 2`/
  `Player 3`/`Player 4` (profile riêng): `clients=4`/`players=4`, snapshot
  **496–549 bytes / 1 chunk** mỗi client; first-join snapshot phase cũ **1.251 s** (1 mẫu, gồm
  cả event); bandwidth không thao tác cao nhất **~1.9 KB/s/client**, **~8 pps**. Chi tiết +
  caveat MPPM ở mục "S8/S9" phía trên; tách phase ngày 2026-10-26 ở mục dưới.
- **2026-10-25:** 4 player join/visual đã chạy sạch; sync vị trí đã fix và kiểm 2 player
  (transform + joystick tay).
- **2026-10-26:** 4-player movement đã kiểm bằng probe (khớp **0.005 m**); snapshot phase
  đã tách: data sẵn sàng **0.541 / 0.578 s**, JSON + decompress + replace **53–62 ms**,
  `OnDataSyncToNewClient` **0.995 / 1.066 s**; multi-chunk **4 chunk**, hash host == client;
  bandwidth di chuyển/nấu đã đo trên Editor/MPPM; EditMode **49/49**.
- **Còn:** **GC KB/s trên Player build** và bandwidth trên Android/Player build. S7 chỉ có số
  Editor + probe; cần Development Build + Profiler UI (hoặc đọc `.raw`). Nếu muốn giảm thời
  gian join UX, điều tra `OnDataSyncToNewClient`/TMP — **không phải mục 02**.
- Đã dán nhãn **LÀM / KHÔNG LÀM** cho từng mục trong `optimization_plan.md` §3; mục **02 là
  KHÔNG LÀM** vì JSON/decompress không phải phần lớn thời gian.
- **Tài sản để lại:** `Assets/Editor/PerfScenarios.cs` có 4 probe mới —
  `Tools/Profiling/S4 - Start placement (ghost)` (vào placement từ code, đã chạy thành công),
  `Tools/Profiling/S4 - Cancel placement`, `Tools/Verify/Input diagnostics (…)`,
  `Tools/Verify/S4 state (ghost / drag / mouse)` (chưa dùng tới vì bị hoãn compile).


## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| Code | Profiler **không đổi code**; S8 2026-10-25 có sửa `Player.prefab` (thay missing `ClientNetworkTransform` bằng `NetworkTransform` owner authority). |
| Thời gian | 1 buổi (30–60 phút) cho editor, thêm thời gian nếu build Android. |
| Rủi ro | 0 cho phần đo; prefab fix đã kiểm 2 player + **4-player movement** (probe, 2026-10-26). |
| Lợi ích | Chấm được 6/13 mục ở nhóm A sang "không làm" → tránh rủi ro regresion không cần thiết. |

## Kiểm chứng

- Profiler có file `.raw`/số liệu lưu được, `docs/profiler_baseline.md` điền đủ bảng.
- Mỗi mục trong `optimization_plan.md` §3 mang nhãn **LÀM / KHÔNG LÀM** kèm số liệu.

## Rủi ro / rollback

- Không có gì để rollback.
- Cạm bẫy duy nhất: đo bằng Editor Play Mode rồi kết luận cho Android — **số Editor luôn đẹp
  hơn máy thật**. Nếu quyết định dựa trên số, phải đo thêm trên thiết bị.
