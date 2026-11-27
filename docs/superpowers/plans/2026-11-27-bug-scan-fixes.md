# Plan sửa bug sau đợt quét toàn dự án (2026-11-27)

Nguồn: quét đọc-code theo 5 mảng (mạng/session, economy/save/ads/IAP, nấu ăn/counter/player,
bot/grid/tutorial, UI/camera/perf). Đã loại các mục có sẵn trong `docs/deferred.md`.

**Kiểm**: ✔ = đã đọc lại code, xác nhận đường lỗi · ◐ = có dẫn chứng code, **đọc lại khi bắt
đầu batch**; không tái hiện được thì bỏ và ghi lý do vào commit message.

---

## 0. Đã chốt với chủ dự án (2026-11-27)

| # | Câu hỏi | Quyết định |
|---|---|---|
| D1 | Upgrade Skill (tốc độ chạy/cắt, tip…) | **Cả quán hưởng.** Mua 1 lần, mọi người (kể cả người join sau) được buff. Chỉ số tính lại từ danh sách upgrade đã mua; không đổi format save |
| D2 | EXP khi lên cấp | **Giữ EXP dư, lên được nhiều cấp một lúc, thưởng coin cho từng cấp** |
| D3 | "Đuổi hết khách" khi có khách đang ăn | **Khách đang ăn được ăn xong và trả tiền**; chỉ đuổi khách chưa được phục vụ |
| D4 | Player id client tự khai | **Sửa phần rẻ ngay** (từ chối id rỗng, bỏ `SetplayerIdServerRpc`); xác thực token UGS → `deferred.md` như P0 bảo mật |
| D5 | Lời nhờ xem ad đè hộp Yes/No đang mở | **Đợi hộp kia đóng rồi mới hỏi**; quá hạn thì tính là hết giờ như hiện nay |
| D6 | Nhặt counter có dụng cụ rỗng trên đó | **Chặn**, báo "cất dụng cụ trước" |
| D7 | Nhánh | **Commit thẳng lên `feat/gesture-cooking`**, mỗi batch 1 commit |
| D8 | Test | Cuối mỗi batch Claude báo → **chủ dự án đóng Editor** → Claude chạy EditMode batchmode; kịch bản tay (Multiplayer Play Mode) chủ dự án chạy theo checklist |

Quyết định kỹ thuật Claude tự chốt (đổi được nếu cần):
- Cosmetic: server chỉ chấp nhận mảnh đã mở khoá, `-1` (bỏ trống) hoặc giá trị người chơi **đang
  mặc sẵn**. Mảnh bị khoá thì bỏ riêng mảnh đó, không từ chối cả yêu cầu. Lý do: UI gửi mọi mảnh kể
  cả mặc định, mà mảnh mặc định có thể chưa "mở khoá".
- IAP: gói gem chưa grant được (không ở quán đang host) được giữ trong danh sách riêng, grant khi
  host quán. Nếu người chơi không host lại trong 3 ngày thì Google vẫn hoàn tiền (chấp nhận được,
  vì chỉ host mới mua được gem).
- Level-up/Reward popup đang mở mà có thưởng mới: **cộng dồn theo loại thưởng** (1 dòng/loại), tiêu
  đề lấy cấp mới nhất.

## 1. Quy trình chung

**Mỗi batch:**
1. Đọc lại code các mục ◐ của batch; mục nào không tái hiện được thì bỏ.
2. Logic cần test → lớp C# thuần trong `Assets/Scripts/...` + test trong `Assets/Tests/`
   (EditMode, `Tests.asmdef`); MonoBehaviour/NetworkBehaviour chỉ gọi vào.
3. Claude báo xong code → chủ dự án **đóng Editor** → Claude chạy:
   ```
   Unity.exe -batchmode -projectPath DailyCooking -runTests -testPlatform EditMode -testResults results.xml
   ```
   (lần chạy này cũng import script mới và sinh file `.meta`). Báo pass/fail kèm output.
4. Chủ dự án mở Editor, chạy checklist tay của batch (Multiplayer Play Mode, `Tools/Verify/*`,
   join code `LOCAL`).
5. Commit **chỉ các file của batch**. Trong index đang có sẵn ~36 thay đổi/xoá không thuộc đợt này
   (`docs/plans/*`, `PerfScenarios.cs`…), nên **không** dùng `git commit -a` hay `git add .`:
   ```
   git add <file mới>.cs <file mới>.cs.meta          # chỉ file mới
   git commit -o -m "fix(scope): …" -- <mọi file của batch>
   ```
   `-o` (`--only`) chỉ commit các path đã liệt kê; thay đổi đang staged của người khác được giữ nguyên.
   Không đụng `DailyCooking/Assets/Editor/PerfScenarios.cs` (đang có sửa đổi chưa commit của người khác).
6. Commit message có scope (`fix(bot): …`) + dòng `Co-Authored-By` theo quy ước.

**Thứ tự:** B1 → B2 → B3 → B4 → B5 → B6 → B7 → B8 → B9 (docs). B6 cần `HideAll` của B4.

