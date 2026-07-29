using CodeMonkey.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;

public class GridBuildingSystem : NetworkSimpleSingleton<GridBuildingSystem>
{
    public Action OnObjectSpawned;
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

    [Header("PostBox")]
    [SerializeField] private GameObject postBoxPrefab;

    [Header("Blocker")]
    [SerializeField] private Transform blockerX;
    [SerializeField] private Transform blockerZ;

    private IGridManager gridManager;
    private IGameManager gameManager;
    private IBuildingPlacementManager buildingPlacementManager;
    private IGridInitializer gridInitializer;
    private IGridVisualizer gridVisualizer;
    private PostBox postBox;
    private bool isInitialized = false;
    private bool stopMoving = false;
    private List<GridWall> gridWallList = new List<GridWall>();

    private Dictionary<string, PlacedObjectTypeSO> placedObjectTypeSODictionary = new Dictionary<string, PlacedObjectTypeSO>();
    
    public PlacedObjectDatabase PlacedObjectDatabase => placedObjectDatabase;
    public Dictionary<string, PlacedObjectTypeSO> PlacedObjectTypeSODictionary => placedObjectTypeSODictionary;

    public Transform Container { get => counterContainer; set => counterContainer = value; }
    public IGridManager GridManager { get => gridManager; set => gridManager = value; }
    public IGameManager GameManagerInterface { get => gameManager; set => gameManager = value; }
    public IBuildingPlacementManager BuildingPlacementManager { get => buildingPlacementManager; set => buildingPlacementManager = value; }
    public IGridInitializer GridInitializer { get => gridInitializer; set => gridInitializer = value; }
    public IGridVisualizer GridVisualizer { get => gridVisualizer; set => gridVisualizer = value; }
    public bool StopMoving { get => stopMoving; set => stopMoving = value; }
    public bool IsInitialized { get => isInitialized; set => isInitialized = value; }
    public PostBox PostBox { get => postBox; set => postBox = value; }

    private void OnDestroy()
    {

    }
    protected override void Awake()
    {
        base.Awake();
        gameManager = GameManager.Instance;

        foreach (var placedObject in placedObjectDatabase.PlacedObjects)
        {
            placedObjectTypeSODictionary[placedObject.Guid] = placedObject;
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsHost || IsServer || MultiplayerManager.Instance.IsSinglePlayerMode)
        {
            Initialize();
        }
        else
        {
            if (GameManager.Instance?.GameData != null)
                Initialize();
            else
                MultiplayerManager.Instance.OnDataSyncToNewClient += (object sender, EventArgs e) => Initialize();
        }
    }

    private void Initialize()
    {
        if (GameManager.Instance?.GameData == null) return;
        if (GameManager.Instance.GameData.GridData.GridArrayData == null)
        {
            gridManager = new GridManager(0, 0, cellSize, Vector3.zero, (GridXZ<GridObject> grid, int x, int z) => new List<GridObject> { new GridObject(grid, x, z) });
        }
        else
        {
            gridManager = new GridManager(GameManager.Instance.GameData.GridData, (GridXZ<GridObject> grid, int x, int z) => new List<GridObject> { new GridObject(grid, x, z) });
        }

        if (IsHost || IsServer || MultiplayerManager.Instance.IsSinglePlayerMode)
        {
            GridObjectSpawner.SpawnObjectsFromData(gridManager.Grid, GameManager.Instance.GameData.GridData.GridArrayData);

            GameObject postBoxInstance = Instantiate(postBoxPrefab, new Vector3(1, 0, -1), Quaternion.identity);
            postBoxInstance.GetComponent<NetworkObject>().Spawn();
            postBoxInstance.transform.eulerAngles = new Vector3(0, 180, 0);
        }

        gridInitializer = new GridInitializer(gridManager, this.gameManager,
            roadContainer, roadPrefab, roadCornerPrefab,
            floorContainer, floorPrefab);

        gridInitializer.InitRoad();
        gridInitializer.InitFloor();
        gridVisualizer = new GridVisualizer(gridManager, gridGuideObject, gridGuideMaterial, gridWallList);
        gridVisualizer.SetActiveGridGuide(false);

        IUIPopupManager uiPopupManagerInstance = UIPopupManager.Instance;
        buildingPlacementManager = new BuildingPlacementManager(gridManager, gridVisualizer, this.gameManager, uiPopupManagerInstance);

        OnObjectSpawned?.Invoke();

        isInitialized = true;
        KitchenGameManager.Instance.Init();
        gameManager.GameData.GridData.Initialize();
        gameManager.InitializePlayer();
        SetBlocker();
        BakeNavMesh();
    }

    private void Start()
    {
        GameInput.Instance.OnMouseClickPerformed += GameInput_OnMouseClickPerformed;
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
        if(GameInput.Instance.IsMouseOverUI() || stopMoving)
            return;
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

    [Rpc(SendTo.Server)]
    public void SpawnObjectServerRpc(string placedObjectTypeSOGuid,Vector2Int origin,Dir dir)
    {
        PlacedObjectTypeSO placedObjectTypeSO = GetPlacedObjectTypeSOByGuid(placedObjectTypeSOGuid);
        if (placedObjectTypeSO == null) return;

        Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
        Vector3 placedObjectWorldPosition = gridManager.Grid.GetWorldPosition(origin) +
            new Vector3(rotationOffset.x, 0, rotationOffset.y) * gridManager.Grid.GetCellSize();
        if (!GridObjectSpawner.IsObjectPlaced(gridManager.Grid, placedObjectTypeSO, origin,dir))
        {
            return;
        }
        
        PlacedObjectFactory.Create(placedObjectWorldPosition, origin, dir,
            placedObjectTypeSO,NetworkManager.Singleton.LocalClientId,false);
    }
    [Rpc(SendTo.Server)]
    public void UpdateGridDataServerRpc(NetworkObjectReference networkObjectReference)
    {
        if(networkObjectReference.TryGet(out NetworkObject networkObject))
        {
            UpdateGridDataClientRpc(networkObject);

        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateGridDataClientRpc(NetworkObjectReference networkObjectReference)
    {
        if(networkObjectReference.TryGet(out NetworkObject networkObject))
        {
            PlacedObjectView placedObjectView = networkObject.GetComponent<PlacedObjectView>();
            //Debug.Log("PlaceObjectType : "+placedObjectView.PlacedObjectTypeSO);
            //Debug.Log("PlaceObjectTypeGuid : "+placedObjectView.GetPlacedObjectTypeSOGuid());
            //Debug.Log("GridManager : " + GridManager);
            List<Vector2Int> gridPositionList = placedObjectView.GetGridPositionList();
            foreach (var gridPosition in gridPositionList)
            {
                GridManager.Grid.AddGridObjectData(gridPosition.x, gridPosition.y,
                    new GridObject(GridManager.Grid, placedObjectView, gridPosition.x, gridPosition.y));
            }

            this.GetComponent<IModuleItem>()?.RegisterItem();

            GameManager.Instance.GameData.UpdateGridData(GridManager.Grid);

        }

    }
    [Rpc(SendTo.Server)]
    public void OnObjectPlacedEventServerRpc()
    {
        OnObjectPlacedEventClientRpc();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void OnObjectPlacedEventClientRpc()
    {
        BuildingPlacementManager.FireOnObjectPlacedEvent();
    }
}
