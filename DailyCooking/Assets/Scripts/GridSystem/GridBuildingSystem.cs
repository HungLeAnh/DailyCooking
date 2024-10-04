using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using JetBrains.Annotations;
using UnityEngine.InputSystem;

public class GridBuildingSystem : MonoBehaviour
{
    [SerializeField] private List<PlacedObjectTypeSO> placedObjectTypeSOList;
    [SerializeField] int gridWidth = 20;
    [SerializeField] int gridHeight = 20;
    [SerializeField] float cellSize = 2f;

    private GridXZ<GridObject> grid;
    private PlacedObjectTypeSO.Dir dir = PlacedObjectTypeSO.Dir.Down;
    private PlacedObjectTypeSO placedObjectTypeSO;

    private void Awake()
    {
       
        grid = new GridXZ<GridObject>(gridWidth,gridHeight,cellSize,Vector3.zero,(GridXZ<GridObject> grid,int x,int z) => new GridObject(grid,x,z));
        
        placedObjectTypeSO = placedObjectTypeSOList[0];

    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            grid.GetXZ(UtilsClass.GetMouseWorldPosition3D(),out int x, out int z);

            List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(new Vector2Int(x, z), dir);

            //Test can build
            bool canBuild = true;
            foreach (var gridPosition in gridPositionList)
            {
                if (!grid.GetGridObject(gridPosition.x, gridPosition.y).CanBuild())
                {
                    canBuild = false; 
                    break;
                }
            }

            if (canBuild)
            {
                Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, z) + 
                    new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();
                PlacedObject placedObject = PlacedObject.Create(placedObjectWorldPosition, new Vector2Int(x,z) , dir, placedObjectTypeSO);

                foreach (var gridPosition in gridPositionList)
                {
                    grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);

                }
            }
            else
            {
                UtilsClass.CreateWorldTextPopup("Can't build here!", UtilsClass.GetMouseWorldPosition3D());
            }
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            dir = PlacedObjectTypeSO.GetNextDir(dir);

        }
    }
}
public class GridObject
{
    private GridXZ<GridObject> grid;
    private int x;
    private int z;
    private PlacedObject placedObject;

    public GridObject(GridXZ<GridObject> grid, int x, int z)
    {
        this.grid = grid;
        this.x = x;
        this.z = z;
    }
    public void SetPlacedObject(PlacedObject placedObject)
    {
        this.placedObject = placedObject;
        grid.TriggerGridObjectChanged(x,z);
    }
    public PlacedObject GetPlacedObject(PlacedObject placedObject)
    {
        return placedObject;
    }
    public void ClearTransform()
    {
        placedObject = null;
        grid.TriggerGridObjectChanged(x, z);

    }
    public bool CanBuild()
    {
        return placedObject == null;
    }

    public override string ToString()
    {
        return x + ", " + z;
    }
}