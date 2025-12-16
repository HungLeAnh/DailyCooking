using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

public class GridManager : IGridManager
{
    private GridXZ<GridObject> grid;

    public GridXZ<GridObject> Grid => grid;

    public GridManager(int width, int height, float cellSize, Vector3 originPosition, 
        System.Func<GridXZ<GridObject>, int, int, List<GridObject>> createGridObject)
    {
        grid = new GridXZ<GridObject>(width, height, cellSize, originPosition, createGridObject);
    }

    public GridManager(GridData gridData, 
        System.Func<GridXZ<GridObject>, int, int, List<GridObject>> createGridObject)
    {
        grid = new GridXZ<GridObject>(gridData, createGridObject);
        GridObjectSpawner.SpawnObjectsFromData(grid, gridData.GridArrayData);
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

    public int GetWidthMax()
    {
        return grid.GetWidthMax();
    }
    public int GetWidthMin()
    {
        return grid.GetWidthMin();
    }
    public int GetHeightMax()
    {
        return grid.GetHeightMax();
    }
    public int GetHeightMin()
    {
        return grid.GetHeightMin();
    }

    public float GetCellSize()
    {
        return grid.GetCellSize();
    }

    public void UnlockGrid(int width, int height)
    { 
        grid.UnlockGrid(width, height);
    }
    public void ExpandGrid() 
    {
        grid.Expand();
    }

    public void AddGridObjectData(List<GridObjectData>[,] gridObjectDataList)
    {
        GridObjectSpawner.SpawnObjectsFromData(grid, gridObjectDataList);
    }

    public int2 WorldPositionToGridPos(float x, float y)
    {
        if(grid.ValidateGridPosition(new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y))) != null)
        {
            grid.GetXZ(new Vector3(Mathf.RoundToInt(x), 0, Mathf.RoundToInt(y)), out int xPos, out int yPos);
            return new int2(xPos, yPos);
        }
        return  new int2(-int.MaxValue, -int.MaxValue);
    }

    public Vector3 GridPositionToWorldPosition(int2 int2)
    {
        if (grid.ValidateGridPosition(new Vector2Int(int2.x, int2.y)) != null)
        {
            return grid.GetWorldPosition(int2.x, int2.y);
        }
        return Vector3.negativeInfinity;
    }

    public Vector2 GetGridSize()
    {
        return new Vector2Int(grid.GetWidthMax(), grid.GetHeightMax());
    }
}
