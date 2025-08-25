using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using JetBrains.Annotations;
using UnityEngine.InputSystem;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.InputSystem.EnhancedTouch;
using Newtonsoft.Json;

public class GridBuildingSystem : SimpleSingleton<GridBuildingSystem>
{    
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;
    private const float GRID_OFFSET = 0.01f;


    public event EventHandler OnBuildingStart;
    public event EventHandler OnBuildingEnd;
    public event EventHandler<OnSelectedChangedArgs> OnSelectedChanged;
    public class OnSelectedChangedArgs : EventArgs
    {
        public PlacedObjectTypeSO placedObjectTypeSO;
        public Vector3 position;
    }
    public event EventHandler OnObjectPlaced;
    public event EventHandler<PlacedObjectTypeSO> OnReturnPlaceObjectToInventory;

    [SerializeField] private float cellSize = 2f;
    [SerializeField] private PlacedObjectDatabase placedObjectDatabase;

    [Header("Grid Guide")]
    [SerializeField] private GameObject gridGuideObject;
    [SerializeField] private Material gridGuideMaterial;
    
    [Header("Counter")]
    [SerializeField] private Transform counterContainer;
    [SerializeField] private LayerMask counterLayerMask;

    [Header("Floor")]
    [SerializeField] private Transform floorContainer;
    [SerializeField] private GameObject floorPrefab;

    [Header("Wall")]
    [SerializeField] private Transform wallContainer;
    [SerializeField] private GameObject wallPrefab;    
    
    [Header("Pillar")]
    [SerializeField] private Transform pillarContainer;
    [SerializeField] private GameObject pillarPrefab;

    [Header("Road")]
    [SerializeField] private Transform roadContainer;
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private GameObject roadCornerPrefab;

    private GridXZ<GridObject> grid;
    private Dir dir = Dir.Down;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private List<GridWall> gridWallList = new List<GridWall>(); 
    private bool isEditing = false;


    private Dictionary<string, PlacedObjectTypeSO> placedObjectTypeSODictionary = new Dictionary<string, PlacedObjectTypeSO>();
    public PlacedObjectTypeSO PlacedObjectTypeSO => placedObjectTypeSO;
    public PlacedObjectDatabase PlacedObjectDatabase => placedObjectDatabase;
    public Dictionary<string, PlacedObjectTypeSO> PlaceObjectTypeSODictionary => placedObjectTypeSODictionary;

    public Transform Container { get => counterContainer; set => counterContainer = value; }

    private void OnDestroy()
    {

    }
    private void Awake()
    {
        foreach (var placedObject in placedObjectDatabase.PlacedObjects)
        {
            placedObjectTypeSODictionary[placedObject.Guid] = placedObject;
        }
    }
    private void Start()
    {
        if (GameManager.Instance.GameData.GridData.GridArrayData == null)
        {
            grid = new GridXZ<GridObject>(cellSize, Vector3.zero, (GridXZ<GridObject> grid, int x, int z) => new GridObject(grid, x, z));
        }
        else
        {
            grid = new GridXZ<GridObject>(GameManager.Instance.GameData.GridData, (GridXZ<GridObject> grid, int x, int z) => new GridObject(grid, x, z));

        }
        InitRoad();
        InitWallAndFloor();
        InitPillar();
        InitGridGuide();
        CounterModules.Instance.Initialize();

        GameInput.Instance.OnFingerDown += GameInput_OnFingerDown;
        GameInput.Instance.OnFingerUp += GameInput_OnFingerUp;
    }

