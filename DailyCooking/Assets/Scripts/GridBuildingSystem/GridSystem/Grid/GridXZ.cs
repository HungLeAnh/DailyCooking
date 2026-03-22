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
    private int widthMin;
    private int heightMin;
    private int widthMax;
    private int heightMax;
    private float cellSize;
    private Vector3 originPosition;
    private List<TGridObject>[,] gridArray;
    private Func<GridXZ<TGridObject>, int, int, List<TGridObject>> createGridObject;

    public GridXZ(int width, int height, float cellSize, Vector3 originPosition, 
        Func<GridXZ<TGridObject>, int, int, List<TGridObject>> createGridObject)
    {
        this.widthMin = 0;
        this.heightMin = 0;
        this.widthMax = width;
        this.heightMax = height;
        this.cellSize = cellSize;
        this.originPosition = originPosition;
        this.createGridObject = createGridObject;
        gridArray = new List<TGridObject>[width, height];
        ShowDebug();
    }
    public GridXZ(GridData gridData, Func<GridXZ<TGridObject>, int, int, List<TGridObject>> createGridObject)
    {
        this.widthMin = gridData.WidthMin;
        this.heightMin = gridData.HeightMin;
        this.widthMax = gridData.WidthMax;
        this.heightMax = gridData.HeightMax;
        this.cellSize = gridData.CellSize;
        this.originPosition = gridData.OriginPosition;
        this.createGridObject = createGridObject;

        gridArray = new List<TGridObject>[Math.Max(widthMin,widthMax), Math.Max(heightMin,heightMax)];

        for (int x = 0; x < gridArray.GetLength(0); x++)
        {
            for (int z = 0; z < gridArray.GetLength(1); z++)
            {
                gridArray[x, z] =  createGridObject(this, x, z);
            }
        }
        ShowDebug();
    }
    
    private void ShowDebug()
    {
        bool showDebug = false;
        if (showDebug)
        {
            TextMesh[,] debugTextArray = new TextMesh[widthMax, heightMax];

            for (int x = 0; x < gridArray.GetLength(0); x++)
            {
                for (int z = 0; z < gridArray.GetLength(1); z++)
                {
                    debugTextArray[x, z] = UtilsClass.CreateWorldText(gridArray[x, z]?.ToString(), null, GetWorldPosition(x, z) + new Vector3(cellSize, 0, cellSize) * .5f, 15, Color.white, TextAnchor.MiddleCenter, TextAlignment.Center);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), Color.white, 100f);
                    Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), Color.white, 100f);
                }
            }
            Debug.DrawLine(GetWorldPosition(0, heightMax), GetWorldPosition(widthMax, heightMax), Color.white, 100f);
            Debug.DrawLine(GetWorldPosition(widthMax, 0), GetWorldPosition(widthMax, heightMax), Color.white, 100f);

            OnGridObjectChanged += (object sender, OnGridObjectChangedEventArgs eventArgs) =>
            {
                debugTextArray[eventArgs.x, eventArgs.z].text = gridArray[eventArgs.x, eventArgs.z]?.ToString();
            };
        }
    }
    public int GetWidthMin()
    {
        return widthMin;
    }
    public int GetHeightMin()
    {
        return heightMin;
    }
    public int GetWidthMax()
    {
        return widthMax;
    }
    public int GetHeightMax()
    {
        return heightMax;
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
        x = Mathf.RoundToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.RoundToInt((worldPosition - originPosition).z / cellSize);
        if (x < 0)
            x = 0;       
        if (x >= widthMax)
            x = widthMax-1;       
        if (z < 0)
            z = 0;
        if (z >= heightMax)
            z = heightMax-1;
        
    }

    public void TriggerGridObjectChanged(int x, int z) 
    {
        OnGridObjectChanged?.Invoke(this, new OnGridObjectChangedEventArgs { x = x, z = z });
    }
    public void AddGridObjectData(int x, int z, TGridObject gridObject)
    {
        if (x >= 0 && z >= 0 && x < widthMax && z < heightMax) 
        {
            if(gridArray[x, z] == null)
            {
                gridArray[x, z] = new List<TGridObject>();
            }
            gridArray[x, z].Add(gridObject);
            //TriggerGridObjectChanged(x, z);
        }
    }

    public List<TGridObject> GetGridObject(int x, int z) 
    {
        if (x >= 0 && z >= 0 && x < widthMax && z < heightMax) {
            return gridArray[x, z];
        } else {
            return default(List<TGridObject>);
        }
    }    
    public List<TGridObject> GetGridObject(Vector2Int pos) 
    {
        if (pos.x >= 0 && pos.y >= 0 && pos.x < widthMax && pos.y < heightMax) {
            return gridArray[pos.x, pos.y];
        } else {
            return default(List<TGridObject>);
        }
    }

    public List<TGridObject> GetGridObject(Vector3 worldPosition) 
    {
        int x, z;
        GetXZ(worldPosition, out x, out z);
        return GetGridObject(x, z);
    }

    public Vector2Int ValidateGridPosition(Vector2Int gridPosition) 
    {
        return new Vector2Int(
            Mathf.Clamp(gridPosition.x, 0, widthMax - 1),
            Mathf.Clamp(gridPosition.y, 0, heightMax - 1)
        );
    }

    public void UnlockGrid(int width, int height)
    {
        this.widthMin = width;
        this.heightMin = height;
        this.widthMax = width;
        this.heightMax = height;

        gridArray = Resize2DArray(gridArray, width, height);
        ShowDebug();
    }
    public  List<TGridObject>[,] Resize2DArray(List<TGridObject>[,] original, int newRows, int newCols)
    {
        var newArray = new List<TGridObject>[newRows, newCols];
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

    public void Expand()
    {
        if (heightMax == widthMax)
        {
            if (widthMin < widthMax)
            {
                widthMin += 5;
            }
            else
            {
                widthMax += 5;
                heightMin = 5;
            }
        }
        else if (heightMax < widthMax)
        {
            if (heightMin < heightMax)
            {
                heightMin += 5;
            }
            else
            {
                heightMax += 5;
                widthMin = 5;
            }
        }
        gridArray = Resize2DArray(gridArray, widthMax, heightMax);

    }
}
