using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using System;
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

    [Header("Road")]
    [SerializeField] private Transform roadContainer;
    [SerializeField] private GameObject roadPrefab;
    [SerializeField] private GameObject roadCornerPrefab;

    [Header("Counter")]
    [SerializeField] private Transform counterContainer;
    [SerializeField] private LayerMask counterLayerMask;

    [Header("Blocker")]
    [SerializeField] private Transform blockerX;
    [SerializeField] private Transform blockerZ;

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
    protected override void Awake()
    {
        base.Awake();
        gameManager = GameManager.Instance;
        IUIPopupManager uiPopupManagerInstance = UIPopupManager.Instance;

        foreach (var placedObject in placedObjectDatabase.PlacedObjects)
        {
            placedObjectTypeSODictionary[placedObject.Guid] = placedObject;
        }
        if (GameManager.Instance.GameData.GridData.GridArrayData == null)
        {
            gridManager = new GridManager(0, 0, cellSize, Vector3.zero, (GridXZ<GridObject> grid, int x, int z) => new List<GridObject> {new GridObject(grid, x, z) });
        }
        else
        {
            gridManager = new GridManager(GameManager.Instance.GameData.GridData, (GridXZ<GridObject> grid, int x, int z) => new List<GridObject> { new GridObject(grid, x, z) });

        }
        gridInitializer = new GridInitializer(gridManager, this.gameManager, 
            roadContainer, roadPrefab, roadCornerPrefab, 
            floorContainer, floorPrefab);

        gridInitializer.InitRoad();
        gridInitializer.InitFloor();
        gridVisualizer = new GridVisualizer(gridManager, gridGuideObject, gridGuideMaterial, gridWallList);
        gridVisualizer.SetActiveGridGuide(false);


        buildingPlacementManager = new BuildingPlacementManager(gridManager, gridVisualizer, this.gameManager, uiPopupManagerInstance);

        KitchenGameManager.Instance.Init();
    }
    private void Start()
    {
        GameInput.Instance.OnMouseClickPerformed += GameInput_OnMouseClickPerformed;
        SetBlocker();
        BakeNavMesh();
    }
    public void BakeNavMesh()
    {
        navMeshSurface.BuildNavMesh();
    }
    private void SetBlocker()
    {
        blockerX.localPosition = new Vector3(0f, 0f, gridManager.GetHeightMax() * gridManager.GetCellSize() + 5f);
        blockerZ.localPosition = new Vector3(gridManager.GetWidthMax() * gridManager.GetCellSize() + 5f, 0f, 0f);
    }

    private void GameInput_OnMouseClickPerformed(object sender, Vector2 e)
    {
        // Check if in edit mode
        if (!BuildingPlacementManager.IsBuilding ||
            BuildingPlacementManager.PlacedObjectTypeSO != null)
            return; 
        float maxDistance = 999f;
        Ray ray = Camera.main.ScreenPointToRay(e);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, maxDistance, counterLayerMask))
        {
            if (raycastHit.transform.TryGetComponent<PlacedObjectView>(out PlacedObjectView targetPlaceObjectView))
            {
                if(targetPlaceObjectView.transform.GetComponent<IPlaceable>().CanRemove())
                    BuildingPlacementManager.HandleExistingObjectInteraction(targetPlaceObjectView, raycastHit.transform.position);
                else
                {
                    UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameNotiPopup,
                        new UIGameNotiPopup.Param
                        {
                            Title = "warning",
                            Message = "Item is used, cannot remove."
                        });
                }
            }
        }
    }
    public void UnlockGrid()
    {
        gridManager.UnlockGrid(GameDefine.GridSize,GameDefine.GridSize);
        gameManager.GameData.UpdateGridData(gridManager.Grid);
        gridInitializer.InitFloor();
        if (!GameManager.Instance.GameData.TutorialData.HasPlayedFirstTime)
        {
            gridInitializer.InitDefaultCounters();
            gameManager.GameData.UpdateGridData(gridManager.Grid);
        }
        SetBlocker();
    }
    public void ExpandGrid(float amount)
    {
        gridManager.ExpandGrid();
        gameManager.GameData.UpdateGridData(gridManager.Grid);
        SetBlocker();
        gridInitializer.InitFloor();
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
}