| Batch | Nội dung | Ưu tiên | Cỡ |
|---|---|---|---|
| B1 | Bot, phục vụ, đĩa, ghế | P0 | M |
| B2 | Economy: EXP, upgrade, cosmetic, IAP, daily, save | P0/P1 | L |
| B3 | Định danh người chơi trong session | P0/P1 | M |
| B4 | Vòng đời session & UI | P1 | M |
| B5 | Menu tuỳ chọn chọn theo GUID | P1 | M |
| B6 | Quảng cáo | P1 | M |
| B7 | Nấu ăn: gesture, output, đồ đang cầm | P2 | M |
| B8 | Grid: nhặt/đặt đồ | P2 | S |
| B9 | Cập nhật docs | — | S |

---

## B1 — Bot, phục vụ, đĩa, ghế · `fix(bot)` · P0

| # | Bug | Kiểm |
|---|---|---|
| 1.1 | Đĩa đã ăn phục vụ lại được → trả tiền vô hạn; kéo theo double-book ghế | ✔ |
| 1.2 | Đồng hồ kiên nhẫn chạy cả lúc đang ăn → khách bỏ về không trả tiền, ghế + bàn kẹt tới hết session | ✔ |
| 1.3 | "Đuổi hết khách" để lại đĩa chưa ăn → ghế kẹt | ✔ |
| 1.4 | Bot lấy từ pool giữ emotion/timer cũ | ✔ |
| 1.5 | `BotManager` không dọn pool giữa các session → host lần 2 không có khách | ✔ code, cần kiểm runtime |
| 1.6 | Giật được đĩa đã ăn từ tay người chơi khác | ◐ |
| 1.7 | Bàn biến mất khi khách đang đi tới → kẹt `WalkToTable` mãi | ◐ |
| 1.8 | Path tạm bị chặn → thử lại 1 lần rồi bỏ về | ◐ |

**Lớp thuần mới** `Assets/Scripts/Bot/BotRules.cs`:
- `static bool CanServe(bool isEaten, bool isServed)` → `!isEaten && !isServed`.
- `static bool PatienceTicks(BotStateType state)` → true với `WaitForTable`, `WalkToTable`, `OrderFood`,
  `WaitingForFood`; false với `Idle`, `Eating`, `Leaving`.
- `static bool CanKick(BotStateType state)` → false với `Eating`, `Leaving` (D3).
- `static bool CanPickUpEatenPlate(bool isEaten, bool onTable, bool actorHasItem)`.

**Sửa:**
1. [WaitingForFoodState.cs:16-34](../../../DailyCooking/Assets/Scripts/Bot/States/WaitingForFoodState.cs) `OnFoodServed`:
   return sớm khi `!BotRules.CanServe(tableware.IsEaten, tableware.IsServed)`.
   [TablewareKitchenObject.cs:74](../../../DailyCooking/Assets/Scripts/KitchenObject/TablewareKitchenObject.cs) `TryAddIngredient`:
   trả `false` khi `isEaten.Value || isServed.Value`.
2. [BotCustomerController.cs:184-206](../../../DailyCooking/Assets/Scripts/Bot/BotCustomerController.cs) `Update`: chỉ chạy đồng hồ khi
   `currentEmotion != None && BotRules.PatienceTicks(CurrentStateType.Value)`. Tip giữ theo mood lúc
   phục vụ (`tipPercentage` không đổi).
3. `ResetBot` (`:349`): thêm `currentEmotion.Value = EmotionType.None`, `ResetClockTimer()`,
   `waitingFood.Clear()`. `ShowOrder` (`:329`): thêm `ResetClockTimer()`.
4. [BotManager.cs:215](../../../DailyCooking/Assets/Scripts/Bot/BotManager.cs) `KickAllBots`: bỏ qua bot có
   `!BotRules.CanKick(state)`; bot đang ăn tự `FinishEating` → `Leaving` như bình thường.
5. `BotManager` vòng đời:
   - `OnNetworkSpawn` (server): `pools.Clear(); activeBots.Clear();` **trước** khi tạo pool; subscribe
     `GameManager.OnStateChanged` ở đây (bỏ khỏi `Start`, vì `Start` chạy ở main menu khi chưa là server).
   - Thêm `OnNetworkDespawn`: dừng `spawnCoroutine`, unsubscribe, clear `pools`/`activeBots`.
   - **Kiểm runtime trước khi sửa:** host → thoát về menu → host lại → có `MissingReferenceException`
     / không có khách? Ghi kết quả vào commit message.
6. [TablewareKitchenObject.cs:132](../../../DailyCooking/Assets/Scripts/KitchenObject/TablewareKitchenObject.cs) `InteractEvent`: chỉ
   cho nhặt khi `GetKitchenObjectParent() is Table` (dùng `BotRules.CanPickUpEatenPlate`).
7. [WalkToTableState.cs:44](../../../DailyCooking/Assets/Scripts/Bot/States/WalkToTableState.cs): `seatTransform == null || table == null`
   → `Leave()` thay vì `return`. `Table`: chuyển `TableManager.UnregisterTable` từ `OnDestroy` sang
   `OnNetworkDespawn` (giữ `OnDestroy` gọi lại an toàn nếu chưa unregister).
8. `WalkToTableState.Update` nhánh path lỗi: reset `waitTimer = 0` sau mỗi lần tính lại; tối đa 3 lần
   (15 s) mới `Leave()`.

