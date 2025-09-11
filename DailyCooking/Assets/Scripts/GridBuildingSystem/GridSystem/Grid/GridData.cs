using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public class GridData
{
    public Action OnGridDataChanged;

    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private List<GridObjectData> gridArrayData;
    public int Width { get => width; set => width = value; }
    public int Height { get => height; set => height = value; }
    public float CellSize { get => cellSize; set => cellSize = value; }
    public Vector3 OriginPosition { get => originPosition; set => originPosition = value; }
    public List<GridObjectData> GridArrayData { get => gridArrayData; set => gridArrayData = value; }
    public GridData() { }
    public void UpdateGridData(GridXZ<GridObject> grid)
    {
        if (grid == null) return;
        width = grid.GetWidth();
        height = grid.GetHeight();
        cellSize = grid.GetCellSize();
        originPosition = grid.GetOriginPosition();
        GridArrayData = new List<GridObjectData>();
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var gridObject = grid.GetGridObject(x, z);
                if (gridObject == null) continue;  

                var model = gridObject.GetPlacedObject()?.GetModel();
                if (model == null || 
                    GridArrayData.Any(x=>x.Origin == model.Origin && 
                        x.PlacedObjectTypeSOGuid == model.GetPlacedObjectTypeSOGuid())) 
                    continue;
                GridArrayData.Add(new GridObjectData(model.GetPlacedObjectTypeSOGuid(), model.Origin, model.Dir));
            }
        }
        OnGridDataChanged?.Invoke();
    }

}
