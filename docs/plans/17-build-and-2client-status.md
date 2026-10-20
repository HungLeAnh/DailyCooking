# 17 — Kiểm lại các mục "chưa chạy": build APK và 2 client

Ngày: 2026-10-20. Không có thay đổi runtime nào trong file này — chỉ ghi **kết quả kiểm** và
**những gì chưa kiểm được**, kèm lý do cụ thể.

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

## 3. 2 client / late join — **CHƯA KIỂM**

Không chạy được trong phiên này:

- Cần **process thứ hai**: Multiplayer Play Mode (`com.unity.multiplayer.playmode` 3.0.0) có cài,
  nhưng virtual player phải tạo trong cửa sổ MPPM (không có API công khai để tự động hoá), và
  trong môi trường này **click OS không tới được UI của Unity**.
- Hoặc build standalone rồi chạy 2 instance + join Relay — nhưng build standalone cần đổi build
  target (reimport toàn bộ asset) và vẫn cần thao tác UI để nhập join code.
- Build Android không thể chạy trên máy (không có thiết bị/emulator trong phiên).

Điều **có thể** nói: các commit trong nhánh không đổi wire format / chữ ký RPC (review trước đã
kiểm `git diff`), nhưng **không** vì thế mà coi late join là "đã kiểm". Vẫn cần một lượt chơi
2 client thật.