**Test** `Assets/Tests/BotRulesTests.cs`: `CanServe` (4 tổ hợp), `PatienceTicks` (mọi state),
`CanKick`, `CanPickUpEatenPlate`.

**Checklist tay** (host + Player 2):
- [ ] Khách A ăn xong, nhặt đĩa (nhận tiền) → đưa cho khách B gọi cùng món: bị từ chối.
- [ ] Phục vụ khách lúc mặt giận và đồng hồ gần hết → khách ăn xong, trả tiền, ghế dùng lại được.
- [ ] Bấm "Đuổi hết khách" khi 1 khách đang ăn → khách đó ăn xong, trả tiền; khách khác về; ghế trống.
- [ ] Host → thoát về menu → host lại cùng quán → sau ~10 s có khách.
- [ ] Player 2 cầm đĩa đã ăn → host bấm vào đĩa đó → không giật được.

---

## B2 — Economy · `fix(economy)` · P0/P1

| # | Bug | Kiểm |
|---|---|---|
| 2.1 | Mua gem treo rồi bị Google hoàn tiền (package IAP đánh dấu "đã xử lý" dù game chưa grant) | ✔ |
| 2.2 | Upgrade Skill chỉ buff người mua, người khác không mua được (D1) | ✔ |
| 2.3 | EXP dư bị xoá, tối đa 1 cấp/lần (D2) | ✔ |
| 2.4 | Level-up/Reward popup nhân dòng khi hiện lại lúc đang mở | ◐ |
| 2.5 | Server cho mặc cosmetic chưa mở khoá | ✔ |
| 2.6 | Ngày reset daily-free không mirror → qua 0h UTC client thấy offer còn, UI hiện "+amount" dù server từ chối | ◐ |
| 2.7 | Index save hỏng → tạo quán trùng tên ghi đè file save cũ | ◐ |

**Lớp thuần mới / sửa:**
- `Assets/Scripts/Managers/SkillStats.cs`: `static void Recompute(PlayerStats stats, IEnumerable<UpgradeSO> purchased)`
  → đặt về giá trị gốc (move 5, cut 1, carry 1, tip 0), rồi áp từng upgrade Skill (`MoveSpeed`/`CuttingSpeed`
  nhân `1+value`, `CarryingCapacity`/`TipIncrease` cộng). Kết quả không phụ thuộc thứ tự, gọi lại nhiều
  lần vẫn ra cùng số. Giá trị gốc lấy từ hằng trong `PlayerStats` (thêm `const`).
- `Assets/Scripts/Managers/CosmeticRules.cs`: `static Dictionary<string,int> Sanitize(requested, current, Func<string,int,bool> isUnlocked)`.
- `EconomyRules.LevelUpRewardCoins(int fromLevelExclusive, int toLevelInclusive)` = tổng `GetLevelUpRewardCoins(l)`.

**Sửa:**
1. **IAP** [IAPManager.cs](../../../DailyCooking/Assets/Scripts/Store/IAPManager.cs):
   - Tách `TryGrantAndConfirm(PendingOrder)` từ `OnPurchasePending` (validate → grant nếu chưa grant →
     `ConfirmPurchase`).
   - Thêm `Dictionary<string, PendingOrder> waitingForRestaurant` (theo `TransactionID`).
     `OnPurchasePending` khi `!CanGrantNow()` → lưu vào đây. `OnPurchasesFetched` → lưu mọi
     `orders.PendingOrders` chưa grant (package đã lọc event nên phải tự giữ).
   - `RetryPendingPurchases`: nếu `CanGrantNow()` → `TryGrantAndConfirm` từng order đang giữ rồi xoá
     khỏi danh sách; sau đó vẫn `FetchPurchases()`.
   - Chỉ kiểm được trên thiết bị (Editor dùng fake store, APK đang bị chặn bởi keystore): ghi vào
     checklist thiết bị trong `deferred.md`.
2. **Upgrade Skill = cả quán (D1)** [GameManager.Economy.cs:183-225](../../../DailyCooking/Assets/Scripts/Managers/GameManager.Economy.cs):
   - `PurchaseUpgradeServerRpc`: bỏ điều kiện `buyerStats == null`; sau `GameData.PurchaseUpgrade` gọi
     `ServerRecomputeSkillStats()`.
   - `ServerRecomputeSkillStats()`: với mọi `PlayersStats` → `SkillStats.Recompute(…)` →
     `SetPlayerStatsClientRpc(playerId, …)` (mirror tổng, không delta).
   - Gọi thêm khi: host vừa có `GameData` (`NewGame`/`LoadGame` → lúc server spawn) và khi
     `GameData.OnPlayerStatsAdded` trên server (người mới join được buff ngay).
   - Xoá `ApplyUpgradeToStats` cũ (chỉ upgrade gọi nó; đã grep: không có chỗ nào khác sửa stats).
   - Save cũ: người mua giữ đúng số cũ, người khác được buff khi load. Không đổi format, không bump
     `SaveVersion`.
3. **EXP (D2)** [RestaurantData.cs:63](../../../DailyCooking/Assets/Scripts/Managers/RestaurantData.cs) `UpdateRestaurantExp`:
   `while (Exp >= Level * 100) { Exp -= Level * 100; Level++; OnLevelUp?.Invoke(Level); }` (một lần
   `OnLevelChange`). `ServerAddExp` (`GameManager.Economy.cs:24`): trả
   `EconomyRules.LevelUpRewardCoins(levelBefore, levelAfter)` bằng **một** `ApplyCoins`. Client đã hỗ
   trợ nhiều cấp (`SetProgress`).
