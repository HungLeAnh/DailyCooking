# 17 — Kiểm lại các mục "chưa chạy": build APK và 2 client

Ngày: 2026-10-20; §3 cập nhật 2026-10-22. Không có thay đổi runtime nào trong file này — chỉ ghi
**kết quả kiểm** và **những gì chưa kiểm được**, kèm lý do cụ thể.

## 1. Chạy lại bộ kiểm tiêu chuẩn (sau khi dọn file tạm)

| Bước | Kết quả |
|---|---|
| `refresh_unity(force)` → 32 s | OK |
| `read_console(error, warning)` | **0 error / 0 warning** |
| `run_tests(EditMode)` | **49/49 passed**, 0 failed, 0 skipped (`0.236 s`) |
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
- **Chưa đo:** S8 4-player, băng thông bytes/s + packet count, và thời gian snapshot tách riêng
  khỏi sign-in.
- **MPPM console:** clone `Player 2` **0 error / 0 warning**. Lỗi read-only/shader trước đó do
  cache `Library` lệch, đã xử lý bằng reset `Library`; NRE `GameInput.IsMouseOverUI`
  (`Touchscreen.current == null`) đã fix null guard. Host có 2 NRE nội bộ MPPM
  (`MainPlayerSystems.Listen`) khi clone được launch qua API nội bộ; không phải game code,
  console đã được clear.
- Số cũ **5.45 s / 909 chars / 484 bytes** đo trên clone `mppm159b293e` trước reset, chỉ tham
  chiếu. Số **7.85 s / 449–463 bytes** thuộc clone tạo tay `S8Test`, **không dùng**.
