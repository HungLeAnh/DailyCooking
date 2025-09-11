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

    public Vector3 GetFirstEmptyGridPos()
    {
        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int z = 0; z < grid.GetHeight(); z++)
            {
                if (grid.GetGridObject(x, z).CanBuild())
                {
                    return grid.GetWorldPosition(x, z);
                }
            }
        }
        return Vector3.zero;
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
        return new Vector2Int(grid.GetWidth(), grid.GetHeight());
    }
}