# 18 — Warning `Look rotation viewing vector is zero`

Ngày: 2026-10-21. Mục phát sinh khi dọn warning engine (README § "Mục phát sinh thêm").

| | |
|---|---|
| **Trạng thái** | ✅ Đã sửa + đã kiểm trong play mode |
| **Rủi ro** | Rất thấp — chỉ bỏ 1 phép xoay khi không có input |
| **File** | `Assets/Scripts/Player/PlayerStateMachine.cs` (`HandleMovement`) |

## Hiện trạng

`HandleMovement()` tính `moveDir` từ input rồi **luôn** gán:

```csharp
Context.IsWalking = moveDir != Vector3.zero;

float rotateSpeed = 10f;
transform.forward = Vector3.Slerp(transform.forward, moveDir, rotateSpeed * Time.deltaTime);
```

Khi người chơi đứng yên, `moveDir == Vector3.zero`. `Vector3.Slerp(a, zero, t)` trả về
**đúng vector zero** khi `t >= 1`, tức khi `Time.deltaTime >= 0.1f` (frame dài ≥ 100 ms:
domain reload, dừng ở breakpoint, editor hitch, lúc chạy profiler). Gán
`transform.forward = (0,0,0)` đi vào `Quaternion.LookRotation` và Unity log:

```
Look rotation viewing vector is zero
```

Vì vậy warning **không phải mỗi frame** ở 60 FPS (t ≈ 0.16), mà xuất hiện theo từng cú hitch —
khớp với việc nó chỉ thấy trong các lượt đo profiler trước đây.

## Thay đổi

```csharp
if (Context.IsWalking)
{
    float rotateSpeed = 10f;
    transform.forward = Vector3.Slerp(transform.forward, moveDir, rotateSpeed * Time.deltaTime);
}
```

`Context.IsWalking` đã được tính ngay phía trên (`moveDir != Vector3.zero`) và cũng đúng cho
trường hợp bị tường chặn: `moveDir` vẫn khác zero nên nhân vật vẫn quay về hướng bị chặn như cũ.

## Ảnh hưởng

- Không đổi tốc độ quay, không đổi hướng đi, không đổi input.
- Bỏ hẳn lời gọi `LookRotation(zero)` → hết warning.
- Không đụng prefab, GUID, field serialize.

## Kiểm chứng (play mode, host đơn, 2026-10-21)

| Kiểm | Kết quả |
|---|---|
| Đứng yên 8 s trong `GameScene`, player `walk=False` | **0 warning / 0 error** |
| Gọi lại biểu thức cũ với `t = 1` trên chính player | Sinh đúng warning `Look rotation viewing vector is zero` → xác nhận cơ chế |
| `run_tests(EditMode)` | **49/49 pass** |
| `Tools/Build Checks/Compile Android Player Scripts` | **110 assemblies** |
| Console sau refresh | **0 error / 0 warning** |

Giới hạn: chưa dựng lại được một cú hitch ≥ 100 ms **trong lúc đi** để thấy warning cũ bắn ra
theo đường production; phần "cơ chế" được xác nhận bằng lời gọi tương đương ở trên.
