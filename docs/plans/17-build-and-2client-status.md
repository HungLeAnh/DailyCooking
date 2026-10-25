# 17 — Kiểm lại các mục "chưa chạy": build APK và 2/4 client

Ngày: 2026-10-20; §3 cập nhật 2026-10-22; §5 cập nhật 2026-10-25; §6 cập nhật 2026-10-26.
Không có thay đổi runtime nào trong file này — chỉ ghi **kết quả kiểm** và **những gì chưa
kiểm được**, kèm lý do cụ thể.

## 1. Chạy lại bộ kiểm tiêu chuẩn (sau khi dọn file tạm)

| Bước | Kết quả |
|---|---|
| `refresh_unity(force)` → 32 s | OK |
| `read_console(error, warning)` | **0 error / 0 warning**; 2026-10-26 sau refresh cũng **0/0** |
| `run_tests(EditMode)` | **49/49 passed**, 0 failed, 0 skipped (`0.236 s`); chạy lại **2026-10-26** sau fix `Player.prefab`: **49/49**, 0 failed/skipped (`1.109 s`) |
| `Tools/Build Checks/Compile Android Player Scripts` | **`[BuildChecks] Android player scripts compiled: 110 assemblies.`** |

## 2. Build Android thật (APK) — **CHƯA KIỂM ĐƯỢC** trong môi trường này

Đã thử bằng `BuildPipeline.BuildPlayer` (script editor tạm, đã xoá) với đúng cấu hình của project:
Android, IL2CPP, `targetSdk 35`, scene trong Build Settings.

### Lần 1 — cấu hình như repo đang có (custom keystore)

```
Failed  errors=1  size=0.0 MB  time=00:00:25
UnityException: Unable to sign the Android application
```

Unity hiện modal **"Unable to sign the Android application"** rồi dừng ngay ở bước ký.

Nguyên nhân: `ProjectSettings.asset` có `androidUseCustomKeystore: 1`,
`AndroidKeystoreName: D:/Unity/KeyStore/MyRestaurant.keystore`, alias `dev`, **nhưng không có
mật khẩu keystore / mật khẩu alias** trong repo (đúng như mong đợi — secret không nằm trong git).
Không thể ký nếu không có secret của bạn. **Không phải lỗi code.**

### Lần 2 — tạm dùng debug keystore để bỏ qua bước ký

Đặt `PlayerSettings.Android.useCustomKeystore = false` (đã **khôi phục lại `true`** sau khi thử):

```
Failed  errors=2  size=0.0 MB  time=00:06:07
UnityException: Required API level(s): 35.
```

Bước ký **đã qua**; build chạy 6 phút rồi dừng vì Unity hiện modal
**"Android SDK is missing required platform API"**. Máy này thiếu **Android SDK platform 35**
(đúng `AndroidTargetSdkVersion: 35` trong Player Settings). Unity đề nghị `Update Android SDK`
nhưng tôi **không tự cài** SDK lên máy bạn, và trong môi trường này click OS không tới được modal
của Unity nên cũng không bấm được nút đó.

Kết luận: **chưa có APK nào được tạo** (`size=0.0 MB`, thư mục output tạm rỗng). Muốn kiểm thật
thì cần: (a) cài Android SDK platform 35, (b) cung cấp mật khẩu keystore + alias (hoặc chấp nhận
build debug-signed). Sau đó chạy lại đúng 2 bước trên.

### Dọn dẹp sau khi build

Build của Unity đã ghi đè một số file **không liên quan tới logic**:

- `ProjectSettings.asset` → `androidUseCustomKeystore: 0`
- `ProjectSettings/GraphicsSettings.asset`, `UniversalRenderPipelineGlobalSettings.asset`,
  6 file `Assets/URPDefaultResources/*.asset`, `Roboto-Bold SDF.asset` (Unity serialize lại)
- sinh `Assets/Resources/PerformanceTestRunInfo.json` + `PerformanceTestRunSettings.json`