4. **Popup thưởng** `UILevelUpPopup` / `UIRewardPopup`: nếu `ShowPopup` khi đang mở → cộng dồn amount
   vào dòng cùng loại thưởng thay vì thêm dòng; level text = cấp mới nhất.
5. **Cosmetic** [GameManager.Data.cs:128](../../../DailyCooking/Assets/Scripts/Managers/GameManager.Data.cs) `SaveCustomizationServerRpc`:
   sau `DecodeCustomizations` → `CosmeticRules.Sanitize(decoded, stats.CharacterCustomizationIds,
   GameData.CosmeticData.IsCosmeticUnlocked)`; mirror bản đã lọc (encode lại) chứ không mirror chuỗi
   client gửi.
6. **Daily-free** `SetDailyFreeCountsClientRpc` (`GameManager.Economy.cs:313`): gửi thêm ngày reset
   (`long` ticks UTC); `ShopData.SetDailyFreeCounts(coin, gem, refreshDate)`. Popup "+amount" ở
   [UIDailyFreeItem.cs](../../../DailyCooking/Assets/Scripts/UI/UIPopup/Popups/ShopPopup/UIDailyFreeItem.cs) chỉ hiện khi server xác nhận: server gửi
   `DailyFreeGrantedRpc(type, amount)` về đúng client claim (host gọi trực tiếp); bỏ `ShowPopup` ngay
   sau `ClaimDailyFreeServerRpc`.
7. **Save** [GameManager.Save.cs:86](../../../DailyCooking/Assets/Scripts/Managers/GameManager.Save.cs) `NewGame`: từ chối thêm khi
   file `GameData_<name>` đã tồn tại trên đĩa (`FileDataHandler` thêm `Exists()`), alert "Đã có
   quán tên này".

**Test:**
- `RestaurantDataTests`: giữ dư (95 + 40 ở cấp 1 → cấp 2, 35 EXP), lên 2 cấp một lần,
  `OnLevelUp` bắn đúng từng cấp; test cũ "100 EXP → cấp 2, 0 EXP" vẫn pass.
- `EconomyTests`: `LevelUpRewardCoins(1,3)` = thưởng cấp 2 + cấp 3; `(2,2)` = 0.
- `SkillStatsTests` (mới): rỗng → giá trị gốc; 2 upgrade đổi thứ tự cùng kết quả; gọi 2 lần không nhân đôi.
- `CosmeticRulesTests` (mới): mảnh mở khoá qua; mảnh khoá bị bỏ; `-1` qua; giá trị đang mặc qua dù khoá.
- `GameDataTests`/`ShopData`: `SetDailyFreeCounts` với ngày hôm nay → client không tự reset.

**Checklist tay:**
- [ ] Cheat EXP sát mốc → phục vụ 1 món → giữ EXP dư; cheat lớn → lên 2 cấp, coin = tổng 2 mức, 1 popup.
- [ ] Player 2 mua Speed Boost → host và Player 2 đều nhanh hơn; Player 3 join sau cũng nhanh hơn.
- [ ] Load save cũ có upgrade → chỉ số đúng, không nhân đôi.
- [ ] Đổi trang phục bằng mảnh đã mở → các máy thấy giống nhau.

---

## B3 — Định danh người chơi trong session · `fix(net)` · P0/P1

| # | Bug | Kiểm |
|---|---|---|
| 3.1 | Máy từng là client rồi host → `playerDataNetworkList` còn entry cũ → client mới bị gán nhầm player id | ✔ |
| 3.2 | Cùng tài khoản join phòng mình đang host → chiếm avatar host | ✔ |
| 3.3 | Reclaim ghế đang giữ không giữ chỗ trong lúc sync → avatar bị despawn giữa chừng, phòng có thể vượt 4 | ◐ |
| 3.4 | Rejoin rớt transport tự nhiên → lần thử sau không về scene sạch | ◐ |
| 3.5 | Player id client tự khai (D4: phần rẻ) | ✔ |

**Sửa:**
1. [MultiplayerManager.cs](../../../DailyCooking/Assets/Scripts/Network/MultiplayerManager.cs):
   - `OnNetworkSpawn` khi `IsServer`: `playerDataNetworkList.Clear()` (trước khi host tự thêm mình;
     kiểm thứ tự bằng log, nếu callback connect của host chạy trước thì clear trong
     `PrepareHostCallbacks`/ngay sau `StartHost`).
   - `NetworkManager_Server_OnClientConnectedCallback` (`:345`): trước `Add`, xoá mọi entry trùng
     `clientId` hoặc trùng `playerId` (phòng thủ).
