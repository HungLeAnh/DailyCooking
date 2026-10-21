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
| 14 | **Runtime ghi thẳng vào material asset** — ✅ đã sửa | [14-grid-guide-material-mutation.md](14-grid-guide-material-mutation.md) · `GridBuildingSystem.cs:26` (`[SerializeField] Material gridGuideMaterial`) → `GridVisualizer.cs:24-28` `SetFloat(...)` | Chạy play mode 1 lần là `GridShaderMaterial.mat` bị commit đè vĩnh viễn (đã thấy `5/5.01` → `0/0.01`). Giá trị đó vô nghĩa vì runtime vẫn set lại, nhưng **working tree bẩn sau mỗi lần chạy** và designer có thể vô tình commit. Đã sửa bằng `new Material(sharedMaterial)` + `Dispose`; kiểm bằng `EditorUtility.IsDirty` và SHA256 của `.mat`. |
| 15 | **Cắt trên thớt không bấm được** — ✅ đã sửa, đã chơi kiểm | [15-cutting-interaction-ihasprogress.md](15-cutting-interaction-ihasprogress.md) · `CookingToolCounterController.cs:5` thiếu `IHasProgress` → `PlayerStateMachine.cs:200` luôn thấy `null` → `InteractAlternateEvent` không bao giờ chạy → `CookingTool.Cut()` không bao giờ được gọi | Phát hiện khi chơi thật để đo S7: đặt tomato lên thớt rồi click thì **nhấc tomato trả lại tay** thay vì cắt. Hồi quy từ `git show 9e3c5fe4e` (class cũ `CuttingCounterController`/`StoveCounterController` đều có `IHasProgress`). Chặn toàn bộ vòng lặp: không có slices → không nấu được → không phục vụ được → **không lên Level 2**. |
| 16 | **Tự động hoá input bằng Input System** — ✅ đã kiểm | [16-input-automation-and-ghost-drag.md](16-input-automation-and-ghost-drag.md) | Tìm ra nguyên nhân các lần inject trước thất bại: editor mất focus → `Application.isFocused == false` → `InputManager` chạy `Editor` update type → InputAction không actuate. Công thức: `IgnoreFocus` + `AllDeviceInputAlwaysGoesToGameView` + `runPlayerUpdatesInEditMode`, inject state trong `[DefaultExecutionOrder(-1000)]`. Kéo ghost bằng chuột: **PASS**. |
| 17 | **Build APK + 2 client** — ⛔ chưa kiểm được | [17-build-and-2client-status.md](17-build-and-2client-status.md) | Chặn bởi môi trường: thiếu mật khẩu keystore (`MyRestaurant.keystore`, alias `dev`) và máy thiếu Android SDK platform 35; 2 client cần process Unity thứ hai (MPPM tạo virtual player qua UI). |
| 18 | **Warning `Look rotation viewing vector is zero`** — ✅ đã sửa | [18-look-rotation-warning.md](18-look-rotation-warning.md) · `PlayerStateMachine.HandleMovement()` | Khi idle `moveDir == zero`, `Vector3.Slerp(a, zero, t)` trả đúng `zero` lúc `t >= 1` (frame ≥ 100 ms) → `transform.forward = zero` → `LookRotation` log warning. Bọc trong `if (Context.IsWalking)`. |
| 19 | **3 subscriber `OnMouseClickPerformed` / "1 click ra 3 nhát cắt"** — ✅ đã điều tra, không sửa | [19-click-subscribers-audit.md](19-click-subscribers-audit.md) | 3 subscriber có việc khác nhau; chỉ `PlayerStateMachine` cắt. Đo: mỗi lần nhấn = **1** event; 3 nhát cắt = **3 click** (`CookingTimer` 0→1→2→3). Không có bug. |
| 20 | **Kéo ghost bằng touch** — ✅ đã kiểm | [20-touch-drag-verification.md](20-touch-drag-verification.md) | Inject `TouchState` (`Began`/`Moved`/`Ended`) vào `Touchscreen`: `Pointer.current = FastTouchscreen`, `IsDragging=true`, ghost bám ô (2,2) → (4,1), thả → `false`. |
| 21 | **`StewSO.kitchenObjectSOList` trỏ sai** — ✅ đã sửa | `Assets/SO/FoodSO/StewSO.asset` · `BotCustomerController.cs:211-222` | FoodSO liệt kê `Potato Chopped + Carrot Slices`, trong khi `bowl.prefab` chỉ nhận `StewCookedSO` và `plate.prefab` không nhận nguyên liệu sống → **không thể phục vụ Stew**. Đổi list thành `[StewCookedSO]`; giữ `price=35`, `exp=40`, `_guid`. Serve + collect lại sau reimport: **54 coins / 40 XP**. |

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
| 04 | [04-ui-object-pooling.md](04-ui-object-pooling.md) | ⚠️ Popup root **đã cache sẵn**; `UICharacterItem` **đã pool**. Còn lại `UIMenuPopup` rebuild mỗi lần mở | TB | ✅ **Bước 1 đã làm (2026-10-21)** — `UIMenuPopup` create-once + pool; 10 lần mở vẫn 5 category / 20 food item |
| 05 | [05-camera-main-remaining.md](05-camera-main-remaining.md) | ✅ 17 lần gọi thật; **chỉ 1 đường per-frame** (`BuildingGhost` kéo ghost) | **Thấp** | ✅ **Đã làm** — vệ sinh code, không phải thắng perf có số đo |
| 06 | [06-playerstats-cache.md](06-playerstats-cache.md) | ⚠️ Allocation **đã xong**; `Update()` đã `!IsOwner` → chỉ 1 lần/frame, list ≤4 | Thấp | Không |