Tất cả đã được **revert/xoá** (`git checkout --`, xoá file untracked). `git status` sau cùng chỉ
còn đúng: `StoveCounter.png` (thay đổi sẵn có của bạn), thư mục untracked
`GooglePlayGames/.../current-build/`, và các file doc.

## 3. 2 client / late join — **ĐÃ KIỂM MỘT PHẦN** (2 player, 2026-10-23, sau reset `Library`)

Đã rerun **host + 1 MPPM virtual Editor đúng tên `Player 2`** trên clone mới
`Library/VP/mppm4214af7c`, không dùng build standalone:

| Hạng mục | Kết quả |
|---|---|
| Host | scene `GameScene`, `IsServer=True`, `clients=2`, `players=2` |
| `Player 2` | scene `GameScene`, `connected=True`, `players=2` |
| Late join | **3.77 s** (sign-in **0.37 s** + join **3.40 s**) |
| Join snapshot | host log: **760 chars, 449 bytes compressed, 1 chunk** (`SNAPSHOT_CHUNK_SIZE=4000`) |

- **3.77 s** đo từ trước `SignInAnonymouslyAsync` đến khi client `connected=True`, scene
  `GameScene` và `GameData != null`. Sign-in dùng token cache nên chỉ 0.37 s; **không so
  trực tiếp** với 5.45 s cũ (sign-in lạnh).
- Snapshot 760/449 nhỏ hơn 909/484 cũ vì save test `S8PostReset` mới, không phải regression.
- `chunkCount=1` < ngưỡng `> 5`: lần này **không** kiểm được nhánh ghép nhiều chunk.
- **Lượt này chưa đo:** băng thông lúc gameplay nặng và thời gian snapshot tách riêng khỏi sign-in.
  S8 4-player + băng thông nhẹ đã kiểm ở §4; movement/bandwidth/snapshot lặp lại đã kiểm ở §6.
- **MPPM console:** clone `Player 2` **0 error / 0 warning**. Lỗi read-only/shader trước đó do
  cache `Library` lệch, đã xử lý bằng reset `Library`; NRE `GameInput.IsMouseOverUI`
  (`Touchscreen.current == null`) đã fix null guard. Host có 2 NRE nội bộ MPPM
  (`MainPlayerSystems.Listen`) khi clone được launch qua API nội bộ; không phải game code,
  console đã được clear.
- Số cũ **5.45 s / 909 chars / 484 bytes** đo trên clone `mppm159b293e` trước reset, chỉ tham
  chiếu. Số **7.85 s / 449–463 bytes** thuộc clone tạo tay `S8Test`, **không dùng**.

## 4. S8 4 player + bandwidth — **ĐÃ KIỂM MỘT PHẦN** (2026-10-23)

Host Editor chính + 3 MPPM virtual Editor `Player 2`/`Player 3`/`Player 4`
(`mppm4214af7c`, `mppm5d14303d`, `mppmd382a7e5`), save test `S8Four`, Relay thật. Mỗi clone
dùng profile riêng (`s8_player2/3/4`) nên `PlayerId` khác nhau.

| Hạng mục | Kết quả |
|---|---|
| Host | scene `GameScene`, `IsServer=True`, `clients=4`, `players=4`, cả 4 `PlayerObject != null` |
| 3 client | `connected=True`, scene `GameScene`, `players=4` |
| Join snapshot | host log 3 lần: **990 / 1139 / 1288 chars**, **496 / 523 / 549 bytes**, **1 chunk** mỗi client |
| Snapshot phase (`Player 2`, first join) | **1.251 s** từ connected/scene → `GameData != null` |
| Bandwidth host | Tx **1.12 KB/s**, Rx **0.81 KB/s**, Tx **8.50 pps**, Rx **7.16 pps** |
| Bandwidth client cao nhất | Tx **1.07 KB/s**, Rx **0.81 KB/s**, Tx **8.14 pps**, Rx **7.12 pps** |

