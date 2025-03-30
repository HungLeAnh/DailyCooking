/* 
    ------------------- Code Monkey -------------------

    Thank you for downloading this package
    I hope you find it useful in your projects
    If you have any questions let me know
    Cheers!

               unitycodemonkey.com
    --------------------------------------------------
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;


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


    public GridXZ(int width, int height, float cellSize, Vector3 originPosition, Func<GridXZ<TGridObject>, int, int, TGridObject> createGridObject) {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new TGridObject[width, height];

        for (int x = 0; x < gridArray.GetLength(0); x++) {
            for (int z = 0; z < gridArray.GetLength(1); z++) {
                gridArray[x, z] = createGridObject(this, x, z);
            }
        }

        bool showDebug = true;
        if (showDebug) {
            TextMesh[,] debugTextArray = new TextMesh[width, height];

            for (int x = 0; x < gridArray.GetLength(0); x++) {
                for (int z = 0; z < gridArray.GetLength(1); z++) {
                    debugTextArray[x, z] = UtilsClass.CreateWorldText(gridArray[x, z]?.ToString(), null, GetWorldPosition(x, z) + new Vector3(cellSize, 0, cellSize) * .5f, 15, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), Color.white, 100f);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), Color.white, 100f);
                }
            }
            Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 100f);
            Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 100f);

            OnGridObjectChanged += (object sender, OnGridObjectChangedEventArgs eventArgs) => {
                debugTextArray[eventArgs.x, eventArgs.z].text = gridArray[eventArgs.x, eventArgs.z]?.ToString();
            };
        }
    }
    public GridXZ(GridData gridData, Func<GridXZ<TGridObject>, int, int, TGridObject> createGridObject)
    {
        this.width = gridData.Width;
        this.height = gridData.Height;
        this.cellSize = gridData.CellSize;
        this.originPosition = gridData.OriginPosition;

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
        for (int x = 0; x < gridData.GridArrayData.GetLength(0); x++)
        {
            for (int z = 0; z < gridData.GridArrayData.GetLength(1); z++)
            {
                if(gridData.GridArrayData[x, z] == null) continue;

                if (gridData.GridArrayData[x,z].Origin.x == x && gridData.GridArrayData[x,z].Origin.y == z)
                {
                    PlacedObjectTypeSO placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(gridData.GridArrayData[x, z].PlacedObjectTypeSOGuid);
                    List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(new Vector2Int(x, z), gridData.GridArrayData[x,z].Dir);
                    Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(gridData.GridArrayData[x, z].Dir);
                    Vector3 placedObjectWorldPosition = this.GetWorldPosition(x, z) +
                        new Vector3(rotationOffset.x, 0, rotationOffset.y) * this.GetCellSize();
                    PlacedObjectView placedObject = PlacedObjectFactory.Create(placedObjectWorldPosition, new Vector2Int(x, z), gridData.GridArrayData[x, z].Dir, placedObjectTypeSO);

                    foreach (var gridPosition in gridPositionList)
                    {
                        var gridObject = this.GetGridObject(gridPosition.x, gridPosition.y) as GridObject;
                        gridObject.SetPlacedObject(placedObject);

                    }
                }
            }
        }
        bool showDebug = true;
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

            OnGridObjectChanged += (object sender, OnGridObjectChangedEventArgs eventArgs) => {
                debugTextArray[eventArgs.x, eventArgs.z].text = gridArray[eventArgs.x, eventArgs.z]?.ToString();
            };
        }
    }

    public int GetWidth() {
        return width;
    }

    public int GetHeight() {
        return height;
    }

    public float GetCellSize() {
        return cellSize;
    }
    public Vector3 GetOriginPosition()
    {
        return originPosition;
    }
    public Vector3 GetWorldPosition(int x, int z) {
        return new Vector3(x, 0, z) * cellSize + originPosition;
    }

    public void GetXZ(Vector3 worldPosition, out int x, out int z) {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
    }

    public void SetGridObject(int x, int z, TGridObject value) {
        if (x >= 0 && z >= 0 && x < width && z < height) {
            gridArray[x, z] = value;
            TriggerGridObjectChanged(x, z);
        }
    }

    public void TriggerGridObjectChanged(int x, int z) {
        OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, z = z });
    }

    public void SetGridObject(Vector3 worldPosition, TGridObject value) {
        GetXZ(worldPosition, out int x, out int z);
        SetGridObject(x, z, value);
    }

    public TGridObject GetGridObject(int x, int z) {
        if (x >= 0 && z >= 0 && x < width && z < height) {
            return gridArray[x, z];
        } else {
            return default(TGridObject);
        }
    }

    public TGridObject GetGridObject(Vector3 worldPosition) {
        int x, z;
        GetXZ(worldPosition, out x, out z);
        return GetGridObject(x, z);
    }

    public Vector2Int ValidateGridPosition(Vector2Int gridPosition) {
        return new Vector2Int(
            Mathf.Clamp(gridPosition.x, 0, width - 1),
            Mathf.Clamp(gridPosition.y, 0, height - 1)
        );
    }

}
[Serializable]
public class GridData
{
    private int width;
    private int height;
    private float cellSize;
    private Vector3 originPosition;
    private GridObjectData[,] gridArrayData;
    public int Width { get => width; set => width = value; }
    public int Height { get => height; set => height = value; }
    public float CellSize { get => cellSize; set => cellSize = value; }
    public Vector3 OriginPosition { get => originPosition; set => originPosition = value; }
    public GridObjectData[,] GridArrayData { get => gridArrayData; set => gridArrayData = value; }

    public GridData(GridXZ<GridObject> grid)
    {
        if (grid == null) return;
        width = grid.GetWidth();
        height = grid.GetHeight();
        cellSize = grid.GetCellSize();
        originPosition = grid.GetOriginPosition();
        GridArrayData = new GridObjectData[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var gridObject = grid.GetGridObject(x, z);
                if (gridObject == null) continue;  

                var model = gridObject.GetPlacedObject()?.GetModel();
                if (model == null) continue;
                GridArrayData[x, z] = new GridObjectData(model.GetPlacedObjectTypeSOGuid(), model.Origin, model.Dir);
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