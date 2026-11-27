# Task đang hoãn / để sau

Tổng hợp từ các tài liệu trong `docs/`. Cập nhật lần cuối: **2026-11-14**.

Nguồn: `optimization-log.md` (gộp từ `optimization_plan.md` + `plans/*`), `datamanagement.md` §4–5,
`dataflow.md` §8, `phases.md` (và `project_analysis.md`, `phase5_summary.md` — đã xoá, nội dung đã chép vào đây).

Quy ước trạng thái:

| Nhãn | Nghĩa |
|---|---|
| ⏸ Hoãn | Có kế hoạch, chưa làm, chưa có quyết định đóng |
| ❌ Không làm | Đã đo/kết luận là không đáng làm |
| ⏳ Chưa xong | Đã làm một phần, phần còn lại ghi rõ bên dưới |
| 🚫 Chặn | Không làm được vì thiếu môi trường/secret |
| ✅ Đã xong | Giữ lại để đối chiếu, không còn việc |

---

## 1. Mạng & hiệu năng

Nhóm A trong `optimization-log.md` §4 là **đề xuất của agent, chưa được chủ dự án xác nhận** —
có thể kéo lại làm bất cứ lúc nào.

| Mục | Việc còn lại | Trạng thái |
|---|---|---|
| 01 RPC chain | Bước 1 gom RPC; Bước 2 đưa `GameData` lên `NetworkVariable`. Chỉ làm nếu đổi protocol hoặc profiler chứng minh cần | ⏸ Hoãn |
| 02 JSON join sync | Thay JSON+gzip+chunk bằng binary/`NetworkVariable` | ❌ Không làm — JSON+decompress chỉ 53–62 ms; >1 s là `OnDataSyncToNewClient`/TMP |
| 03 GUID → int | Nén GUID trên 6 RPC | ❌ Khuyến nghị không làm — tiết kiệm ~28 B/RPC, không phải hot path |
| 04 UI pooling | Bước 1 `UIMenuPopup` đã xong; **Bước 2** `UICharacterPopup` badge và **Bước 4** `UIUpgradePopup` cache mảng chưa làm; cần số GC trước khi làm tiếp | ⏳ Chưa xong |
| 05 `Camera.main` | Cache các chỗ đọc mỗi frame | ✅ Đã xong (kiểm 2026-11-14) |
| 06 `PlayerStats` cache | Cache reference trong `PlayerStateMachine.HandleMovement` | ❌ Không làm — allocation đã hết, rủi ro stale reference |
| 11 SO ↔ prefab cycle | Bỏ field SO baked trên prefab `KitchenObject` | ⏸ Hoãn tới khi làm Addressables |
| 12 Bước 3 | Hàm resolve shop reward dùng chung (`ShopRewardResolver.TryResolve`) | ⏸ Tùy chọn, chưa làm |
| 09 Bước 3 | Guard lúc build cho `ShopItemCategory` (số ghim ≠ thứ tự asset → `LogError`) | ⏸ Chưa làm (Bước 2 `[InspectorName]` đã có) |
| 15 | `InteractAlternateEvent` của `TablewareKitchenObject` và `PostBox` không tới được vì thiếu `IHasProgress` | ⏸ Chưa sửa — chưa xác định hành vi mong muốn |

Chi tiết từng mục: `docs/optimization-log.md` §2 (mục 01–12, 15).

## 2. Chưa kiểm được / chặn môi trường

| Mục | Việc còn lại | Trạng thái |
|---|---|---|
| 13 Profiler | **GC KB/s trên Player build**; **bandwidth + S9 trên Android/Player build** | 🚫 Chặn — cần Development Build + Profiler UI (hoặc file `.raw`) |
| 17 Build APK | Chưa tạo được APK nào | 🚫 Chặn — thiếu mật khẩu keystore (`MyRestaurant.keystore`, alias `dev`) và Android SDK platform 35 |
| 14 Grid guide | Xem material trên build Android thật | ⏸ Chưa kiểm (mới kiểm Editor) |
| 19 Click audit | Kiểm click với 2 client | ⏸ Chưa kiểm |
| 20 Touch drag | Ngón tay thật trên Android, multi-touch/pinch, `fingerId` trên thiết bị | ⏸ Chưa kiểm |
| 04 Pooling | Đo GC Alloc trước/sau; kiểm client thấy card mới; chạy lại toàn bộ tutorial bằng tay | ⏸ Chưa kiểm |