2. Approval (`:399`) + [SeatRegistry.cs](../../../DailyCooking/Assets/Scripts/Network/Session/SeatRegistry.cs):
   - `Approve(..., string hostPlayerId)` → thêm kết quả `RefusedHostAccount` (enum không serialize,
     append cuối) khi `playerId == hostPlayerId`; và `RefusedNoPlayerId` khi id rỗng (D4).
   - MultiplayerManager từ chối với reason mới; [SessionEndText.cs](../../../DailyCooking/Assets/Scripts/Network/Session/SessionEndText.cs) có câu:
     "Tài khoản này đang mở quán trên máy khác" / "Chưa đăng nhập".
   - Phòng thủ: không bao giờ `ApprovedReplaceLive` khi `LiveClient == ServerClientId`.
   - **Kiểm MPPM:** Player 2 phải có player id khác host (log `approved <id>`). Nếu trùng (cùng
     PlayerPrefs → cùng tài khoản anonymous) → thêm `AuthenticationService.SwitchProfile` theo tag MPPM
     trong `#if UNITY_EDITOR` trước khi sign-in, nếu không MPPM sẽ bị từ chối join.
3. **Reclaim** `SeatRegistry`: khi trả `ApprovedReclaimHeld` → chuyển hold sang
   `reclaiming[clientId] = (playerId, heldUntilCũ, now)`; `TakenSeats` tính cả `reclaiming`;
   `Connected` xoá; `DroppedBeforeConnect` trả lại hold (deadline cũ); `Expire`: reclaim quá
   `PendingSeconds` → trả lại hold. `IsHeld` trả true khi đang reclaim (rà các chỗ gọi `IsHeld`).
4. **Rejoin** `ReconnectLoop` (`:198`): nhánh `StartAttempt` → nếu `needsFreshStart` **hoặc** scene hiện
   tại ≠ `MainMenuScene` → `PrepareRejoin()` (chờ vòng sau), ngược lại `TryRejoin`.
5. **D4:** xoá `SetplayerIdServerRpc` + lời gọi ở `:464` (approval đã gắn id). Thêm vào `deferred.md`
   mục P0: "xác thực player id bằng access token UGS (JWT) ở approval".

**Test** `SeatRegistryTests`: từ chối id của host; từ chối id rỗng; reclaim chiếm ghế trong lúc sync
(phòng 4/4 không nhận thêm); reclaim rớt → hold quay lại với deadline cũ; reclaim treo quá 30 s → hold
quay lại rồi hết hạn đúng giờ. `ReconnectStepTests` nếu đổi `ReconnectStep`.

**Checklist tay:**
- [ ] Player 2 join quán host A → rời → Player 2 tự host → Player 3 join → Player 3 mua upgrade/đổi đồ:
  áp đúng Player 3 (xem log player id).
- [ ] Join bằng chính tài khoản host (2 máy cùng tài khoản, hoặc ép id) → bị từ chối, có thông báo.
- [ ] Player 2 rớt mạng ~55 s rồi vào lại → giữ avatar + đồ đang cầm.

---

## B4 — Vòng đời session & UI · `fix(ui)` · P1

| # | Bug | Kiểm |
|---|---|---|
| 4.1 | Kết thúc session chỉ đóng popup Settings → popup khác còn mở chặn interstitial, NRE ở main menu, hiện lại ở ván sau | ✔ |
| 4.2 | Singleton trùng vẫn chạy phần Awake còn lại (GameInput thêm input action mỗi lần về menu…) | ✔ |
| 4.3 | NetworkManager trùng (DDOL) tích dần mỗi lần về MainMenu | ◐ |
| 4.4 | HUD bấm được trên client trước khi snapshot về → NRE | ◐ |
| 4.5 | Main menu cho Join trong lúc host đang await Relay; host fail không báo | ✔ |
| 4.6 | Tutorial của host reset về bước 0 mỗi khi có client join/reconnect | ✔ |
| 4.7 | Shop/Character popup unsubscribe khỏi `GameData` mới thay vì instance đã subscribe | ✔ |

**Sửa:**
1. `UIPopupManager.HideAll(params UIPopupType[] keep)`: ẩn mọi popup trong `visiblePopupList` (duyệt
   ngược) trừ `keep`. `MultiplayerManager.RunExit` (`:293`) gọi `HideAll()` thay cho
   `HidePopup(UISettingPopup)`. `UIInventoryPopup.HidePopup`: null-guard `GridBuildingSystem.Instance`.
2. `Singleton<T>`/`NetworkSingleton<T>`: thêm `protected bool IsDuplicate => s_Instance != this;`. Mọi
   lớp persistent override `Awake` thêm `if (IsDuplicate) return;` ngay sau `base.Awake()`:
   `BotManager`, `KitchenGameManager`, `EmotionManager`, `GameManager`, `TutorialManager`,
   `CounterModules`, `MultiplayerManager`, `GameInput`, `MusicManager`, `SoundManager`,
   `UIPopupManager` (`ConfigManager` đã có). `GameInput` thêm `OnDestroy`: disable + `Dispose`
   `playerAction` khi là instance thật.
3. NetworkManager trùng: trong `MultiplayerManager.SceneManager_sceneLoaded`, destroy mọi
   `NetworkManager` khác `NetworkManager.Singleton`. **Trước khi sửa:** xem GameObject chứa
   NetworkManager trong `MainMenuScene` chỉ có NetworkManager + transport (nếu có component khác thì
   chỉ destroy component).
4. Client đang `Syncing` (cả join lần đầu): bật `ReconnectingOverlay.SetVisible(true, "Đang tải dữ liệu quán…")`,
   tắt ở `Session.SnapshotApplied`. Null-guard `GameData`/`playerData` trong `ShowPopup` của Shop, Menu,
   Character, Inventory, LevelUp.
