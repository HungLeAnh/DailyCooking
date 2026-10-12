# Plan chi tiết — 13 mục còn lại

Mỗi file trong thư mục này là một kế hoạch riêng cho một mục, ghi rõ: **hiện trạng (kèm
`file:line`), thay đổi gì, ảnh hưởng gì, cách kiểm chứng, rủi ro**.

Nguồn gốc: `docs/optimization_plan.md` §3, `docs/datamanagement.md` §4.
Kết quả phân tích lần này đã **sửa lại một số mô tả cũ trong optimization_plan cho khớp code** —
xem ghi chú ở từng file.

---

## Mục phát sinh thêm (ngoài 13 mục trên)

| # | Việc | Vị trí | Vì sao thêm |
|---|---|---|---|
| 14 | **Runtime ghi thẳng vào material asset** | `GridBuildingSystem.cs:26` (`[SerializeField] Material gridGuideMaterial`) → `GridVisualizer.cs:24-28` `SetFloat(...)` | Chạy play mode 1 lần là `GridShaderMaterial.mat` bị commit đè vĩnh viễn (đã thấy `5/5.01` → `0/0.01`). Giá trị đó vô nghĩa vì runtime vẫn set lại, nhưng **working tree bẩn sau mỗi lần chạy** và designer có thể vô tình commit. Sửa: `new Material(sharedMaterial)` rồi dispose, hoặc `MaterialPropertyBlock`. |

---

## A. Mạng / tối ưu — đề xuất hoãn vì rủi ro cao (CHƯA được xác nhận)

> **Nguồn:** nhóm này là **đề xuất của agent**, không phải quyết định của chủ dự án — không có
> record nào trong repo xác nhận. Chi tiết ở `docs/optimization_plan.md` §3.
> Bất kỳ mục nào trong nhóm này cũng có thể được đưa vào làm lại bất cứ lúc nào.

| # | File | Đã điều tra | Rủi ro | Nên làm? |
|---|---|---|---|---|
| 01 | [01-rpc-chain-removal.md](01-rpc-chain-removal.md) | ⚠️ Mô tả gốc **sai** — 3 class được nêu không còn RPC. Chuỗi RPC thật nằm ở `GameManager.Data.cs` và **bắt buộc** (GameData không phải NetworkVariable) | Cao | Chỉ bước 1 (gom RPC), bước 2 giao mục 02 |
| 02 | [02-json-join-sync.md](02-json-join-sync.md) | ✅ Hoạt động đúng, chunk 4000 B, chạy **1 lần mỗi lần join** | **Cao nhất** | Chưa — xem 13 trước |
| 03 | [03-guid-to-int.md](03-guid-to-int.md) | ✅ `.Guid` = Unity asset GUID 32 hex; 6 RPC chở GUID | TB–cao | Không (tiết kiệm ~28 B/RPC) |
| 04 | [04-ui-object-pooling.md](04-ui-object-pooling.md) | ⚠️ Popup root **đã cache sẵn**; `UICharacterItem` **đã pool**. Còn lại `UIMenuPopup` rebuild mỗi lần mở | TB | Bước 1 (`UIMenuPopup`) nếu profiler báo |
| 05 | [05-camera-main-remaining.md](05-camera-main-remaining.md) | ✅ 16 chỗ thật; **chỉ 1 đường per-frame** (`BuildingGhost` kéo ghost) | **Thấp** | **Có** — nhóm an toàn nhất |
| 06 | [06-playerstats-cache.md](06-playerstats-cache.md) | ⚠️ Allocation **đã xong**; `Update()` đã `!IsOwner` → chỉ 1 lần/frame, list ≤4 | Thấp | Không |

## B. Data cần bạn quyết (con số / tên)

| # | File | Cần bạn chốt | Rủi ro |
|---|---|---|---|
| 07 | [07-stewso-rewards.md](07-stewso-rewards.md) | `price` / `exp` cho Stew — đề xuất **35 / 40** | Kỹ thuật thấp |
| 08 | [08-stewburnedso-name.md](08-stewburnedso-name.md) | Tên món cháy — đề xuất **`Stew Burned`** | Rất thấp |
| 09 | [09-configshop-enum-ints.md](09-configshop-enum-ints.md) | Cách ghim số + quy tắc thêm giá trị enum mới | Thấp |

