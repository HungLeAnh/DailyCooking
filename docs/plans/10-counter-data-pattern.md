# 10 — Hai kiểu nạp dữ liệu của counter (baked SO vs resolve theo GUID)

| | |
|---|---|
| **Nhóm** | C. Kiến trúc cần chốt |
| **Nguồn** | `docs/datamanagement.md` Tier 2 #4 + §4.7 |
| **Trạng thái** | ✅ **Đã làm 2 việc nhỏ** (guard null + comment quy ước). **Không chuẩn hoá pattern** — hai pattern giữ nguyên. Compile Android + 49/49, console 0/0. Quy ước chưa dán vào `docs/datamanagement.md` §4.7. |
| **Rủi ro** | Trung bình nếu làm; **0 nếu không làm** |
| **Ưu tiên** | Thấp |

---

## Mục tiêu

Project có 2 cách counter biết "mình chứa gì":

- **A. Resolve theo GUID** — đọc `PlacedObjectView.GetPlacedObjectTypeSOGuid()` → tra dictionary.
- **B. Baked SO trên prefab** — `[SerializeField] private KitchenObjectSO`.

Câu hỏi: có nên chuẩn hoá về A không?

## Hiện trạng — bảng đầy đủ 7 counter

| Controller | Nội dung | Trạng thái mạng | Cách biết SO |
|---|---|---|---|
| `ClearCounterController` | 1 món cầm tay | object con networked | **B**: `KitchenObject.GetKitchenObjectSO()` |
| `TrashCounterController` | xoá món | — | không cần |
| `ContainerCounterController` | 1 slot nguyên liệu | `NetworkVariable<ContainerDataSerializable>` | **A**: GUID → `KitchenObjectSODic` |
| `OptionalContainerCounterController` | N slot | `NetworkList<ContainerDataSerializable>` | **A**: GUID → `KitchenObjectSODic` |
| `TablewareCounterController` | **không giới hạn**, luôn rót bát/dĩa | **không có state** | **B**: `[SerializeField] KitchenObjectSO` (`:5`) |
| `CookingToolCounterController` | chấp nhận loại công cụ | tool của `CookingTool` | enum list baked (`:7`) |
| `PostBox` | 1 kiện hàng | theo GUID kiện | **A** cho nội dung + **B** cho `postBoxKitchenSO` (`:10`) |

Hai counter dùng A **đều là container có nội dung thay đổi** — GUID cần thiết vì cùng 1 prefab
(Fridge) có thể chứa rau khác nhau tuỳ nhà hàng.

`TablewareCounterController` **không đổi nội dung** (luôn ra bát/dĩa, không giới hạn) — baked SO
đơn giản, đúng, và **không có gì để resolve**.

## Kết luận điều tra

**Hai pattern là đúng, không phải nợ kỹ thuật.** So sánh:

| | A: GUID | B: Baked |
|---|---|---|
| Nội dung thay đổi? | Cần | Không cần |
| Lỗi khi designer quên? | Log `GridObjectDatas is null!` (loud) | Im lặng (dễ sai) |
| Code | ~25 dòng/base + network | 5 dòng |
| Cycle asset SO↔prefab | Không | **Có** (xem mục 11) |

→ Chuẩn hoá `TablewareCounterController` sang A sẽ **thêm code, thêm state mạng, không thêm giá trị**.

## Thay đổi đề xuất

### Việc nên làm (2 việc nhỏ, không đổi pattern)

**1. Guard cho `TablewareCounterController` (1 dòng)**

```csharp
public override void InteractEvent(PlayerStateMachine playerStateMachine)
{
    if (!playerStateMachine.HasKitchenObject())
    {
        if (_tablewareKitchenObjectSO == null)
        {
            Debug.LogError("TablewareCounterController: _tablewareKitchenObjectSO is not assigned!", this);
            return;
        }
        KitchenObject.SpawnKitchenObject(_tablewareKitchenObjectSO, playerStateMachine);
    }
}
```

Hiện `KitchenGameManager.cs:163` đã check null và trả về `null` → người bấm im lặng, không biết
tại sao không ra bát.

**2. Ghi chú trong file**

```csharp
// Fixed-content counter: the SO is baked on the prefab on purpose.
// Counters whose contents change at runtime (fridge/refillable) must go through
// PlacedObjectView GUID + NetworkVariable — see BaseContainerCounterController.
```

### Việc KHÔNG nên làm

Chuyển `TablewareCounterController` sang GUID + `NetworkVariable<...>`:
- Thêm state mạng cho counter vốn không có state.
- Thêm đường sync để host replicate → tăng bề mặt lỗi.
- Không giảm được dòng code nào ở nơi khác.

### Nếu vẫn muốn một chuẩn chung

Chuẩn hoá ở mức **quy ước**, không phải ở mức code:

> *Counter có nội dung thay đổi → pattern A. Counter nội dung cố định → pattern B + guard null.*

Dán quy ước này vào `docs/datamanagement.md` §4.7 là đủ.

## Ảnh hưởng

| Hạng mục | Đề xuất (2 việc nhỏ) | Chuẩn hoá về A |
|---|---|---|
| Code thêm | ~7 dòng | ~40 dòng + state mạng mới |
| Rủi ro regresion | Rất thấp | Trung bình (đổi đường sync) |
| Lỗi designer phát hiện sớm | Có (log) | Có |
| Save / wire format | Không đổi | **Đổi** (cần cả 2 client cùng bản) |

## Kiểm chứng

1. Compile Android + 49/49 test.
2. Bỏ trống `_tablewareKitchenObjectSO` trên 1 prefab tạm → Play → bấm counter → Console hiện
   `TablewareCounterController: _tablewareKitchenObjectSO is not assigned!` (thay vì im lặng).
3. Gán lại → counter rót bát/dĩa bình thường.

## Rủi ro / rollback

- Đề xuất gần như không có rủi ro. Rollback 1 commit.
- Nếu làm phương án "chuẩn hoá về A": rủi ro mất đồng bộ nội dung counter giữa host/client — phải
  test 2 client + late join.
