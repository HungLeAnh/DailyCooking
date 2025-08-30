using UnityEngine;
using Unity.Mathematics;

public interface IGridManager
{
    GridXZ<GridObject> Grid { get; }
    Vector3 GetWorldPosition(int x, int z);
    void GetXZ(Vector3 worldPosition, out int x, out int z);
    Vector2Int ValidateGridPosition(Vector2Int gridPosition);
    int GetWidth();
    int GetHeight();
    float GetCellSize();
    void UnlockGrid(int width, int height);
    void AddGridObjectData(System.Collections.Generic.List<GridObjectData> gridObjectDataList);
}