5. [UIMainMenuPopup.cs](../../../DailyCooking/Assets/Scripts/UI/UIPopup/Popups/UIMainMenuPopup.cs): cờ `isStarting` chặn New/Load/Join khi đang
   khởi động (`try/finally` reset cờ); host trả join code rỗng → alert "Không mở được quán, thử lại"
   + `GameManager.ClearActiveSave()`.
6. [TutorialManager.cs:41](../../../DailyCooking/Assets/Scripts/Managers/TutorialManager.cs): bỏ nghe `GameManager.OnPlayerSpawned` (server, mọi
   avatar). Thêm event tĩnh `PlayerStateMachine.OnLocalPlayerSpawned` (bắn ở `OnNetworkSpawn` khi
   `IsOwner`); tutorial chỉ chạy khi `NetworkManager.IsServer` (giữ host-only như cũ) và chưa có panel
   đang chạy.
7. `UIShopPopup`/`UICharacterPopup`: lưu instance `ShopData`/`CosmeticData` đã subscribe, unsubscribe
   đúng instance đó (như `UIShopItem`).

**Test:** logic chủ yếu là vòng đời Unity → kiểm tay. Nếu `HideAll` tách được thứ tự ẩn ra hàm thuần
thì thêm test nhỏ; không bắt buộc.

**Checklist tay:**
- [ ] Client mở Shop → host thoát → client về menu: không còn popup, không lỗi Console; vào ván mới
  interstitial chạy lại.
- [ ] Vào/ra ván 3 lần → Console không cảnh báo nhiều NetworkManager; input không bị lặp (zoom 1 nấc/lần).
- [ ] Join → bấm Shop ngay khi vào scene: thấy overlay tải, không NRE.
- [ ] Bấm New rồi Join liên tục ở main menu: chỉ 1 hành động chạy.
- [ ] Quán mới (chưa xong tutorial) → Player 2 join giữa tutorial: tutorial host không reset.

---

## B5 — Menu tuỳ chọn chọn theo GUID · `fix(net)` · P1

| # | Bug | Kiểm |
|---|---|---|
| 5.1 | Hai người mở cùng menu, A lấy hết 1 món → list dịch → B nhận nhầm món; PostBox xoá nhầm package khỏi save; client sửa đổi gửi index bất kỳ không cần được hiện menu | ✔ |
| 5.2 | Menu cắt lọc null nhưng `ApplyOption` index list chưa lọc | ✔ |

**Lớp thuần mới** `Assets/Scripts/Managers/OptionOffers.cs`: `Offer(ulong client, ulong targetId, IEnumerable<string> guids)`,
`bool TryConsume(ulong client, ulong targetId, string guid)` (đúng target + guid thuộc offer → xoá
offer, trả true), `Forget(ulong client)`.

**Sửa:**
1. [UIManager.cs](../../../DailyCooking/Assets/Scripts/Managers/UIManager.cs): `ShowOptionMenu` ghi offer; `RequestOption(sender, string guid)`;
   `ChooseOptionServerRpc(target, string guid)` → `TryConsume` thất bại thì bỏ qua; `Forget` khi client
   disconnect.
2. `IHasOptionalSO.ApplyOption(PlayerStateMachine actor, string optionGuid)`; mỗi nơi tự tìm entry theo GUID
   **trong list hiện tại**, không còn thì alert "Món này vừa hết":
   - [OptionalContainerCounterController.cs:52](../../../DailyCooking/Assets/Scripts/MVC/Controllers/CounterControllers/OptionalContainerCounterController.cs): index đầu tiên có GUID đó.
   - [PostBox.cs:108](../../../DailyCooking/Assets/Scripts/MVC/Controllers/CounterControllers/PostBox.cs): như trên; `ServerRemovePostBoxPackage` theo GUID đó.
   - `CookingToolCounterController` → [CookingTool.ApplyOption](../../../DailyCooking/Assets/Scripts/Counters/CookingTool.cs) (`:944`): tìm recipe cắt có
     `output.Guid == guid` trong **list gốc** để lấy index cho `ApplyCuttingOption`; combine tương tự
     theo output GUID.
3. `UIOptionMenuPopup` (`:122`): gửi `optionalList[index].Guid`.

**Test** `OptionOffersTests`: consume đúng → true rồi hết; sai target / guid ngoài offer / không có
offer → false; offer mới thay offer cũ.

**Checklist tay:**
- [ ] Host + Player 2 cùng mở menu tủ nhiều món; host lấy hết món A; Player 2 chọn món B → nhận đúng B;
  chọn A → "Món này vừa hết".
- [ ] PostBox: tương tự với 2 package; save sau đó còn đúng package.
- [ ] Thớt có nhiều cách cắt: chọn từng cách → ra đúng món.

---

## B6 — Quảng cáo · `fix(ads)` · P1

