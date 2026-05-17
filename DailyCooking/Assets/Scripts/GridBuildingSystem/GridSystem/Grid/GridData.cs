using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

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

    public GridData() 
    {
         
    }
    public void Initialize()
    {
        GridBuildingSystem.Instance.GridManager.Grid.OnGridObjectChanged += Grid_OnGridObjectChanged;
        GridBuildingSystem.Instance.GridManager.Grid.OnGridSizeChanged += Grid_OnGridSizeChanged;
    }

    private void Grid_OnGridSizeChanged()
    {
        var grid = GridBuildingSystem.Instance.GridManager.Grid;
        if (grid == null) return;
     
        WidthMin = grid.GetWidthMin();
        HeightMin = grid.GetHeightMin();
        WidthMax = grid.GetWidthMax();
        HeightMax = grid.GetHeightMax();
        cellSize = grid.GetCellSize();
        originPosition = grid.GetOriginPosition();
        Resize2DArray(GridArrayData, widthMax, heightMax);
    }

    private void Grid_OnGridObjectChanged(object sender, GridXZ<GridObject>.OnGridObjectChangedEventArgs e)
    {
        var gridObject = GridBuildingSystem.Instance.GridManager.Grid.GetGridObject(e.x, e.z);
        if (gridObject == null) return;
        foreach (var gridObjectItem in gridObject)
        {
            var placedObjectView = gridObjectItem.GetPlacedObject();
            if (placedObjectView == null)
                continue;
            if(GridArrayData == null)
                GridArrayData = new List<GridObjectData>[widthMax, heightMax];
            if (GridArrayData[e.x, e.z] == null)
                GridArrayData[e.x, e.z] = new List<GridObjectData>();
            if (GridArrayData[e.x, e.z].Any(y => y.Origin == placedObjectView.Origin && y.Dir == placedObjectView.Dir &&
                y.PlacedObjectTypeSOGuid == placedObjectView.GetPlacedObjectTypeSOGuid()))
                continue;

            GridArrayData[e.x, e.z].Add(new GridObjectData(placedObjectView.GetPlacedObjectTypeSOGuid(), placedObjectView.Origin, placedObjectView.Dir, placedObjectView.InventoryTabType));
        }
        OnGridDataChanged?.Invoke();
    }
    public void ChangeGridObjectData(int x, int z, GridObjectData newData, string guid)
    {
        if (x < 0 || x >= widthMax || z < 0 || z >= heightMax)
            return;

        var gridObjectDatas = GridArrayData[x, z];
        if (gridObjectDatas == null)
        {
            GridArrayData[x, z] = new List<GridObjectData>();
            gridObjectDatas = GridArrayData[x, z];
            gridObjectDatas.Add(newData);
            OnGridDataChanged?.Invoke();
            return;
        }
        else
        {
            for (int i = 0; i < gridObjectDatas.Count; i++)
            {
                if (gridObjectDatas[i].PlacedObjectTypeSOGuid == guid)
                {
                    gridObjectDatas[i] = newData;
                    OnGridDataChanged?.Invoke();
                    return;
                }
            }

        }
    }
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
    public List<GridObjectData>[,] Resize2DArray(List<GridObjectData>[,] original, int newRows, int newCols)
    {
        var newArray = new List<GridObjectData>[widthMax, heightMax];
        int rowsToCopy = 0;
        int colsToCopy = 0;
        if (original != null)
        {
            rowsToCopy = Math.Min(original.GetLength(0), newRows);
            colsToCopy = Math.Min(original.GetLength(1), newCols);
            for (int i = 0; i < newRows; i++)
            {
                for (int j = 0; j < newCols; j++)
                {
                    if (i < rowsToCopy && j < colsToCopy)
                    {
                        newArray[i, j] = original[i, j];

                    }
                    else
                    {
                        newArray[i, j] = new List<GridObjectData>();
                    }

                }
            }
        }
        else
        {
            rowsToCopy = newRows;
            colsToCopy = newCols;
            for (int i = 0; i < newRows; i++)
            {
                for (int j = 0; j < newCols; j++)
                {
                    newArray[i, j] = new List<GridObjectData>();
                }
            }
        }

        return newArray;
    }
    public List<GridObjectData> GetGridObjectDatas(int x, int z)
    {
        if (x < 0 || x >= widthMax || z < 0 || z >= heightMax)
            return null;
        return GridArrayData[x, z];
    }
}
