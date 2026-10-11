# 11 — Chu kỳ tham chiếu `KitchenObjectSO` ↔ prefab

| | |
|---|---|
| **Nhóm** | C. Kiến trúc cần chốt |
| **Nguồn** | `docs/datamanagement.md` Tier 2 #2 |
| **Trạng thái** | Chưa làm — **đã điều tra, chỉ có 1 cặp thật sự** |
| **Rủi ro** | Trung bình (phải đổi đường spawn) |
| **Ưu tiên** | Chưa cần, **trừ khi làm Addressables (mục Tier 3)** |

---

## Mục tiêu

Chỉ ra đúng những cặp asset tham chiếu lẫn nhau, và quyết định có phá vòng hay không.

## Hiện trạng — đã rà toàn bộ, chỉ 1 vòng thật

### ✅ `PlacedObjectTypeSO` ↔ prefab — **KHÔNG có vòng**

- `PlacedObjectTypeSO.prefab` → prefab (SO trỏ prefab).
- `PlacedObjectView.cs:15` `private PlacedObjectTypeSO placedObjectTypeSO;`
  → **không có `[SerializeField]`**, chỉ gán lúc chạy qua GUID:

```csharp
// PlacedObjectView.cs:25 và :97
placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(placedObjectTypeSOGuid.Value.ToString());
public void Intialize(string placedObjectTypeSOGuid, ...) { this.placedObjectTypeSOGuid.Value = placedObjectTypeSOGuid; }
```

→ Đường đặt đồ đã sạch từ trước. **Không cần đụng.**

### ❌ `KitchenObjectSO` ↔ kitchen object prefab — **CÓ vòng**

```
KitchenObjectSO.prefab  ──(Transform, public field)──►  prefab
      ▲                                                     │
      └────────── [SerializeField] kitchenObjectSO ─────────┘
                   (KitchenObject.cs:27, nằm trên prefab)
```

Bằng chứng vòng là **bắt buộc**, không phải vô tình — `KitchenGameManager.cs:167` spawn xong
**không hề gán lại**:

```csharp
KitchenObject kitchenObject = Instantiate(kitchenObjectSO.prefab).GetComponent<KitchenObject>();
if (!kitchenObject.InitializeParent(kitchenObjectParent, index)) { Destroy(...); return null; }
kitchenObject.NetworkObject.Spawn(true);
```

Nghĩa là `kitchenObjectSO` trên instance **luôn lấy từ prefab đã bake**. Nếu bỏ serialize mà không
thêm bước gán → mọi `GetKitchenObjectSO()` trả `null` → lỗi dây chuyền ở
`TablewareKitchenObject.cs:70`, `CookingToolCounterController.cs:108`, `ClearCounterController`,
`FoodCompleteVisual.cs:60`.

### Các tham chiếu SO khác không tạo vòng

| Tham chiếu | Bên | Vòng? |
|---|---|---|
| `PostBox.postBoxKitchenSO` (`PostBox.cs:10`) | prefab cố định → SO (không phải SO trỏ về prefab này) | Không |
| `TablewareCounterController._tablewareKitchenObjectSO` (`:5`) | prefab → SO | Không |
| `CookingToolCounterController.acceptedToolTypes` (`:7`) | enum, không phải SO | Không |

## Thay đổi đề xuất

### Lựa chọn 1 — Giữ nguyên (khuyến nghị, nếu KHÔNG làm Addressables)

Vòng này **hoạt động đúng** trong Unity: asset tham chiếu GUID lẫn nhau là bình thường,
`Resources.UnloadUnusedAssets` xử lý được, scene load không lỗi.

- Ảnh hưởng: **0**.
- Cái mất: nếu sau này chia Addressables theo group (`Configs/` và `Prefab/` ở 2 folder khác
  nhau) thì vòng **chặn build** vì vòng tham chiếu cross-group.

### Lựa chọn 2 — Phá vòng (làm khi bắt đầu Addressables)

1. `KitchenObject.cs:27` bỏ `[SerializeField]`:

```csharp
// was: [SerializeField] protected KitchenObjectSO kitchenObjectSO;
protected KitchenObjectSO kitchenObjectSO;
public void SetKitchenObjectSO(KitchenObjectSO so) => kitchenObjectSO = so;
```

2. `KitchenGameManager.cs:167` gán ngay sau `Instantiate`:

```csharp
KitchenObject kitchenObject = Instantiate(kitchenObjectSO.prefab).GetComponent<KitchenObject>();
kitchenObject.SetKitchenObjectSO(kitchenObjectSO);
```

3. **Bắt buộc audit toàn bộ chỗ đọc** để chắc không có prefab nào đang bake sai SO
   (khác với SO đã spawn nó — nếu có thì đó là bug đang ẩn):

```bash
# tìm mọi prefab trong Assets/Prefab có component KitchenObject với m_SerializableData kitchenObjectSO
```

4. Bắt buộc: prefab nào được spawn qua đường **không** qua `KitchenGameManager.SpawnKitchenObject`
   (nếu có) phải gán SO theo cách tương ứng.

- Ảnh hưởng: **phá được vòng**, nhưng đổi contract của `KitchenObject` — mọi đường spawn phải
  tuân thủ. Sai sót → `GetKitchenObjectSO()` null lúc runtime.

## Ảnh hưởng

| Hạng mục | Lựa chọn 1 | Lựa chọn 2 |
|---|---|---|
| Chạy hôm nay | Không đổi | Không đổi (nếu audit đủ) |
| Addressables/AssetBundle | Chặn nếu tách group | Sạch |
| Code thêm | 0 | ~6 dòng + audit toàn bộ prefab |
| Rủi ro | 0 | Trung bình — null SO lúc runtime |
| Save / mạng | Không đổi | Không đổi |

**Khuyến nghị:** ghi lại phát hiện này vào `docs/datamanagement.md`, và **hoãn** tới khi nào
chọn làm Addressables (mục Tier 3 #3) thì làm cùng lúc.

## Kiểm chứng

Nếu làm lựa chọn 2:

1. Compile Android + 49/49 test.
2. Editor script quét mọi prefab có `KitchenObject` để chắc field SO đã bỏ (không còn serialized).
3. Chạy đủ các đường spawn: lấy nguyên liệu từ container, plate từ tableware counter, dụng cụ
   từ cooking tool, phần thưởng từ shop/PostBox.
4. Kiểm tra `GetKitchenObjectSO()` != null ở:
   `TablewareKitchenObject.TryAddIngredient`, `CookingToolCounterController.InteractEvent`,
   `ClearCounterController.InteractEvent`, `FoodCompleteVisual`.
5. Late joiner thấy đúng hình ảnh món trên bàn.

## Rủi ro / rollback

- Rủi ro chính: prefab được spawn qua đường chưa được gán SO → null.
- Rollback: revert 1 commit; `KitchenObject` lại serialize như cũ.