- **1.251 s** là một mẫu first join, gồm RPC + decompress + JSON; lượt này **chưa lặp lại**
  và là Editor/MPPM nên chưa đủ kết luận. Bỏ số rejoin `1.082 s` / `2.976 s` vì gọi
  `ShutdownAndReset()` trực tiếp, không phải flow leave chuẩn. Đã lặp lại 3 mẫu ngày
  2026-10-26 ở §6.
- Bandwidth đo bằng `NetworkDriver.GetStatistics()` (UTP transport), cửa sổ ~36.6 s, **không
  thao tác người chơi**. Cao nhất ~1.9 KB/s tổng hai chiều mỗi client, thấp hơn nhiều ngưỡng
  100 KB/s/client; lượt này **chưa đo lúc di chuyển/nấu** — đã đo ngày 2026-10-26 ở §6.
- `Player 2`: 1.07/0.81 KB/s, 8.14/7.12 pps; `Player 3`: 0.34/0.41 KB/s, 3.28/3.50 pps;
  `Player 4`: 0.36/0.46 KB/s, 3.39/3.85 pps.
- 3 client **0 error / 0 warning** sau clear. Khi thử rejoin, `ShutdownAndReset()` trên client
  làm lộ bug thật: `playerDataNetworkList.Clear()` báo `Write permissions (Server)`. Đã fix
  `f2240ca4d` (chỉ host clear) và verify flow leave chuẩn: **0 error / 0 warning**.
- Host có NRE nội bộ MPPM lặp lại trong `MainPlayerSystems+<>c__DisplayClass8_0.<Listen>b__1`
  (`UpdateCloneLogCountsMessage`), **không phải game code**.
- **Lượt này chưa đo:** lặp lại snapshot phase, bandwidth lúc gameplay nặng, GC trên Player build.
  Đã đo lại movement/bandwidth/snapshot ngày 2026-10-26 ở §6.

## 5. S8 4 player join/visual + sync vị trí — 2026-10-25

Đã chạy host Editor chính + 3 MPPM virtual Editor `Player 2`/`Player 3`/`Player 4`
(`mppm4214af7c`, `mppm5d14303d`, `mppmd382a7e5`), mỗi clone profile riêng
(`s8_player2/3/4`), save test `S8Verify3`, Relay thật.

| Hạng mục | Kết quả |
|---|---|
| Host | scene `GameScene`, `IsServer=True`, `clients=4`, `players=4` |
| 3 client | `connected=True`, scene `GameScene`, `players=4` |
| UI | `UIMainMenuPopup(Clone)` đã hide; không còn popup che scene game |
| Console | 3 client **0 error / 0 warning**; host chỉ NRE nội bộ MPPM |

Đây là kiểm **join/visual**, không phải kiểm sync vị trí.

### Bug sync vị trí

`Player.prefab` reference script GUID `da52bf8bbc1de48cfb221a6ff30f7972`, class
`Unity.Multiplayer.Center.NetcodeForGameObjectsExample.ClientNetworkTransform`. GUID này
**không còn asset** (`AssetDatabase.GUIDToAssetPath` trả rỗng), nên prefab load ra một
component **MISSING**. Player chỉ di chuyển local, không replicate transform.

Đã xoá component missing và thêm `Unity.Netcode.Components.NetworkTransform` với
`AuthorityMode=Owner`, sync position `X/Y/Z`, rotation `Y`, `Interpolate=true`. Giữ nguyên
`NetworkObject`, `PlayerStateMachine`, `PlayerIKHandler`, `NetworkAnimator`; không đổi GUID,
class name hay field name của script game.

### Kiểm 2 player sau fix

Host + `Player 2`, save `S8Move`, Relay thật:

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
  ngày 2026-10-26 ở §6. Không suy ra từ lượt 4-player visual ở trên.

### Tutorial khi test S8

