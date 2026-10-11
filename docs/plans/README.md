# Plan chi tiết — 13 mục còn lại

Mỗi file trong thư mục này là một kế hoạch riêng cho một mục, ghi rõ: **hiện trạng (kèm
`file:line`), thay đổi gì, ảnh hưởng gì, cách kiểm chứng, rủi ro**.

Nguồn gốc: `docs/optimization_plan.md` §3, `docs/datamanagement.md` §4.
Kết quả phân tích lần này đã **sửa lại một số mô tả cũ trong optimization_plan cho khớp code** —
xem ghi chú ở từng file.

---

## A. Mạng / tối ưu — đã hoãn vì rủi ro cao

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

## Thứ tự đề xuất

1. **13** — đo baseline (không rủi ro, chốt được nhóm A).
2. **08** + **07** — 2 mục data nhỏ nhất, sửa nhanh, hết ngay.
3. **09** — ghim enum, chống bug project từng dính.
4. **05** — cache `Camera.main`, nhóm an toàn nhất nhóm A.
5. **12** — thêm log lỗi cho shop reward.
6. **04** bước 1 — nếu profiler chỉ ra S6 tốn GC.
7. Còn lại: chỉ làm khi profiler chứng minh, hoặc khi vào giai đoạn Addressables (11) / đổi wire protocol (01–03).

## Điều kiện kiểm chứng chung cho mọi mục

```
unityMCP_refresh_unity  {compile: request, mode: force, scope: all, wait_for_ready: true}
  → sleep 30s
unityMCP_read_console   {types: ["error","warning"]}   # phải 0/0
unityMCP_run_tests      {mode: EditMode}               # baseline 49/49 pass
Tools → Build Checks → Compile Android Player Scripts # DLL ở Temp/BuildChecks/AndroidPlayerScripts/
```

Chưa commit `DailyCooking/Assets/GooglePlayGames/.../current-build/` (untracked).