    private void GameInput_OnFingerUp(object sender, Finger e)
    {

    }
    private void GameInput_OnFingerDown(object sender, Finger e)
    {
        if (!isEditing || placedObjectTypeSO != null)
            return;
        float maxDistance = 999f;
        Ray ray = Camera.main.ScreenPointToRay(e.screenPosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, maxDistance, counterLayerMask))
        {
            if (raycastHit.transform.TryGetComponent<PlacedObjectView>(out PlacedObjectView targetPlaceObjectView))
            {
                dir = targetPlaceObjectView.GetModel().Dir;
                SetPlacedObjectTypeSO(targetPlaceObjectView.GetModel().PlacedObjectTypeSO,raycastHit.transform.position);
                var counterView = targetPlaceObjectView.GetComponent<BaseCounterView>();
                CounterModules.Instance.DestroyCounter(counterView);
                UIPopupManager.Instance.HidePopup(UIPopupType.UIInventoryPopup,
                    new UIInventoryPopup.Param { isPlacingObject = true});
            }
        }
    }
    public void DestroyPlaceObject(PlacedObjectView placedObjectView)
    {
        List<Vector2Int> gridPositionList = placedObjectView.GetGridPositionList();
        foreach (Vector2Int gridPosition in gridPositionList)
        {
            grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
        }
        placedObjectView.DestroySelf();
    }
    private void InitRoad()
    {
        GameObject cornerRoad = Instantiate(roadCornerPrefab, grid.GetWorldPosition(0, 0), Quaternion.identity);
        cornerRoad.transform.SetParent(roadContainer);

        for (int i = 0; i < 100; i++)
        {
            GameObject road = Instantiate(roadPrefab, grid.GetWorldPosition(0, i), Quaternion.identity);
            road.transform.SetParent(roadContainer);
            road.transform.rotation = Quaternion.Euler(0, 0, 0);

        }        
        for (int i = 0; i < 100; i++)
        {
            GameObject road = Instantiate(roadPrefab, grid.GetWorldPosition(i, 0), Quaternion.identity);
            road.transform.SetParent(roadContainer);
            road.transform.rotation = Quaternion.Euler(0, 270, 0);

        }
    }    
    private void InitPillar()
    {
        if(grid.GetWidth() == 0 || grid.GetHeight() == 0)
            return;
        GameObject botLeftPillar = Instantiate(pillarPrefab, grid.GetWorldPosition(0, 0), Quaternion.identity);
        botLeftPillar.transform.SetParent(roadContainer);
        botLeftPillar.transform.rotation = Quaternion.Euler(0, 0, 0);
        gridWallList.Add(botLeftPillar.GetComponent<GridWall>());

        GameObject botRightPillar = Instantiate(pillarPrefab, grid.GetWorldPosition(0,grid.GetHeight() ), Quaternion.identity);
        botRightPillar.transform.SetParent(roadContainer);  
        botRightPillar.transform.rotation = Quaternion.Euler(0, 90, 0);
        gridWallList.Add(botRightPillar.GetComponent<GridWall>());

        GameObject topLeftPillar = Instantiate(pillarPrefab, grid.GetWorldPosition(grid.GetWidth(), 0), Quaternion.identity);
        topLeftPillar.transform.SetParent(roadContainer);
        topLeftPillar.transform.rotation = Quaternion.Euler(0, 270, 0);
        gridWallList.Add(topLeftPillar.GetComponent<GridWall>());

        GameObject topRightPillar = Instantiate(pillarPrefab, grid.GetWorldPosition(grid.GetWidth(),grid.GetHeight()), Quaternion.identity);
        topRightPillar.transform.SetParent(roadContainer);
        topRightPillar.transform.rotation = Quaternion.Euler(0, 180, 0);
        gridWallList.Add(topRightPillar.GetComponent<GridWall>());

    }

    private void InitGridGuide()
    {
        SetActiveGridGuide(false);
    }

    private void InitWallAndFloor()
    {
        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int z = 0; z < grid.GetHeight(); z++)
            {
                GameObject floor = Instantiate(floorPrefab, grid.GetWorldPosition(x, z), Quaternion.identity);
                floor.transform.SetParent(floorContainer);
                floor.transform.localPosition = new Vector3(floor.transform.localPosition.x, 0f, floor.transform.localPosition.z);

                if (x == 0 || z == 0 || x == grid.GetWidth() - 1 || z == grid.GetHeight() - 1)
                {
                    PlaceWall(x, z, grid.GetWidth(), grid.GetHeight());
                }
            }
        }
    }

    private void PlaceWall(int x, int z, int gridWidth, int gridHeight)
    {
        GameObject wall = Instantiate(wallPrefab, grid.GetWorldPosition(x, z) +
            new Vector3(grid.GetCellSize() / 2, 0, grid.GetCellSize() / 2), Quaternion.identity);
        wall.transform.SetParent(wallContainer);
        gridWallList.Add(wall.GetComponent<GridWall>());

        if (x == 0) // Left border (facing right)
        {
            wall.transform.localPosition += new Vector3(-0.25f, 0, 0);
            wall.transform.rotation = Quaternion.Euler(0, 270, 0);
        }
        else if (x == gridWidth - 1) // Right border (facing left)
        {
            wall.transform.localPosition -= new Vector3(-0.25f, 0, 0);
            wall.transform.rotation = Quaternion.Euler(0, 90, 0);
        }
        if (z == 0) // Bottom border (facing down)
        {
            wall.transform.localPosition -= new Vector3(0, 0, 0.25f);
            wall.transform.rotation = Quaternion.Euler(0, 180, 0);
        }
        else if (z == gridHeight - 1) // Top border (facing up)
        {
            wall.transform.localPosition += new Vector3(0, 0, 0.25f);
            wall.transform.rotation = Quaternion.Euler(0, 0, 0);
        }

        if (x == 0) // Bottom corner
        {
            if (z == 0 || z == gridHeight - 1)
            {
                GameObject blcwall = Instantiate(wallPrefab, grid.GetWorldPosition(x, z) +
                                    new Vector3(grid.GetCellSize() / 2, 0, grid.GetCellSize() / 2), Quaternion.identity);
                blcwall.transform.SetParent(wallContainer);
                blcwall.transform.localPosition -= new Vector3(0.25f, 0, 0);
                blcwall.transform.rotation = Quaternion.Euler(0, 270, 0);
                gridWallList.Add(blcwall.GetComponent<GridWall>());

                wall.transform.localPosition += new Vector3(0.25f, 0, 0);
            }
        }
        else if (x == gridWidth - 1) // Top corner
        {
            if (z == 0 || z == gridHeight - 1)
            {
                GameObject brcwall = Instantiate(wallPrefab, grid.GetWorldPosition(x, z) +
                                    new Vector3(grid.GetCellSize() / 2, 0, grid.GetCellSize() / 2), Quaternion.identity);
                brcwall.transform.SetParent(wallContainer);
                brcwall.transform.localPosition += new Vector3(0.25f, 0, 0);
                brcwall.transform.rotation = Quaternion.Euler(0, 90, 0);
                gridWallList.Add(brcwall.GetComponent<GridWall>());

                wall.transform.localPosition += new Vector3(-0.25f, 0, 0);
            }
        }
    }

    public void UnlockGrid()
    {
        grid.UnlockGrid(GameDefine.GridSize,GameDefine.GridSize);
        GameManager.Instance.GameData.UpdateGridData(grid);
        InitWallAndFloor();
        InitPillar();
        if (!GameManager.Instance.GameData.TutorialData.HasPlayedFirstTime)
        {
            InitDefaultCounters();
            GameManager.Instance.GameData.UpdateGridData(grid);
        }
    }
    private void InitDefaultCounters()
    {
        List<GridObjectData> gridObjectDataList = JsonConvert.DeserializeObject<List<GridObjectData>>(GameDefine.GridArrayDataInit,GameManager.Instance.DataHandler.Settings);
        foreach (GridObjectData gridObject in gridObjectDataList)
        {
            if (GameManager.Instance.GameData.GridData.GridArrayData.Contains(gridObject))
                continue;
            GameManager.Instance.GameData.GridData.GridArrayData.Add(gridObject);
        }
        grid.AddGridObjectData(gridObjectDataList);
    }
    public void SetActiveGridGuide(bool isActive)
    {
        gridGuideObject.SetActive(isActive);
        gridGuideMaterial.SetFloat("_GridWidth", grid.GetWidth() + GRID_OFFSET);
        gridGuideMaterial.SetFloat("_GridHeight", grid.GetHeight() + GRID_OFFSET);
        gridGuideMaterial.SetVector("_CellSize", new Vector2(grid.GetCellSize(), grid.GetCellSize()));
    }
    public void RotateBuildingObject()
    {
        dir = PlacedObjectTypeSO.GetNextDir(dir);
        //Debug.Log("Dir: "+ dir.ToString());
    }
    public bool TryPlaceBuildingObject(Vector3 interactPos)
    {
        if (placedObjectTypeSO == null) return false;

        //Debug.LogError($"{interactPos}: ({Mathf.RoundToInt(interactPos.x)}," +
        //    $"{Mathf.RoundToInt(interactPos.y)},{Mathf.RoundToInt(interactPos.z)})");

        grid.GetXZ(new Vector3(Mathf.RoundToInt(interactPos.x),
                                Mathf.RoundToInt(interactPos.y),
                                Mathf.RoundToInt(interactPos.z)), out int x, out int z);

        Vector2Int placedObjectOrigin = new Vector2Int(x, z);
        placedObjectOrigin = grid.ValidateGridPosition(placedObjectOrigin);
        
        //Debug.LogError($"placedObjectOrigin : {placedObjectOrigin}");
        
        if (placedObjectOrigin == Vector2Int.zero && (interactPos.x < 0 || interactPos.z < 0))
        {
            return false;
        }
        List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(placedObjectOrigin, dir);


        bool canBuild = true;

        foreach (var gridPosition in gridPositionList)
        {
            var gridObject = grid.GetGridObject(gridPosition.x, gridPosition.y);
            if (gridObject == null || !gridObject.CanBuild())
            {
                canBuild = false;
                break;
            }
        }

        if (canBuild)
        {
            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) +
                new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();
            PlacedObjectView placedObject = PlacedObjectFactory.Create(placedObjectWorldPosition, placedObjectOrigin, dir, placedObjectTypeSO);
            CounterModules.Instance.AddCounterController(placedObject.GetComponent<BaseCounterController>());
            
            foreach (var gridPosition in gridPositionList)
            {
                grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);

            }
            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            
            GameManager.Instance.GameData.UpdateGridData(grid);
            DeselectObjectType();
            return true;
        }
        else
        {
               return false;
        }
    }
    private void DeselectObjectType()
    {
        placedObjectTypeSO = null;
        dir = Dir.Down;
        RefreshSelectedObjectType(-Vector3.one);

    }
    private void RefreshSelectedObjectType(Vector3 targetPosition)
    {
        OnSelectedChanged?.Invoke(this, new OnSelectedChangedArgs { placedObjectTypeSO = placedObjectTypeSO,
                                            position = targetPosition});

    }
    public void FireOnBuildingStartEvent()
    {
        OnBuildingStart?.Invoke(this, EventArgs.Empty);
        SetActiveGridGuide(true);
        ShowWallShadow(true);
        isEditing = true;
    }
    public void FireOnBuildingEndEvent()
    {
        OnBuildingEnd?.Invoke(this, EventArgs.Empty);
        SetActiveGridGuide(false);
        ShowWallShadow(false);
        isEditing = false;
    }
    public Vector3 GetMouseWorldSnappedPosition()
    {
        Vector3 interactPos = UtilsClass.GetTouchWorldPosition3D();
        if(interactPos == -Vector3.one)
            return -Vector3.one;
        
        grid.GetXZ(new Vector3(Mathf.RoundToInt(interactPos.x),
                                Mathf.RoundToInt(interactPos.y),
                                Mathf.RoundToInt(interactPos.z)), out int x, out int z);
        Vector2Int placedObjectOrigin = new Vector2Int(x, z);
        placedObjectOrigin = grid.ValidateGridPosition(placedObjectOrigin);
        if (placedObjectTypeSO != null)
        {
            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y); //+ new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();
            return placedObjectWorldPosition;
        }
        else
        {
            return -Vector3.one;
        }
    }

    public Quaternion GetPlacedObjectRotation()
    {
        if (placedObjectTypeSO != null)
        {
            return Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }
    public Vector3 GetPlacedObjectRotationOffset()
    {
        Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
        Vector3 positionOffset = new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();
        return positionOffset;
    }
    public void SetPlacedObjectTypeSO(PlacedObjectTypeSO placedObjectTypeSO,Vector3 objectPosition)
    {
        if(this.placedObjectTypeSO != null)
        {
            GameManager.Instance.GameData.AddInventoryData(this.placedObjectTypeSO.Guid);//??? should be using RemoveInventoryData?

            GameManager.Instance.GameData.UpdateGridData(grid);
            OnReturnPlaceObjectToInventory?.Invoke(this, placedObjectTypeSO);
        }
        this.placedObjectTypeSO = placedObjectTypeSO;
        RefreshSelectedObjectType(objectPosition);
    }

    private void ShowWallShadow(bool isShow)
    {
        foreach (var wall in gridWallList)
        {
            if (isShow)
            {
                wall.OnGridEdit();
            }
            else
            {
                wall.OnExitGridEdit();
            }
            
        }
    }

    public PlacedObjectTypeSO GetPlacedObjectTypeSOByGuid(string Guid)
    {
        if (placedObjectTypeSODictionary.TryGetValue(Guid,out PlacedObjectTypeSO placedObjectSO))
        {
            return placedObjectSO;
        }
        else
        {
            return null;
        }
    }
    public PlacedObjectTypeSO GetPlacedObjectTypeSOById(string id)
    {
        return PlacedObjectDatabase.PlacedObjects.Find(x => x.id == id);
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

    public void SaveGrid()
    {
        GameManager.Instance.GameData.UpdateGridData(grid);
    }

}
