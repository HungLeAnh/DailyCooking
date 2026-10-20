# 14 — Runtime ghi thẳng vào material asset (`GridShaderMaterial.mat`)

## Hiện trạng

- `DailyCooking/Assets/Scripts/GridBuildingSystem/GridSystem/GridBuildingSystem.cs:26`

  ```csharp
  [SerializeField] private Material gridGuideMaterial;
  ```

  Field này trỏ tới `DailyCooking/Assets/Shaders/GridShaderMaterial.mat`
  (guid `3077f760661a789459e2a175cc9eac66`), dùng cho object `GridGuide`
  (MeshRenderer + plane mesh) — có trong **cả** `GameScene` và `TestScene`.

- `DailyCooking/Assets/Scripts/GridBuildingSystem/GridSystem/GridVisualizer.cs:21-29`:

  ```csharp
  gridGuideMaterial.SetFloat("_GridWidthMax", gridManager.GetWidthMax() + GRID_OFFSET);
  ...
  gridGuideMaterial.SetVector("_CellSize", ...);
  ```

  Đây là `Material.SetFloat/SetVector` trên **chính asset**, không phải bản sao.

`Material.SetFloat` không phải "chỉ set runtime": nó sửa object asset trong bộ nhớ và
**đánh dấu dirty**. Lần `AssetDatabase.SaveAssets()` / `Ctrl+S` / build kế tiếp sẽ ghi thẳng
xuống `.mat` trong repo.

Bằng chứng đo được:

- `.mat` đang được commit với `_GridWidthMax: 5.01`, `_GridWidthMin: 5`,
  `_GridHeightMax: 5.01`, `_GridHeightMin: 5` — đúng output của một lượt chơi grid 5x5, cell 2.
- Trong phiên sửa mục này: `EditorUtility.IsDirty(mat) = True` (từ các lượt chơi trước), gọi
  `AssetDatabase.SaveAssets()` → file đổi `5.01/5` → `0.01/0` (grid 0x0 của nhà hàng mới) →
  `git diff` ra 4 dòng. Đã `git checkout --` để trả lại nội dung cũ.

## Ảnh hưởng

- Working tree **bẩn sau mỗi lượt play**; diff trông như "designer vừa sửa material" → rất dễ
  commit nhầm.
- Material dùng chung cho hai scene: `GameScene` và `TestScene` ghi đè lẫn nhau, scene nào chạy
  sau thì giá trị của nó thắng.
- Giá trị trong asset bị nhiễm số liệu runtime nên không còn là "giá trị thiết kế".

## Thay đổi

1. `GridVisualizer`:

   ```csharp
   this.gridGuideMaterial = gridGuideMaterial != null ? new Material(gridGuideMaterial) : null;
   ApplyGridGuideMaterial();   // gán bản sao cho Renderer của gridGuideObject
   ```

   và implement `IDisposable`:

   ```csharp
   public void Dispose()
   {
       if (gridGuideMaterial == null) return;
       if (Application.isPlaying) UnityEngine.Object.Destroy(gridGuideMaterial);
       else UnityEngine.Object.DestroyImmediate(gridGuideMaterial);
       gridGuideMaterial = null;
   }
   ```

2. `GridBuildingSystem.OnDestroy()` thêm:

   ```csharp
   if (gridVisualizer is IDisposable disposableGridVisualizer)
       disposableGridVisualizer.Dispose();
   gridVisualizer = null;
   ```

Không đổi: tên field serialized `gridGuideMaterial` (scene reference giữ nguyên), bản thân
material asset, prefab/GUID, cách shader nhận property.

## Ảnh hưởng sau khi sửa

- Không còn ghi vào asset → `EditorUtility.IsDirty(mat)` luôn `false`, file `.mat` giữ nguyên
  từng byte sau khi chơi.
- Hình ảnh giống hệt: cùng shader, cùng tên property, cùng giá trị (chỉ khác là ghi lên bản sao).
- Thêm đúng **1 `Material` instance** cho mỗi `GridBuildingSystem` (vài KB), huỷ cùng scene.
- Asset giữ giá trị hiện tại (5 / 5.01) làm mặc định khi xem trong Editor — từ nay runtime không
  ghi đè lên nó nữa.

## Kiểm chứng

Đã làm:

- `refresh_unity(force)` → console **0 error / 0 warning**.
- `run_tests(EditMode)` → **49/49 pass**.
- `Tools/Build Checks/Compile Android Player Scripts` → **110 assemblies**.
- Play mode (`NewGame` + host + `GameScene` + `UnlockGrid()` + `SetActiveGridGuide(true)`):
  - `GridGuide` renderer dùng material **khác** asset (`isAsset=False`).
  - Đổi grid để giá trị runtime khác hẳn giá trị cũ trong asset:
    `GridXZ.UnlockGrid(3, 4)` rồi `SetActiveGridGuide(true)` →
    material của renderer `_GridWidthMin/_GridHeightMin = 3/4`, `_GridWidthMax/_GridHeightMax = 3.01/4.01`;
    **asset vẫn** `5/5` và `5.01/5.01`, `IsDirty = False`.
    (Lần đo đầu, grid 5x5 trùng đúng giá trị cũ trong asset nên **không phân biệt được** — phải
    đổi grid mới thành bằng chứng; đây là lý do ghi cả hai bước.)
  - Trong và sau lượt chơi: 0 error; 1 warning có sẵn `Look rotation viewing vector is zero`.
- Sau khi `stop`: SHA256 của `GridShaderMaterial.mat` = `A2F9F5F418951676BE07FF8187A28EF4B0E38688B3C55512CAA8D98A17229899`,
  **giống hệt** trước khi vào play; `git status -- DailyCooking/Assets/Shaders` sạch.

Chưa kiểm: chưa xem hình trong build Android thật (mới play mode Editor + compile script).

## Rủi ro

- Thấp, chỉ ảnh hưởng rendering.
- Nếu designer gán một material **khác** vào field `gridGuideMaterial` trong khi renderer của
  `GridGuide` đang dùng material khác nữa, code sẽ gán bản sao của **field** cho renderer → hiển
  thị theo field. Trước đây trường hợp này không có tác dụng gì; nay thì có. Chấp nhận.
- `Dispose` chạy trong `OnDestroy` với `DestroyImmediate` khi không ở play mode, để không sinh
  warning "Destroy may not be called from edit mode".
