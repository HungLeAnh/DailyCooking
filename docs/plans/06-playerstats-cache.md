# 06 — Cache `PlayerStats` trong `PlayerStateMachine.HandleMovement`

| | |
|---|---|
| **Nhóm** | A. Mạng / tối ưu (đã hoãn) |
| **Nguồn** | `docs/optimization_plan.md` §3.6 |
| **Trạng thái** | ⚠️ **Phần allocation đã xong** (commit `288f03fd6`); phần còn lại là micro |
| **Rủi ro** | Thấp |
| **Ưu tiên** | Thấp — làm xong cho trọn hạng, hoặc bỏ |

---

## Mục tiêu

Bỏ chuỗi singleton + quét list mỗi frame khi đọc stats của player đang điều khiển.

## Hiện trạng — đã điều tra, tốt hơn dự kiến

### 1. Allocation đã được xử lý

`GameData.GetPlayerStatsById` (commit `288f03fd6`) đã bỏ closure, giờ là vòng `for` chỉ mục:

```csharp
// Linear scan without a closure: this runs every frame from PlayerStateMachine.HandleMovement.
List<PlayerStats> stats = PlayersStats;
for (int i = 0; i < stats.Count; i++) { ... }
```

→ **Không còn GC alloc.**

### 2. Chạy 1 lần/frame, không phải mỗi avatar

`PlayerStateMachine.cs:120-128`:

```csharp
private void Update() {
    if (!IsOwner) return;        // avatar của người khác return ngay
    _stateManager.Update();
    HandleMovement();
    HandleInteractions();
}
```

→ chỉ local player 1 instance/frame.

### 3. `GetOwnerPlayerId()` không alloc khi là owner

`PlayerStateMachine.cs:438-445`:

```csharp
if (IsOwner && SessionManager.Instance != null)
    return SessionManager.Instance.PlayerId;   // => AuthenticationService.Instance.PlayerId (string có sẵn)
```

Nhánh `MultiplayerManager...playerId.ToString()` chỉ chạy khi **không** phải owner — mà nhánh đó
bị `Update()` chặn từ đầu.

### 4. Quét list

≤ 4 phần tử (`MAX_PLAYER_AMOUNT = 4`). 4 lần so chuỗi `PlayerId` mỗi frame = không đáng kể.

### Kết luận

**Không còn vấn đề allocation.** Thứ duy nhất còn lại là chuỗi đọc:

`PlayerStateMachine.cs:447-453`
```csharp
public PlayerStats GetOwnerStats() {
    GameData gameData = GameManager.Instance != null ? GameManager.Instance.GameData : null;
    string playerId = GetOwnerPlayerId();
    if (gameData == null || string.IsNullOrEmpty(playerId)) return null;
    return gameData.GetPlayerStatsById(playerId);
}
```

2 lần đọc static property + 1 lần quét ≤4 phần tử, mỗi frame.

## Thay đổi đề xuất

### Lựa chọn 1 — Cache reference (nếu vẫn muốn làm)

Thêm field vào `PlayerStateMachine`:

```csharp
private PlayerStats _cachedOwnerStats;

public PlayerStats GetOwnerStats() {
    if (_cachedOwnerStats != null) return _cachedOwnerStats;
    ... code hiện tại ...
    _cachedOwnerStats = result;
    return result;
}
```

Hủy cache tại 2 chỗ class đã có sẵn hook:
- `subscribedGameData` / `UnsubscribeFromOwnerStats()` (`:392-399`)
- `GameData_OnPlayerStatsAdded` (đang subscribe `OnPlayerStatsAdded`)

> ⚠️ `PlayerStats` là object C# thuần, `MoveSpeed` đọc qua property → nếu stats **thay đổi trên
> client khác**, cache reference vẫn đọc đúng (vì đọc field của cùng object), chỉ sai khi object
> bị **thay thế** (Xoá rồi tạo lại). Cần xác nhận `OnPlayerStatsRemoved`/`TryAddPlayerStats` có
> replace object không. `TryAddPlayerStats` ở `GameData` tạo `new PlayerStats(...)` → replace
> thật → **phinvalidate cache khi stats bị remove**.

### Lựa chọn 2 — Không làm (khuyến nghị)

4 phép tính mỗi frame không đáng để đánh đổi rủi ro stale reference. Chưa có profiler chứng minh
`GetOwnerStats` hiện trong top.

## Ảnh hưởng

| Hạng mục | Ảnh hưởng |
|---|---|
| GC alloc | Đã = 0 trước khi làm; không đổi. |
| CPU | Bỏ ~4-6 phép đọc/so sánh mỗi frame. Không đo được bằng mắt. |
| Rủi ro | Thấp, nhưng **stale `PlayerStats`** nếu cache không invalidate đúng chỗ. |
| Đụng save / mạng / prefab? | Không. |

## Kiểm chứng

1. Compile Android + 49/49 test.
2. Nếu làm lựa chọn 1, test: player disconnect → `PlayerStats` bị remove → avatar còn lại vẫn đọc
   đúng `MoveSpeed` (không giữ object cũ).
3. Profiler trước/sau: `PlayerStateMachine.Update` — nếu không thấy khác biệt thì revert.

## Rủi ro / rollback

- Stale stats (đi chậm/nhanh sai sau khi có người join/leave).
- Rollback: một commit; revert xong là về hiện trạng đã hoạt động tốt.