| # | Bug | Kiểm |
|---|---|---|
| 6.1 | `OnAdRewarded` tới sau `OnAdClosed` → ad xem hết bị báo "không xem xong" | ✔ code (thứ tự event theo tài liệu LevelPlay) |
| 6.2 | Yêu cầu ad *forced* tới client chưa InGame → client im lặng → host bị khoá popup tới 120 s | ✔ |
| 6.3 | Lời nhờ xem ad dùng chung hộp Yes/No → đè hộp "Xác nhận mua/Mở khoá" đang mở (D5); chiều ngược lại hộp mua cũng đè lời nhờ, rồi timeout ẩn nhầm hộp mua | ✔ |

**Lớp thuần mới:**
- `Assets/Scripts/Ads/RewardedAdSession.cs`: `OnRewarded()`, `OnClosed(float now)`, `bool TryResolve(float now, out AdOutcome)`.
  Đóng mà chưa có reward → chờ thêm `LateRewardGraceSeconds = 1f`; reward tới trong lúc chờ → `Completed`.
- `Assets/Scripts/UI/UIPopup/ConfirmQueue.cs` (generic): `Show(T)` (đang có hộp mở → xếp hàng), `Close()`
  → trả về item kế tiếp, `Cancel(T)` (đang hiện → đóng và hiện item kế tiếp; đang chờ → rút khỏi hàng),
  `Clear()`.

**Sửa:**
1. [AdsManager.cs:217-228](../../../DailyCooking/Assets/Scripts/Ads/AdsManager.cs): dùng `RewardedAdSession`; khi đóng, chờ tối đa 1 s
   (`Update` hoặc coroutine realtime) rồi mới `FinishRewarded`.
2. [GameManager.AdDelegation.cs:200](../../../DailyCooking/Assets/Scripts/Managers/GameManager.AdDelegation.cs) `AskWatchClientRpc`: khi `!InPlayingSession()` mà
   vẫn kết nối → `ReportAdServerRpc(requestId, AdOutcome.NoAd)` thay vì return im lặng.
   `AdDelegationRules.TryGetOverdue`: request forced chưa `Accepted` cũng quá hạn sau
   `AnswerDeadlineSeconds`. Client gửi `AdAcceptedServerRpc` khi ad forced bắt đầu chiếu.
3. **D5** `UIGameConfirmPopup`: hàng đợi `ConfirmQueue<Param>`. `ShowPopup` khi đang có hộp → xếp hàng;
   bấm Yes/No → đóng hộp hiện tại, hiện hộp kế tiếp; thêm `Cancel(Param)`.
   `AskConsent`: giữ tham chiếu `Param` (thay `adConsentPopupOpen`); hết `ConsentSeconds` mà chưa trả lời
   → `Cancel(param)` (đang chờ thì chỉ rút khỏi hàng, **không** ẩn hộp mua) + báo `Timeout`. Các chỗ
   `HidePopup(UIGameConfirmPopup)` trong AdDelegation (`:235, :431, :465`) đổi sang `Cancel(param)`.
   `UIPopupManager.HideAll` (B4) gọi `Clear()` hàng đợi.

**Test:** `RewardedAdSessionTests` (reward trước close; close rồi reward trong 1 s; close không reward
→ `FailedOrClosedEarly` sau 1 s; display fail). `ConfirmQueueTests`. `AdDelegationRulesTests`: forced
chưa accept quá hạn; forced đã accept không quá hạn.

**Checklist tay** (`FakeAdProvider` trong Editor):
- [ ] Player 2 mở "Xác nhận mua" → host nhờ xem ad → hộp mua giữ nguyên; bấm No → lời nhờ hiện ra.
- [ ] Để lời nhờ chờ quá 20 s sau hộp mua → host thấy hết giờ, hộp mua không bị ẩn.
- [ ] Ép forced tới Player 2 đang Syncing → host không bị khoá popup.

---

## B7 — Nấu ăn · `fix(cooking)` · P2

| # | Bug | Kiểm |
|---|---|---|
| 7.1 | Gesture NaN/Inf lọt qua check (Pour NaN → 2 sao, Flip NaN spam RPC tới mọi client) | ✔ |
| 7.2 | Stir nhận mọi prompt cũ bất kỳ lúc nào; prompt index có thể bằng `ExpectedStirPrompts` | ✔ |
| 7.3 | Đường click-to-cut cũ bỏ qua chấm điểm vuốt | ✔ |
| 7.4 | Xoá nguyên liệu trước khi spawn output; spawn null → mất đồ, tool kẹt | ✔ |
| 7.5 | Avatar không có player id bị despawn → đồ đang cầm lơ lửng | ◐ |
| 7.6 | Tool manual-start bấm nút HUD rồi giữ rót sau → lệch > 0.2 → 1 sao không lý do | ✔ |

**Sửa:**
1. [CookStepRecorder.cs](../../../DailyCooking/Assets/Scripts/Counters/Gestures/CookStepRecorder.cs): `RecordSwipe/RecordFlip/RecordPour` từ chối
   `float.IsNaN/IsInfinity` và giá trị vượt khoảng hợp lệ (đọc `GestureRecognizers.PourLevel` để lấy
   cận trên thật, không đoán).
2. `RecordStir(int promptIndex, int current, int expected)`: nhận khi
   `0 <= promptIndex < expected && current - 1 <= promptIndex <= current`. Sửa lời gọi ở
   [CookingTool.cs:692](../../../DailyCooking/Assets/Scripts/Counters/CookingTool.cs).
