using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using System.Linq;


public class GridXZ<TGridObject> {

    public event EventHandler<OnGridObjectChangedEventArgs> OnGridObjectChanged;
    public class OnGridObjectChangedEventArgs : EventArgs {
        public int x;
        public int z;
    }

    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private TGridObject[,] gridArray;
    private Func<GridXZ<TGridObject>, int, int, TGridObject> createGridObject;

    public GridXZ(float cellSize, Vector3 originPosition, Func<GridXZ<TGridObject>, int, int, TGridObject> createGridObject) {
        this.width = 0;
        this.height = 0;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        this.createGridObject = createGridObject;

        gridArray = new TGridObject[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int z = 0; z < gridArray.GetLength(1); z++) {
                gridArray[x, z] = createGridObject(this, x, z);
            }
        }

        ShowDebug();
    }
    public GridXZ(GridData gridData, Func<GridXZ<TGridObject>, int, int, TGridObject> createGridObject)
    {
        this.width = gridData.Width;
        this.height = gridData.Height;
        this.cellSize = gridData.CellSize;
        this.originPosition = gridData.OriginPosition;
        this.createGridObject = createGridObject;

        gridArray = new TGridObject[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] =  createGridObject(this, x, z);
            }
        }

        if (gridData.GridArrayData == null)
        {
            return;
        }
        for (int i = 0; i < gridData.GridArrayData.Count; i++)
        {
            PlacedObjectTypeSO placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(gridData.GridArrayData[i].PlacedObjectTypeSOGuid);
            List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(gridData.GridArrayData[i].Origin, gridData.GridArrayData[i].Dir);
            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(gridData.GridArrayData[i].Dir);
            Vector3 placedObjectWorldPosition = this.GetWorldPosition(gridData.GridArrayData[i].Origin) +
                new Vector3(rotationOffset.x, 0, rotationOffset.y) * this.GetCellSize();
            PlacedObjectView placedObject = PlacedObjectFactory.Create(placedObjectWorldPosition, gridData.GridArrayData[i].Origin, gridData.GridArrayData[i].Dir, placedObjectTypeSO);

            foreach (var gridPosition in gridPositionList)
            {
                var gridObject = this.GetGridObject(gridPosition.x, gridPosition.y) as GridObject;
                gridObject.SetPlacedObject(placedObject);

            }
        }
        /*//for (int x = 0; x < gridData.GridArrayData.GetLength(0); x++)
        //{
        //    for (int z = 0; z < gridData.GridArrayData.GetLength(1); z++)
        //    {
        //        if(gridData.GridArrayData[x, z] == null) continue;

        //        if (gridData.GridArrayData[x,z].Origin.x == x && gridData.GridArrayData[x,z].Origin.y == z)
        //        {
        //            PlacedObjectTypeSO placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(gridData.GridArrayData[x, z].PlacedObjectTypeSOGuid);
        //            List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(new Vector2Int(x, z), gridData.GridArrayData[x,z].Dir);
        //            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(gridData.GridArrayData[x, z].Dir);
        //            Vector3 placedObjectWorldPosition = this.GetWorldPosition(x, z) +
        //                new Vector3(rotationOffset.x, 0, rotationOffset.y) * this.GetCellSize();
        //            PlacedObjectView placedObject = PlacedObjectFactory.Create(placedObjectWorldPosition, new Vector2Int(x, z), gridData.GridArrayData[x, z].Dir, placedObjectTypeSO);

        //            foreach (var gridPosition in gridPositionList)
        //            {
        //                var gridObject = this.GetGridObject(gridPosition.x, gridPosition.y) as GridObject;
        //                gridObject.SetPlacedObject(placedObject);

        //            }
        //        }
        //    }
        //}*/
        ShowDebug();
    }
    public void AddGridObjectData(List<GridObjectData> gridObjectDataList)
    {
        if (gridObjectDataList == null) return;
        if (gridObjectDataList.Count <= 0) return;

        for (int i = 0; i < gridObjectDataList.Count; i++)
        {
            PlacedObjectTypeSO placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(gridObjectDataList[i].PlacedObjectTypeSOGuid);
            List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(gridObjectDataList[i].Origin, gridObjectDataList[i].Dir);
            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(gridObjectDataList[i].Dir);
            Vector3 placedObjectWorldPosition = this.GetWorldPosition(gridObjectDataList[i].Origin) +
                new Vector3(rotationOffset.x, 0, rotationOffset.y) * this.GetCellSize();

            PlacedObjectView placedObject = PlacedObjectFactory.Create(placedObjectWorldPosition, gridObjectDataList[i].Origin, gridObjectDataList[i].Dir, placedObjectTypeSO);

            foreach (var gridPosition in gridPositionList)
            {
                var gridObject = this.GetGridObject(gridPosition.x, gridPosition.y) as GridObject;
                gridObject.SetPlacedObject(placedObject);

            }
        }
    }
    private void ShowDebug()
    {
        bool showDebug = false;
        if (showDebug)
        {
            TextMesh[,] debugTextArray = new TextMesh[width, height];

            for (int x = 0; x < gridArray.GetLength(0); x++)
            {
                for (int z = 0; z < gridArray.GetLength(1); z++)
                {
                    debugTextArray[x, z] = UtilsClass.CreateWorldText(gridArray[x, z]?.ToString(), null, GetWorldPosition(x, z) + new Vector3(cellSize, 0, cellSize) * .5f, 15, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), Color.white, 100f);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), Color.white, 100f);
                }
            }
            Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
            Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

            OnGridObjectChanged += (object sender, OnGridObjectChangedEventArgs eventArgs) =>
            {
                debugTextArray[eventArgs.x, eventArgs.z].text = gridArray[eventArgs.x, eventArgs.z]?.ToString();
            };
        }
    }
    public int GetWidth() 
    {
        return width;
    }

    public int GetHeight() 
    {
        return height;
    }

    public float GetCellSize() 
    {
        return cellSize;
    }
    public Vector3 GetOriginPosition()
    {
        return originPosition;
    }
    public Vector3 GetWorldPosition(int x, int z) 
    {
        return new Vector3(x, 0, z) * cellSize + originPosition;
    }    
    public Vector3 GetWorldPosition(Vector2Int position) 
    {
        return new Vector3(position.x, 0, position.y) * cellSize + originPosition;
    }

    public void GetXZ(Vector3 worldPosition, out int x, out int z) 
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
        if (x < 0)
            x = 0;       
        if (x >= width)
            x = width-1;       
        if (z < 0)
            z = 0;
        if (z >= height)
            z = height-1;
        
    }

    public void SetGridObject(int x, int z, TGridObject value) 
    {
        if (x >= 0 && z >= 0 && x < width && z < height) {
            gridArray[x, z] = value;
            TriggerGridObjectChanged(x, z);
        }
    }

    public void TriggerGridObjectChanged(int x, int z) 
    {
        OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, z = z });
    }

    public void SetGridObject(Vector3 worldPosition, TGridObject value) 
    {
        GetXZ(worldPosition, out int x, out int z);
        SetGridObject(x, z, value);
    }

    public TGridObject GetGridObject(int x, int z) 
    {
        if (x >= 0 && z >= 0 && x < width && z < height) {
            return gridArray[x, z];
        } else {
            return default(TGridObject);
        }
    }    
    public TGridObject GetGridObject(Vector2Int pos) 
    {
        if (pos.x >= 0 && pos.y >= 0 && pos.x < width && pos.y < height) {
            return gridArray[pos.x, pos.y];
        } else {
            return default(TGridObject);
        }
    }

    public TGridObject GetGridObject(Vector3 worldPosition) 
    {
        int x, z;
        GetXZ(worldPosition, out x, out z);
        return GetGridObject(x, z);
    }

    public Vector2Int ValidateGridPosition(Vector2Int gridPosition) 
    {
        return new Vector2Int(
            Mathf.Clamp(gridPosition.x, 0, width - 1),
            Mathf.Clamp(gridPosition.y, 0, height - 1)
        );
    }

    public void UnlockGrid(int width, int height)
    {
        this.width = width;
        this.height = height;

        gridArray = Resize2DArray(gridArray, width, height);
        ShowDebug();
    }
    public  TGridObject[,] Resize2DArray(TGridObject[,] original, int newRows, int newCols)
    {
        var newArray = new TGridObject[newRows, newCols];
        int rowsToCopy = Math.Min(original.GetLength(0), newRows);
        int colsToCopy = Math.Min(original.GetLength(1), newCols);

        for (int i = 0; i < newRows; i++)
        {
            for (int j = 0; j < newCols; j++)
            {
                if(i < rowsToCopy && j < colsToCopy)
                {
                    newArray[i, j] = original[i, j];

                }
                else
                {
                    newArray[i, j] = this.createGridObject(this, i, j);
                }

            }
        }


        return newArray;
    }
}
[Serializable]
public class GridData
{
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
    public void SaveGridData(GridXZ<GridObject> grid)
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
    }

}
[Serializable]
public class GridObjectData
{
    private string _placedObjectTypeSOGuid;
    private Vector2Int origin;
    private Dir dir;
    public Dir Dir { get => dir; set => dir = value; }
    public Vector2Int Origin { get => origin; set => origin = value; }
    public string PlacedObjectTypeSOGuid { get => _placedObjectTypeSOGuid; set => _placedObjectTypeSOGuid = value; }
    public GridObjectData(string placedObjectTypeSOGuid, Vector2Int origin, Dir dir)
    {
        _placedObjectTypeSOGuid = placedObjectTypeSOGuid;
        this.origin = origin;
        this.dir = dir;
    }

}