## B. Data cần bạn quyết (con số / tên) — ✅ đã chốt 2026-10-21

| # | File | Chốt | Rủi ro |
|---|---|---|---|
| 07 | [07-stewso-rewards.md](07-stewso-rewards.md) | **Giữ nguyên `35 / 40`**; đã phục vụ bot thu **54 coins + 40 XP** (2026-10-21); sửa thêm `kitchenObjectSOList` → `[Stew Cooked]` | Kỹ thuật thấp |
| 08 | [08-stewburnedso-name.md](08-stewburnedso-name.md) | **Giữ nguyên `Stew Burned`** (asset đang đúng) | Rất thấp |
| 09 | [09-configshop-enum-ints.md](09-configshop-enum-ints.md) | Cách ghim số + quy tắc thêm giá trị enum mới, `None = 9999` đã áp dụng | Thấp |

## C. Kiến trúc cần chốt

| # | File | Kết luận điều tra | Đề xuất |
|---|---|---|---|
| 10 | [10-counter-data-pattern.md](10-counter-data-pattern.md) | **2 pattern là đúng** (GUID = nội dung động, baked = nội dung cố định) | Không chuẩn hoá; thêm guard null + ghi quy ước |
| 11 | [11-kitchenobject-prefab-baked-so.md](11-kitchenobject-prefab-baked-so.md) | `PlacedObjectView` **đã sạch**; chỉ còn 1 vòng `KitchenObjectSO`↔prefab, và nó **bắt buộc** | Hoãn tới khi làm Addressables |
| 12 | [12-shop-guid-dictionaries.md](12-shop-guid-dictionaries.md) | Bước 1 + 2 + 4 **đã xong** (`217875b19`, `572f37092`); 1 chỗ linear `Find` không còn | **Xong** — test neg đã chạy: guid rác → `LogError` có guid + tên + category, ô ẩn. Ô ẩn "dính" session **đã sửa 2026-10-21** (`Refresh()` resolve lại reward) |

## D. Chưa làm vì thiếu dữ liệu

| # | File | Ưu tiên |
|---|---|---|
| 13 | [13-profiler-baseline.md](13-profiler-baseline.md) | **Cao nhất** — đã đo S3/S7-rút gọn (2026-10-21, Editor + probe); S7 đầy đủ, S8/S9 và GC trên Player build vẫn chờ |

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


## Kiểm bằng mắt — Gói 1 (2026-10-12)

Chạy trong play mode + **Relay host thật** (probe `Tools/Profiling/Enter Game (new host)`).
Ảnh chụp ra `Assets/Screenshots/` rồi **dời khỏi project** — không commit PNG vào repo.

| # | Kiểm | Kết quả |
|---|---|---|
| 07 | Menu popup → tile Stew | ✅ **`$35` / `40 XP`** |
| 08 | Option menu → `OptionMenuItemUI` | ✅ **`Stew Cooked`** / **`Stew Burned`** |
| 12 | Shop popup → tab Resource / Restaurant / Ingredients | ✅ **0 `LogError`**, mọi ô hiện đủ tên + giá + icon |
| 09 | Shop tab Restaurant → nhóm `None` | ✅ Nhóm `None` vẫn hiện. `UIShopItem.GetReward` `default:` là no-op → đổi `8`→`9999` **không đổi hành vi**, chỉ đổi nhãn nhóm từ `8` sang `None` |

### Công cụ kiểm bằng mắt (editor-only, `Assets/Editor/PerfScenarios.cs`)

