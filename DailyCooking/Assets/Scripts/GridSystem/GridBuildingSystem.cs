using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using System;
using Unity.Mathematics;
using UnityEngine.InputSystem.EnhancedTouch;
using Newtonsoft.Json;
using Unity.AI.Navigation;

public class GridBuildingSystem : SimpleSingleton<GridBuildingSystem>
{    
    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;
    private const float GRID_OFFSET = 0.01f;

    public class OnSelectedChangedArgs : EventArgs
    {
        public PlacedObjectTypeSO placedObjectTypeSO;
        public Vector3 position;
    }

    [SerializeField] private float cellSize = 2f;
    [SerializeField] private PlacedObjectDatabase placedObjectDatabase;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Grid Guide")]
    [SerializeField] private GameObject gridGuideObject;
    [SerializeField] private Material gridGuideMaterial;

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

    
    
    [Header("Counter")]
    [SerializeField] private Transform counterContainer;
    [SerializeField] private LayerMask counterLayerMask;    

    private IGridManager gridManager;
    public IBuildingPlacementManager buildingPlacementManager;
    private IGridInitializer gridInitializer;
    private IGridVisualizer gridVisualizer;
    private List<GridWall> gridWallList = new List<GridWall>(); 
     
    


    private Dictionary<string, PlacedObjectTypeSO> placedObjectTypeSODictionary = new Dictionary<string, PlacedObjectTypeSO>();
    
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
        if (GameManager.Instance.GameData.GridData.GridArrayData == null)
        {
            gridManager = new GridManager(cellSize, Vector3.zero, (GridXZ<GridObject> grid, int x, int z) => new GridObject(grid, x, z));
        }
        else
        {
            gridManager = new GridManager(GameManager.Instance.GameData.GridData, (GridXZ<GridObject> grid, int x, int z) => new GridObject(grid, x, z));

        }
        gridInitializer = new GridInitializer(gridManager, roadContainer, roadPrefab, roadCornerPrefab, pillarContainer, pillarPrefab, wallContainer, wallPrefab, floorContainer, floorPrefab); // Initialize GridInitializer
        gridInitializer.InitRoad();
        gridInitializer.InitWallAndFloor();
        gridInitializer.InitPillar();
        gridVisualizer = new GridVisualizer(gridManager, gridGuideObject, gridGuideMaterial, gridWallList); // Initialize GridVisualizer
        gridVisualizer.SetActiveGridGuide(false); // Moved from InitGridGuide()

        buildingPlacementManager = new BuildingPlacementManager(gridManager, gridVisualizer); // Initialize BuildingPlacementManager

    }
    private void Start()
    {

        CounterModules.Instance.Initialize();

        GameInput.Instance.OnFingerDown += GameInput_OnFingerDown;
        GameInput.Instance.OnFingerUp += GameInput_OnFingerUp;
        navMeshSurface.BuildNavMesh();
    }

    private void GameInput_OnFingerUp(object sender, Finger e)
    {

    }
    private void GameInput_OnFingerDown(object sender, Finger e)
    {
        if (!buildingPlacementManager.IsBuilding || 
            buildingPlacementManager.PlacedObjectTypeSO != null)
            return; // Check if in edit mode

        float maxDistance = 999f;
        Ray ray = Camera.main.ScreenPointToRay(e.screenPosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, maxDistance, counterLayerMask))
        {
            if (raycastHit.transform.TryGetComponent<PlacedObjectView>(out PlacedObjectView targetPlaceObjectView))
            {
                // dir = targetPlaceObjectView.GetModel().Dir; // Handled by BuildingPlacementManager
                buildingPlacementManager.SetPlacedObjectTypeSO(targetPlaceObjectView.GetModel().PlacedObjectTypeSO,raycastHit.transform.position);
                var counterView = targetPlaceObjectView.GetComponent<BaseCounterView>();
                CounterModules.Instance.DestroyCounter(counterView);
                UIPopupManager.Instance.HidePopup(UIPopupType.UIInventoryPopup,
                    new UIInventoryPopup.Param { isPlacingObject = true});
            }
            else
            {
                buildingPlacementManager.TryPlaceBuildingObject(raycastHit.point);
            }
        }
    }
    public void DestroyPlaceObject(PlacedObjectView placedObjectView)
    {
        buildingPlacementManager.DestroyPlaceObject(placedObjectView);
    }
        
    

    

    

    

    public void UnlockGrid()
    {
        gridManager.UnlockGrid(GameDefine.GridSize,GameDefine.GridSize);
        GameManager.Instance.GameData.UpdateGridData(gridManager.Grid);
        gridInitializer.InitWallAndFloor();
        gridInitializer.InitPillar();
        if (!GameManager.Instance.GameData.TutorialData.HasPlayedFirstTime)
        {
            gridInitializer.InitDefaultCounters();
            GameManager.Instance.GameData.UpdateGridData(gridManager.Grid);
        }
    }
    
    
    public void RotateBuildingObject()
    {
        buildingPlacementManager.RotateBuildingObject();
    }
    public bool TryPlaceBuildingObject(Vector3 interactPos)
    {
        return buildingPlacementManager.TryPlaceBuildingObject(interactPos);
    }
    
    
    
    
    public Vector3 GetMouseWorldSnappedPosition()
    {
        return buildingPlacementManager.GetMouseWorldSnappedPosition();
    }

    public Quaternion GetPlacedObjectRotation()
    {
        return buildingPlacementManager.GetPlacedObjectRotation();
    }
    public Vector3 GetPlacedObjectRotationOffset()
    {
        return buildingPlacementManager.GetPlacedObjectRotationOffset();
    }
    public void SetPlacedObjectTypeSO(PlacedObjectTypeSO placedObjectTypeSO,Vector3 objectPosition)
    {
        buildingPlacementManager.SetPlacedObjectTypeSO(placedObjectTypeSO, objectPosition);
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
        for (int x = 0; x < gridManager.GetWidth(); x++)
        {
            for (int z = 0; z < gridManager.GetHeight(); z++)
            {
                if (gridManager.Grid.GetGridObject(x, z).CanBuild())
                {
                    return gridManager.GetWorldPosition(x, z);
                }
            }
        }
        return Vector3.zero;
    }

    public int2 WorldPositionToGridPos(float x, float y)
    {
        if(gridManager.ValidateGridPosition(new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y))) != null)
        {
            gridManager.GetXZ(new Vector3(Mathf.RoundToInt(x), 0, Mathf.RoundToInt(y)), out int xPos, out int yPos);
            return new int2(xPos, yPos);
        }
        return  new int2(-int.MaxValue, -int.MaxValue);
    }

    public Vector3 GridPositionToWorldPosition(int2 int2)
    {
        if (gridManager.ValidateGridPosition(new Vector2Int(int2.x, int2.y)) != null)
        {
            return gridManager.GetWorldPosition(int2.x, int2.y);
        }
        return Vector3.negativeInfinity;
    }

    public Vector2 GetGridSize()
    {
        return new Vector2Int(gridManager.GetWidth(), gridManager.GetHeight());
    }

    public void SaveGrid()
    {
        GameManager.Instance.GameData.UpdateGridData(gridManager.Grid);
    }

}