3. [CookingToolCounterController.cs:264](../../../DailyCooking/Assets/Scripts/MVC/Controllers/CounterControllers/CookingToolCounterController.cs) `InteractAlternateEvent`: bỏ gọi
   `Cut()` (cắt chỉ còn qua gesture). Grep tutorial/hint xem còn nhắc "bấm nút để cắt" không.
4. `ConsumeInputsAndSpawnOutput` (`:651`) và nhánh cắt (`:477`): kiểm `KitchenGameManager.CanSpawn(output)`
   (SO có trong `kitchenObjectSOList`, có prefab) **trước** khi `DestroySelf` input; không spawn được →
   `Debug.LogError`, giữ input, về `Idle`, reset đồng hồ.
5. Tách `GameManager.ServerDropHeldFood(PlayerStateMachine avatar)` từ `Session_OnSeatExpired`
   ([GameManager.Session.cs:45](../../../DailyCooking/Assets/Scripts/Managers/GameManager.Session.cs)). `PlayerStateMachine.OnNetworkDespawn`: nếu
   `IsServer && !NetworkManager.ShutdownInProgress` và đang cầm đồ → gọi hàm đó.
6. [CloseUpGestureController.cs:166-198](../../../DailyCooking/Assets/Scripts/Counters/Gestures/CloseUpGestureController.cs): tool manual-start → mức rót
   (cả thanh hiển thị lẫn giá trị báo lên) lấy từ `tool.CookFraction`, không từ lúc client bắt đầu giữ.

**Test** `CookStepRecorderTests`: NaN/Inf/quá cận bị từ chối ở swipe/flip/pour; flip NaN không mở
đường nhận lần 2; stir: prompt cũ (`current-2`) bị từ chối, `current-1` nhận, `expected` bị từ chối.

**Checklist tay:**
- [ ] Cắt, lật, khuấy, rót đúng nhịp → vẫn 3 sao như trước (không chặn nhầm người chơi thật).
- [ ] Blender: bấm start trên HUD, chờ 1 s rồi giữ rót → điểm theo đúng mức hiển thị.
- [ ] Cắt bằng nút alternate (PC) không còn tác dụng.

---

## B8 — Grid · `fix(grid)` · P2

| # | Bug | Kiểm |
|---|---|---|
| 8.1 | Nhặt counter có dụng cụ rỗng trên đó → dụng cụ lơ lửng, load lại thì mất, không hoàn kho (D6) | ◐ |
| 8.2 | Server từ chối pick-up nhưng client đã vào placement → đặt bản khác từ kho / alert | ✔ |

**Sửa:**
1. [CookingToolCounterController.cs:82](../../../DailyCooking/Assets/Scripts/MVC/Controllers/CounterControllers/CookingToolCounterController.cs) `CanRemove`: false khi có tool trên counter
   (không chỉ khi tool chứa đồ). Server alert "Hãy cất dụng cụ trên quầy trước" (thêm lý do tuỳ chọn vào
   `IPlaceable`, mặc định giữ "Item is used, cannot remove.").
2. [GridBuildingSystem.cs:347](../../../DailyCooking/Assets/Scripts/GridBuildingSystem/GridSystem/GridBuildingSystem.cs) `SpawnSavedObjects`: 2 lượt (counter/đồ thường trước,
   tool sau; nhận diện tool theo đúng điều kiện ở `GridObject.cs:151-171`). Tool không có counter bên
   dưới → `LogWarning`, xoá entry khỏi `GridData`, **hoàn về inventory** (sửa save đã hỏng).
3. [BuildingPlacementManager.cs:155](../../../DailyCooking/Assets/Scripts/GridBuildingSystem/GridSystem/BuildingPlacementManager.cs) `HandleExistingObjectInteraction`: chỉ vào
   placement khi server xác nhận: `PickUpPlacedObjectServerRpc` trả `PickUpResultRpc(bool ok)` về đúng
   client; `ok` mới `SetPlacedObjectTypeSO` + ẩn inventory.

**Test** `GridPlacementTests`: hàm sắp thứ tự spawn (tool sau cùng); tool mồ côi được báo để hoàn kho.

**Checklist tay:**
- [ ] Bếp có chảo rỗng → nhặt bếp: bị chặn, có thông báo; cất chảo trước → nhặt được.
- [ ] Bàn có khách vừa đặt chỗ → nhặt bàn: bị từ chối, client không kẹt ghost.
- [ ] Save có chảo mồ côi (tạo bằng code cũ) → load: chảo về kho, có warning.

---

## B9 — Docs · `docs:`

- `docs/dataflow.md`: option menu theo GUID + offer phía server; upgrade Skill cả quán (stats tính
  lại); IAP giữ order chờ host; daily-free mirror ngày reset; hàng đợi hộp Yes/No.
- `docs/deferred.md`: thêm P0 "xác thực player id bằng token UGS"; thêm checklist IAP trên thiết bị;
  đánh dấu các mục đã xong của đợt này.
- `docs/project_overview.md`: chỉ sửa nếu kiến trúc mô tả trong đó bị đổi (upgrade, option menu).

## Ngoài phạm vi

Các mục đã có trong `docs/deferred.md` (movement chưa validate, rewarded ad chưa xác thực server,
receipt validation, `ConfigShop.GetById`: đã kiểm, chỉ có thể stale trong Editor nên giữ P2).