| Menu | Dùng để |
|---|---|
| `Tools/Verify/Unlock all dishes + open Menu popup` | `RestaurantData.Level = 99` + `MenuData.AddUnlockedDish` mọi món → thấy giá thật thay vì overlay khóa |
| `Tools/Verify/Hide tutorial + show Stew tile` | Tắt `TutorialPanel` + `Window_QuestPointer` — cửa sổ focus của tutorial che đúng ô cần xem **và nuốt cả click chuột** |
| `Tools/Verify/Cooked vs burned stew in option menu` | Mở `UIOptionMenuPopup` đúng cách — `ShowPopup(null)` sẽ tự `HidePopup()`, bắt buộc truyền `Param` kèm sender `IHasOptionalSO` |
| `Tools/Verify/Open Shop popup once` | Mở shop popup 1 lần để bấm tay 3 tab |
| `Tools/Verify/Input diagnostics (hold input while it runs)` | Log 8 mẫu / ~3 s: `Keyboard.current.wKey`, `Pointer.current.position/press`, `GetMovementVectorNormalized()`, toạ độ player, `GetOwnerStats()` — **probe chẩn đoán input** |
| `Tools/Profiling/S4 - Start placement (ghost)` | `BuildingPlacementManager.SetPlacedObjectTypeSO` với object lấy từ `PlacedObjectDatabase`, toạ độ = raycast tâm Game view → dựng ghost mà không cần mua từ shop |
| `Tools/Profiling/S4 - Cancel placement` | `SetPlacedObjectTypeSO(null, …)` → nhả selection |
| `Tools/Verify/S4 state (ghost / drag / mouse)` | Đọc `BuildingGhost.IsDragging` / `StopMoving` / `IsMouseOverUI()` / toạ độ ghost ra console |

## Thử tự động hoá S3/S4 bằng computer-control — THẤT BẠI (2026-10-13)

Dùng computer-control MCP để lái chuột/phím thay vì chờ người chơi tay. **Không làm được**,
chi tiết đầy đủ (số probe, wiring joystick, cách loại nguyên nhân) nằm trong
[13-profiler-baseline.md](13-profiler-baseline.md) § "2026-10-13".

Tóm tắt bằng bằng chứng:

| Điều | Kết quả |
|---|---|
| Mapping toạ độ screen→game | ✅ **Khớp 0.5 px** — bấm screen `(1070,340)` → probe đọc `(1019.92, 569.29)`, dự kiến `(1019.9, 569.8)` |
| Chuột vào Input System | ✅ `Pointer.current.position` cập nhật, `press=True` bắt được |
| Bàn phím vào Input System | ❌ `w=False` dưới **cả 3** cách (`key_down`/`key_up`/`press_keys`), có/không `activate_window` |
| Player di chuyển | ❌ `Player(Clone)` đứng yên `(-3,0,-3)`; `Moving` luôn `(0,0)` |
| Đã loại: `GetOwnerStats()` null | ❌ Không phải — probe đọc `ownerStats=ok moveSpeed=5` |
| Đã loại: `IsDisableInput` | ❌ Không phải — mọi lời gọi `DisableInput(...)` đều bị comment |
| Joystick on-screen | ⚠️ Tay cầm = `OnScreenStick` → `<Gamepad>/leftStick`, `raycastTarget=1`; bấm **đúng tâm** `(250.32, 231.10)` `press=True` nhưng **tay cầm không nhúc nhích** |
| Vào placement cho S4 | ✅ Làm được từ code — log `Selected Changed: BowlTablewareCounter` |
| Xác nhận `isDragging == true` | ❌ Không đọc được → **số đo S4-thử không gán được cho `HandleDraging`** |

> **Cập nhật:** S3, S4, S7-rút gọn và bước phục vụ/nấu Stew **đã kiểm** (S4 ngày 2026-10-20;
> S3/S7-rút gọn + Stew ngày 2026-10-21). Còn **S7 đầy đủ** (nấu/cắt/phục vụ trong cửa sổ đo),
> **S8/S9** (2 client / late join) và **GC trên Player build** — xem
> [13-profiler-baseline.md](13-profiler-baseline.md) và
> [17-build-and-2client-status.md](17-build-and-2client-status.md).

### Cập nhật 2026-10-20 — kéo ghost **đã kiểm được**

Cách cũ (computer-control lái chuột OS) vẫn không dùng được, nhưng inject **pointer qua Input
System** thì được, sau khi phát hiện nguyên nhân: editor mất focus → `Application.isFocused=false`
→ `InputManager` chạy ở `Editor` update type → InputAction không actuate dù device state đã đổi.

