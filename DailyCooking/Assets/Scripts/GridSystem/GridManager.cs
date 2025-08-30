using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

public class GridManager : IGridManager
{
    private GridXZ<GridObject> grid;

    public GridXZ<GridObject> Grid => grid;

    public GridManager(float cellSize, Vector3 originPosition, System.Func<GridXZ<GridObject>, int, int, GridObject> createGridObject)
    {
        grid = new GridXZ<GridObject>(cellSize, originPosition, createGridObject);
    }

    public GridManager(GridData gridData, System.Func<GridXZ<GridObject>, int, int, GridObject> createGridObject)
    {
        grid = new GridXZ<GridObject>(gridData, createGridObject);
    }

    public Vector3 GetWorldPosition(int x, int z)
    {
        return grid.GetWorldPosition(x, z);
    }

    public void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        grid.GetXZ(worldPosition, out x, out z);
    }

    public Vector2Int ValidateGridPosition(Vector2Int gridPosition)
    {
        return grid.ValidateGridPosition(gridPosition);
    }

    public int GetWidth()
    {
        return grid.GetWidth();
    }

    public int GetHeight()
    {
        return grid.GetHeight();
    }

    public float GetCellSize()
    {
        return grid.GetCellSize();
    }

    public void UnlockGrid(int width, int height)
    {
        grid.UnlockGrid(width, height);
    }

    public void AddGridObjectData(List<GridObjectData> gridObjectDataList)
    {
        grid.AddGridObjectData(gridObjectDataList);
    }
}