`GameMechanicPanel` là panel chặn, 8 bước, không có nút close (`closeButton: 0`). Để test
nhanh, gọi `CloseTutorial()` trước: callback `TutorialManager_OnTutorialGameMechanicClosed`
mới thấy `HasPlayedFirstTime == false` để `UnlockGrid()` và set flag. Sau đó gọi
`UIHUDManager.ShowAllUIElement()` + `GameManager.ShowJoyStick()`. **Không set
`HasPlayedFirstTime=true` trước `CloseTutorial()`**, vì khi đó `TutorialManager` sẽ bỏ qua
`UnlockGrid()`. Đây là flow test, không phải flow production (production vẫn 8 lần Next).

## 6. S8 4 player movement + bandwidth + snapshot + EditMode — 2026-10-26

Tiếp nối §5, cùng host + 3 MPPM virtual Editor, save `S8Move`, Relay thật. Probe tạm
`TmpS8Probe` đã xoá cùng `.meta`; không còn file tạm. **Đây là Editor/MPPM, không phải build.**

### 6.1 4-player movement — probe

Cho 4 owner chạy vòng tròn 8 s bằng `transform.position` (owner-authoritative
`NetworkTransform`), dừng 1 s rồi đọc trên cả 4 editor:

| Owner | Vị trí đích | Sai lệch lớn nhất |
|---|---|---|
| 0 host | `(-3.000, 0, -3.000)` | 0.005 m |
| 1 P2 | `(-3.000, 0, -1.000)` | 0.005 m |
| 2 P3 | `(-3.000, 0, 1.000)` | 0.005 m |
| 3 P4 | `(-3.000, 0, 3.000)` | 0.005 m |

Cả 4 editor `players=4`; đây là **probe/automation**, không phải joystick tay. Kiểm tay
vẫn chỉ có 2 player.

### 6.2 Snapshot phase — 3 mẫu, probe

| Client | Phase |
|---|---:|
| P2 | 0.994 s |
| P3 | 1.226 s |
| P4 | 1.285 s |

Đo từ `GameScene` active → `GameData != null`; **không gồm** sign-in/connect. Trung bình
1.168 s, trung vị 1.226 s; 2/3 > 1 s. Mẫu trước 1.251 s (2026-10-23) cùng bậc.

### 6.3 Bandwidth — UTP `NetworkDriver.GetStatistics()`

**Di chuyển**, cửa sổ 12.00 s/editor:

| Editor | Tx KB/s | Rx KB/s | Tổng |
|---|---:|---:|---:|
| Host | 50.87 | 13.98 | 64.85 |
| P2 | 7.93 | 12.54 | 20.47 |
| P3 | 8.36 | 12.57 | 20.93 |
| P4 | 7.39 | 11.23 | 18.62 |

**Nấu**, 8 chu kỳ `Cooking` (`PotatoSlices`, restart 5.5 s < 6 s), cửa sổ 10.00 s/editor:

| Editor | Tx KB/s | Rx KB/s | Tổng |
|---|---:|---:|---:|
| Host | 10.14 | 2.51 | 12.65 |
| P2 | 0.82 | 2.57 | 3.39 |
| P3 | 0.88 | 3.75 | 4.63 |
| P4 | 0.93 | 4.17 | 5.10 |

- Client movement **18.6–20.9 KB/s** tổng hai chiều, cooking **3.4–5.1 KB/s**; đều dưới
  ngưỡng 100 KB/s/client.
- Workload nấu là **spawn/despawn + NetworkVariable**, chưa phải full serve/cắt.

### 6.4 EditMode + console

- Xoá probe → `refresh_unity(force/all)` → console **0 error / 0 warning**.
- `run_tests(EditMode)`: **49/49 passed**, 0 failed, 0 skipped (`1.109 s`) — chạy lại sau fix prefab.
- 3 client **0 error / 0 warning**; host có 1 NRE nội bộ MPPM (`MainPlayerSystems.Listen`),
  không phải game code, clear console rồi không lặp lại.