## C. Kiến trúc cần chốt

| # | File | Kết luận điều tra | Đề xuất |
|---|---|---|---|
| 10 | [10-counter-data-pattern.md](10-counter-data-pattern.md) | **2 pattern là đúng** (GUID = nội dung động, baked = nội dung cố định) | Không chuẩn hoá; thêm guard null + ghi quy ước |
| 11 | [11-kitchenobject-prefab-baked-so.md](11-kitchenobject-prefab-baked-so.md) | `PlacedObjectView` **đã sạch**; chỉ còn 1 vòng `KitchenObjectSO`↔prefab, và nó **bắt buộc** | Hoãn tới khi làm Addressables |
| 12 | [12-shop-guid-dictionaries.md](12-shop-guid-dictionaries.md) | 1 chỗ linear `Find` + **fail im lặng làm ô shop biến mất** | Làm phần log lỗi; phần perf không cần |

## D. Chưa làm vì thiếu dữ liệu

| # | File | Ưu tiên |
|---|---|---|
| 13 | [13-profiler-baseline.md](13-profiler-baseline.md) | **Cao nhất** — làm trước nhóm A, nó quyết định 6 mục kia có đáng làm |

---

## Phân loại theo 2 trục

**Trục A — mức độ nghiêm trọng của lỗi nếu cứ để đó.**
**Trục B — độ ảnh hưởng/lan rộng của chính thay đổi khi sửa.**

### A. Xếp theo mức độ nghiêm trọng

| Mức | Mục | Tại sao |
|---|---|---|
| 🔴 **Cao** (người chơi thiệt thật) | **07** Stew `price=0, exp=0` | Trừ 60 coins thật ở `GameManager.Economy.cs:144` rồi trả về 0 ở `:416-417`. 3 chỗ UI còn hiện `$0` |
| 🟠 **Trung bình** | **12** shop reward fail im lặng | Guid sai → ô shop biến mất, Console sạch → khó tìm nguyên nhân |
| 🟠 **Trung bình** (phòng ngừa) | **09** `ShopItemCategory` chưa ghim số | Chưa dính, nhưng project **đã từng** dính đúng loại này với `ShopItemType` (May 2026, ghi trong comment enum) |
| 🟡 **Thấp–Trung bình** (perf, chưa đo) | **05** 16 chỗ `Camera.main` | 2 lookup tag mỗi frame khi kéo ghost (`BuildingGhost.cs:132,160` → `BuildingPlacementManager.cs:107`) |
| 🟡 **Thấp–Trung bình** (perf, chưa đo) | **04** `UIMenuPopup` rebuild mỗi lần mở | Destroy + Instantiate cả grid, mỗi lần bấm category |
| 🟡 **Thấp** | **10** guard null `TablewareCounter` | Silent fail khi designer quên gán SO |
| 🟡 **Thấp** | **01** RPC chain | Bandwidth ~vài chục B/giây, **chưa phải bottleneck** |
| 🟢 **Rất thấp** | **08** `StewBurnedSO.objectName` | Chỉ 2 chỗ gán vào Text |
| 🟢 **Rất thấp** | **03** GUID → int | ~28 byte/RPC, không phải hot path |
| 🟢 **Rất thấp** | **02** JSON join | 1 lần mỗi lần client vào, **không per-frame** |
| ⚪ **Không còn lỗi** | **06** PlayerStats cache | Allocation đã xong; `!IsOwner` chặn từ đầu |
| ⚪ **Không phải lỗi hôm nay** | **11** SO↔prefab cycle | Chạy đúng; chỉ chặn nếu làm Addressables tách group |
| ⚪ **Không phải lỗi** | **13** Profiler | Công cụ đo, rủi ro = 0 |

### B. Xếp theo độ ảnh hưởng khi sửa