## 3. Dữ liệu & kiến trúc

| Vị trí | Việc | Trạng thái |
|---|---|---|
| `datamanagement.md` §4.2 | Shop GUID trùng lặp ở 3 lookup (`ConfigShop`, `PlacedObjectDatabase`, `KitchenObjectSODic`) | ⏸ Open — 3 dictionary trên cùng không gian GUID |
| `datamanagement.md` Tier 2 #2 | Spawned kitchen object đọc SO từ field baked (gộp với mục 11) | ⏸ Hoãn tới Addressables |
| `datamanagement.md` Tier 2 #4 | Chuẩn hoá mọi counter về GUID-resolved | ❌ Không nên làm — `optimization-log.md` mục 10 kết luận 2 pattern là đúng |
| `datamanagement.md` Tier 3 #1, #2 | Pool kitchen object + tách `PrefabSpawnService` | ❌ Đã revert — xung đột `NetworkObject` lifetime |
| `datamanagement.md` Tier 3 #3 | Addressables cho content streaming | ⏸ Future consideration |
| `datamanagement.md` §4.8 | Thêm food/counter phải sửa 4 chỗ (SO, prefab, list scene, `DefaultNetworkPrefabs`) | ⏸ Nợ quy trình — chưa có tool hỗ trợ |

## 4. Nợ kỹ thuật & rủi ro (ưu tiên giảm dần)

Nguồn: `dataflow.md` §8 (và `project_analysis.md` §11–12, đã xoá).

| Ưu tiên | Việc |
|---|---|
| **P0** | Server validate movement (client-authoritative hiện tại) |
| **P0** | Xác thực rewarded ad — hiện client tự báo hoàn thành |
| **P0** | Hoàn tất receipt validation + generate `GooglePlayTangle` |
| **P1** | PlayMode test cho multiplayer (join giữa ván, host leave, snapshot integrity) |
| **P1** | Join đang full-state replace → delta / snapshot + log replay |
| **P1** | 200 road piece instantiate lúc init → pool |
| **P1** | Asset budget: SpriteAtlas, texture compression, Addressables; mục tiêu APK < 80 MB |
| **P2** | `UpgradeTarget.CarryingCapacity` không có SO |
| **P2** | Giá legacy + `Stew` lệch curve |
| **P2** | `ConfigShop.GetById` cache sai điều kiện |
| **P2** | `UIJoyStick` rỗng (dead code hoặc tính năng chưa xong) |
| **P3** | `GameDefine.GridArrayDataInit` hard-code JSON cũ |
| **P3** | Chưa có CMP consent dialog (ads đang deny-by-default) |
| **P3** | Daily reset theo clock host |
| **P3** | `.perch/` chưa ignore (`current-build/` đã ignore) |
| Dài hạn | Tách `GameManager` thành service; host migration; thay SHA-256 bằng PBKDF2/Argon2 |

## 5. Visual

| Việc | Trạng thái |
|---|---|
| `FoodCompleteVisual` cho 14 món mới trên `plate.prefab` / `cup.prefab` (`phases.md`) | ⏳ `plate.prefab` + `bowl.prefab` đã có `FoodCompleteVisual`; **`cup.prefab` chưa có** |

Serving/completion không phụ thuộc phần này — `BotCustomerController` so sánh
`foodSO.kitchenObjectSOList` với `GetKitchenObjectSOList()` của tableware.

---

## 6. Doc đã lỗi thời — đã sửa 2026-11-14

| Doc | Chỗ sửa | Bằng chứng |
|---|---|---|
| `optimization-log.md` mục 05 | Từ "17 `Camera.main` còn lại" → ✅ đã cache hết | `Assets/Scripts` chỉ còn 4 chỗ: `UtilsClass.cs:32`, `Window_QuestPointer.cs:32`, `LookAtCamera.cs:17` (cache) và `CameraController.cs:142` (comment) |
| `datamanagement.md` §4.3 | Enum int thô `⏳ Pending` → ✅ Fixed | `ShopItemCategory.cs` ghim số + `None = 9999`; plan 09 đã migrate `ConfigShopSO.asset` |
| `datamanagement.md` §4.2 + Tier 3 #4 | `UIShopItem` linear `Find` `⏳ Open` → ✅ Fixed | `UIShopItem.cs:112,130` dùng `GetValueOrDefault`; plan 12 đã đóng |