Bật `backgroundBehavior=IgnoreFocus`, `editorInputBehaviorInPlayMode=AllDeviceInputAlwaysGoesToGameView`
và `InputManager.runPlayerUpdatesInEditMode=true`, rồi inject `MouseState` + `InputSystem.Update()`
trong một `MonoBehaviour` có `[DefaultExecutionOrder(-1000)]`. Kết quả: click vào collider ghost →
`IsDragging=true`; rê pointer tới ô (2,2) / (0,0) → ghost đứng đúng `(4,1,4)` / `(0,1,0)`; thả →
`IsDragging=false`; bấm nút Confirm qua `EventSystem` → counter spawn thật.

Chi tiết + giới hạn (chỉ host đơn, không phải tay người, chưa kiểm touch):
[16-input-automation-and-ghost-drag.md](16-input-automation-and-ghost-drag.md).

---

## Điều kiện kiểm chứng chung cho mọi mục

```
unityMCP_refresh_unity  {compile: request, mode: force, scope: all, wait_for_ready: true}
  → sleep 30s
unityMCP_read_console   {types: ["error","warning"]}   # phải 0/0
unityMCP_run_tests      {mode: EditMode}               # baseline 49/49 pass
Tools → Build Checks → Compile Android Player Scripts # DLL ở Temp/BuildChecks/AndroidPlayerScripts/
```

Kết quả chạy lại đầy đủ ngày **2026-10-20**: 0 error / 0 warning, **49/49 pass**,
**110 assemblies**. Build APK thật thì **chưa kiểm được** (thiếu mật khẩu keystore + máy thiếu
Android SDK platform 35), 2 client/late join cũng chưa kiểm — chi tiết và lý do ở
[17-build-and-2client-status.md](17-build-and-2client-status.md).

### Kiểm lại 2026-10-21 (sau nhóm 2)

Sau khi sửa warning quay + `UIShopItem.Refresh` + pool `UIMenuPopup`: refresh `force/all` →
**0 error / 0 warning**, `run_tests(EditMode)` **49/49 pass**, `Compile Android Player Scripts`
**110 assemblies**. Play mode (host đơn, Relay thật) dùng probe inject input rồi **xoá probe**:

- warning quay: đứng yên 8 s → không còn warning; gọi lại biểu thức cũ với `t = 1` → warning bắn
  đúng như mô tả (doc 18).
- click/cắt: mỗi lần nhấn = 1 event; 3 nhát cắt = 3 click (doc 19).
- touch: kéo ghost bằng `TouchState` → `IsDragging` bật/tắt đúng, ghost bám ô (doc 20).
- menu popup: 10 lần mở vẫn 5 category / 20 food item; thêm/xoá/thêm lại món đúng (doc 04).
- shop: ô bị ẩn bằng guid rác hiện lại sau khi mở lại shop (doc 12).

Không có PNG nào được commit; `git status` sau lượt chơi không có thay đổi asset ngoài 5 file code
+ tài liệu.

Chưa commit `DailyCooking/Assets/GooglePlayGames/.../current-build/` (untracked).

### Kiểm thêm 2026-10-21 — S3, S7-rút gọn, Stew

- **S3:** inject `W` → player `(-3,0,-3)` → `(-3,0,7.91)`; click `(690,538)` chọn
  `PlateTablewareCounter(Clone)` và spawn `plate(Clone)` vào tay. Click tổng hợp cần rebind
  `Click` sang `<Mouse>/leftButton`; probe đã xoá.
- **S7-rút gọn:** 60.0 s / 7689 frames, p50 **7.50 ms**, p95 **8.45 ms**, p99 10.73 ms,
  max 1582.11 ms (một stall đơn lẻ), GC **3722.4 KB/s** (Editor + probe — **chưa phải số Player
  build**), 5 bot active. **Không phải S7 đầy đủ**: cửa sổ này không chứa nấu/cắt/phục vụ.
- **Stew:** nấu `Potato Chopped` → `Stew Cooked` → `Stew Burned` (`stew_bowlBurned(Clone)`,
  burn timer 5.00). Phục vụ trong **bát**: bot order → `Eating` → `eaten=True`,
  `pendingCash=54` / `pendingExp=40`; collect → `Coins 200→254`, `Exp 0→40`.
  Đã sửa `StewSO.kitchenObjectSOList` → `[StewCookedSO]` và chạy lại sau reimport.
- **Chưa kiểm:** S7 đầy đủ (nấu/cắt/phục vụ trong cửa sổ đo), S8/S9 (2 client / late join) và
  GC trên Development Build.
- Không commit PNG; probe tạm `Assets/Scripts/TmpInputProbe.cs` đã xoá kèm `.meta`.
