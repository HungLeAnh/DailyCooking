using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public class GridData
{
    [System.NonSerialized]
    public Action OnGridDataChanged;

    private int widthMin;
    private int heightMin;
    private int widthMax;
    private int heightMax;
    private float cellSize;
    private Vector3 originPosition;
    private List<GridObjectData>[,] gridArrayData;
    public int WidthMin { get => widthMin; set => widthMin = value; }
    public int HeightMin { get => heightMin; set => heightMin = value; }
    public int WidthMax { get => widthMax; set => widthMax = value; }
    public int HeightMax { get => heightMax; set => heightMax = value; }
    public float CellSize { get => cellSize; set => cellSize = value; }
    public Vector3 OriginPosition { get => originPosition; set => originPosition = value; }
    public List<GridObjectData>[,] GridArrayData { get => gridArrayData; set => gridArrayData = value; }

    public GridData() { }
    public void UpdateGridData(GridXZ<GridObject> grid)
    {
        if (grid == null) return;

        WidthMin = grid.GetWidthMin();
        HeightMin = grid.GetHeightMin();
        WidthMax = grid.GetWidthMax();
        HeightMax = grid.GetHeightMax();
        cellSize = grid.GetCellSize();
        originPosition = grid.GetOriginPosition();
        GridArrayData = new List<GridObjectData>[widthMax, heightMax];
        for (int x = 0; x < widthMax; x++)
        {
            for (int z = 0; z < heightMax; z++)
            {
                var gridObject = grid.GetGridObject(x, z);
                if (gridObject == null) continue;  

                foreach (var gridObjectItem in gridObject)
                {
                    var placedObjectView = gridObjectItem.GetPlacedObject();
                    if (placedObjectView == null)
                        continue;
                    if (GridArrayData[x, z] == null)
                        GridArrayData[x, z] = new List<GridObjectData>();
                    if (GridArrayData[x,z].Any(y=>y.Origin == placedObjectView.Origin && y.Dir == placedObjectView.Dir &&
                        y.PlacedObjectTypeSOGuid == placedObjectView.GetPlacedObjectTypeSOGuid()))
                        continue;


                    GridArrayData[x, z].Add(new GridObjectData(placedObjectView.GetPlacedObjectTypeSOGuid(), placedObjectView.Origin, placedObjectView.Dir,placedObjectView.InventoryTabType));
                }
            }
        }
        OnGridDataChanged?.Invoke();
    }

}
