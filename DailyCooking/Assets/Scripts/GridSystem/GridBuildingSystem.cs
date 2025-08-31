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
    private IGameManager gameManager;
    private IBuildingPlacementManager buildingPlacementManager;
    private IGridInitializer gridInitializer;
    private IGridVisualizer gridVisualizer;
    private List<GridWall> gridWallList = new List<GridWall>();

    private Dictionary<string, PlacedObjectTypeSO> placedObjectTypeSODictionary = new Dictionary<string, PlacedObjectTypeSO>();
    
    public PlacedObjectDatabase PlacedObjectDatabase => placedObjectDatabase;
    public Dictionary<string, PlacedObjectTypeSO> PlaceObjectTypeSODictionary => placedObjectTypeSODictionary;

    public Transform Container { get => counterContainer; set => counterContainer = value; }
    public IGridManager GridManager { get => gridManager; set => gridManager = value; }
    public IGameManager GameManagerInterface { get => gameManager; set => gameManager = value; }
    public IBuildingPlacementManager BuildingPlacementManager { get => buildingPlacementManager; set => buildingPlacementManager = value; }
    public IGridInitializer GridInitializer { get => gridInitializer; set => gridInitializer = value; }
    public IGridVisualizer GridVisualizer { get => gridVisualizer; set => gridVisualizer = value; }

    private void OnDestroy()
    {

    }
    private void Awake()
    {
        // Get concrete instances of singletons
        this.gameManager = GameManager.Instance;

        ICounterModules counterModulesInstance = CounterModules.Instance;
        IUIPopupManager uiPopupManagerInstance = UIPopupManager.Instance;

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
        gridInitializer = new GridInitializer(gridManager, this.gameManager, roadContainer, roadPrefab, roadCornerPrefab, pillarContainer, pillarPrefab, wallContainer, wallPrefab, floorContainer, floorPrefab); // Initialize GridInitializer
        gridInitializer.InitRoad();
        gridInitializer.InitWallAndFloor();
        gridInitializer.InitPillar();
        gridVisualizer = new GridVisualizer(gridManager, gridGuideObject, gridGuideMaterial, gridWallList); // Initialize GridVisualizer
        gridVisualizer.SetActiveGridGuide(false); // Moved from InitGridGuide()


        buildingPlacementManager = new BuildingPlacementManager(gridManager, gridVisualizer, this.gameManager, counterModulesInstance, uiPopupManagerInstance); // Initialize BuildingPlacementManager
        counterModulesInstance.Initialize(); // Initialize CounterModules here

    }
    private void Start()
    {

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
                buildingPlacementManager.HandleExistingObjectInteraction(targetPlaceObjectView);
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
        gameManager.GameData.UpdateGridData(gridManager.Grid);
        gridInitializer.InitWallAndFloor();
        gridInitializer.InitPillar();
        if (!GameManager.Instance.GameData.TutorialData.HasPlayedFirstTime)
        {
            gridInitializer.InitDefaultCounters();
            gameManager.GameData.UpdateGridData(gridManager.Grid);
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
    
    
    
    
    public void SaveGrid()
    {
        gameManager.GameData.UpdateGridData(gridManager.Grid);
    }
}