| Ảnh hưởng khi sửa | Mục | Đụng gì |
|---|---|---|
| 🔴 **Cao nhất — đổi wire/save protocol** | **02** JSON join | Join path + bộ serialise dùng chung với file save (`MultiplayerManager.cs:112,159` → `FileDataHandler`). Host & client phải cùng bản |
| 🔴 **Cao** | **01 bước 2** (đưa `GameData` lên `NetworkVariable`) | Đổi `GameManager.Data.cs` (13 cặp RPC) + `Economy.cs` (11) + join + save |
| 🔴 **Cao** | **03 lựa chọn B** (GUID → uint hash) | Va chạm hash = gán nhầm món; bắt buộc có build-time validation |
| 🟠 **Trung bình** | **04** `UIMenuPopup` | Listener trùng (`onClick` chồng), item hiện sai/thừa — **rủi ro regresion UI cao nhất nhóm A** |
| 🟠 **Trung bình** | **06** cache `PlayerStats` | Stale reference nếu không invalidate đúng lúc `OnPlayerStatsAdded` / remove |
| 🟠 **Trung bình** | **11** phá cycle | Phải audit mọi prefab có `KitchenObject`, đổi contract spawn; sai → `GetKitchenObjectSO()` null lúc runtime |
| 🟡 **Thấp** | **01 bước 1** (gom RPC) | Đổi nội dung RPC, giữ nguyên mô hình |
| 🟡 **Thấp** | **05** cache `Camera.main` | Không đổi chữ ký public, không đụng save/mạng/prefab |
| 🟡 **Thấp** | **12** đổi linear → dict | Kết quả y hệt, chỉ nhanh hơn |
| 🟢 **Rất thấp** | **09** ghim enum | Giá trị mới == giá trị cũ → **không file `.asset` nào đổi** (kiểm chứng bằng `git diff`) |
| 🟢 **Rất thấp** | **10** thêm guard null | +5 dòng, không đổi behavior khi SO đúng |
| 🟢 **Rất thấp** | **12** thêm `LogError` | +2 dòng |
| 🟢 **Rất thấp** | **07 / 08** sửa asset | Sửa 2 field / 1 chuỗi, không đổi schema |
| ⚪ **0** | **13** Profiler | Không sửa code |

### Ma trận → thứ tự hành động

```
              Sửa ít rủi ro ────────────────────► Sửa nhiều rủi ro
          ┌──────────────────────┬──────────────────────────────┐
 Nghiêm   │  ★ LÀM NGAY         │  CHỜ PROFILER / CHỜ THIẾT KẾ │
 trọng    │  07 Stew rewards     │  01 bước 2, 02 JSON join     │
          │  12 thêm LogError    │  03 lựa chọn B               │
          ├──────────────────────┼──────────────────────────────┤
          │  LÀM NHANH           │  CÂN NHẮC KỸ                 │
 Chưa     │  09 ghim enum        │  04 UIMenuPopup               │
 nghiêm   │  08 đổi tên Stew     │  06 (khuyến nghị: không làm) │
 trọng    │  10 guard null        │  11 (chờ Addressables)       │
          │  05 Camera.main      │                              │
          │  12 đổi dict         │                              │
          ├──────────────────────┼──────────────────────────────┤
 Không    │  13 Profiler (= 0)   │  01 bước 1 (chỉ khi 02 đi    │
 phải lỗi │                      │   cùng, hoặc profiler báo)   │
          └──────────────────────┴──────────────────────────────┘
```

### Thứ tự hành động

1. **13** — đo baseline (rủi ro = 0, chốt được có nên làm Gói 3/4 không).
2. **Gói 1 — làm ngay, không cần doanh mục gì** (nghiêm trọng cao, rủi ro gần 0):
   `07` → `12 (LogError)` → `08` → `09` → `10 (guard)`
3. **Gói 2 — làm khi rảnh, vẫn an toàn:** `05` → `12 (đổi dict)`
4. **Gói 3 — chỉ khi profiler chứng minh:** `04` (bước 1 `UIMenuPopup`)
5. **Gói 4 — đề xuất không làm trừ khi vào giai đoạn đổi protocol / Addressables** *(đề xuất của
   agent, chờ bạn xác nhận — chưa phải quyết định)*: `01`, `02`, `03`, `06`, `11`


## Điều kiện kiểm chứng chung cho mọi mục

```
unityMCP_refresh_unity  {compile: request, mode: force, scope: all, wait_for_ready: true}
  → sleep 30s
unityMCP_read_console   {types: ["error","warning"]}   # phải 0/0
unityMCP_run_tests      {mode: EditMode}               # baseline 49/49 pass
Tools → Build Checks → Compile Android Player Scripts # DLL ở Temp/BuildChecks/AndroidPlayerScripts/
```

Chưa commit `DailyCooking/Assets/GooglePlayGames/.../current-build/` (untracked).
