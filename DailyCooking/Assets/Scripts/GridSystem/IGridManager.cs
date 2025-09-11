using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
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
    void AddGridObjectData(List<GridObjectData> gridObjectDataList);
    Vector3 GetFirstEmptyGridPos();
    int2 WorldPositionToGridPos(float x, float y);
    Vector3 GridPositionToWorldPosition(int2 int2);
    Vector2 GetGridSize();
}
