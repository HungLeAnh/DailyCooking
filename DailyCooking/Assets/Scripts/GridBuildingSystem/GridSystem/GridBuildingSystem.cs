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
    [SerializeField] private DefaultGridConfigSO DefaultGridConfigSO;
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

    public override void OnDestroy()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnMouseClickPerformed -= GameInput_OnMouseClickPerformed;
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.OnDataSyncToNewClient -= MultiplayerManager_OnDataSyncToNewClient;
        base.OnDestroy();
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
                MultiplayerManager.Instance.OnDataSyncToNewClient += MultiplayerManager_OnDataSyncToNewClient;
        }
    }

    private void MultiplayerManager_OnDataSyncToNewClient(object sender, EventArgs e)
    {
        MultiplayerManager.Instance.OnDataSyncToNewClient -= MultiplayerManager_OnDataSyncToNewClient;
        Initialize();
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
            floorContainer, floorPrefab,DefaultGridConfigSO);

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
    // Bots path on the server only; refresh after the floor changes size.
    private void RebuildNavMesh()
    {
        if (!IsServer) return;
        if (navMeshSurface.navMeshData == null)
            navMeshSurface.BuildNavMesh();
        else
            navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);
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
            // Tools (e.g. pan mesh) live on child transforms: walk up to the owning placed object.
            PlacedObjectView targetPlaceObjectView = raycastHit.transform.GetComponentInParent<PlacedObjectView>();
            if (targetPlaceObjectView != null)
            {
                IPlaceable placeable = targetPlaceObjectView.GetComponent<IPlaceable>();
                if (placeable != null && placeable.CanRemove())
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
    // Server only: first-time unlock of the starting area (plus default counters for a new restaurant).
    public void UnlockGrid()
    {
        if (!IsServer)
        {
            Debug.LogWarning("GridBuildingSystem.UnlockGrid must run on the server.");
            return;
        }
        gridManager.UnlockGrid(GameDefine.GridSize,GameDefine.GridSize);
        OnGridSizeChangedOnServer();
        if (!GameManager.Instance.GameData.TutorialData.HasPlayedFirstTime)
        {
            gridInitializer.InitDefaultCounters();
        }
    }
    // Server only: called by the server-validated expansion upgrade purchase.
    public void ExpandGrid(float amount)
    {
        if (!IsServer)
        {
            Debug.LogWarning("GridBuildingSystem.ExpandGrid must run on the server.");
            return;
        }
        gridManager.ExpandGrid();
        OnGridSizeChangedOnServer();
    }

    // Sent before any object spawns into the new cells, so clients have them ready.
    private void OnGridSizeChangedOnServer()
    {
        var grid = gridManager.Grid;
        ApplyGridSizeClientRpc(grid.GetWidthMin(), grid.GetHeightMin(), grid.GetWidthMax(), grid.GetHeightMax());
        RefreshGridArea();
        RebuildNavMesh();
    }

    [Rpc(SendTo.NotServer)]
    private void ApplyGridSizeClientRpc(int widthMin, int heightMin, int widthMax, int heightMax)
    {
        if (gridManager == null) return;
        gridManager.Grid.SetSize(widthMin, heightMin, widthMax, heightMax);
        RefreshGridArea();
    }

    private void RefreshGridArea()
    {
        gridInitializer.InitFloor();
        SetBlocker();
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

    // Server only. Placed objects stay server-owned so they survive their builder leaving.
    public NetworkObject SpawnPlacedObject(PlacedObjectTypeSO placedObjectTypeSO, Vector2Int origin, Dir dir)
    {
        if (!IsServer || placedObjectTypeSO == null) return null;
        if (placedObjectTypeSO.prefab == null)
        {
            Debug.LogError($"GridBuildingSystem: Missing prefab for PlacedObjectTypeSO '{placedObjectTypeSO.name}' Guid={placedObjectTypeSO.Guid}", placedObjectTypeSO);
            return null;
        }
        Vector3 worldPosition = PlacementRules.GetWorldPosition(gridManager.Grid, placedObjectTypeSO, origin, dir);
        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, worldPosition,
            Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0), counterContainer).transform;
        placedObjectTransform.GetComponent<PlacedObjectView>().Intialize(placedObjectTypeSO.Guid, origin, dir);
        NetworkObject networkObject = placedObjectTransform.GetComponent<NetworkObject>();
        networkObject.Spawn();
        return networkObject;
    }

    // A player places an item from the restaurant inventory.
    [Rpc(SendTo.Server)]
    public void PlaceObjectServerRpc(string placedObjectTypeSOGuid, Vector2Int origin, Dir dir, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        PlacedObjectTypeSO placedObjectTypeSO = string.IsNullOrEmpty(placedObjectTypeSOGuid) ? null : GetPlacedObjectTypeSOByGuid(placedObjectTypeSOGuid);
        if (placedObjectTypeSO == null || gridManager == null || GameManager.Instance.GameData == null) return;

        if (GameManager.Instance.GameData.InventoryData.Count(placedObjectTypeSOGuid) <= 0)
        {
            UIManager.Instance.ShowAlert(senderClientId, "This item is not in the inventory.");
            return;
        }
        if (!PlacementRules.CanPlace(gridManager.Grid, placedObjectTypeSO, origin, dir, requireUnlockedCells: true))
        {
            UIManager.Instance.ShowAlert(senderClientId, "Can't build here!");
            return;
        }

        GameManager.Instance.ServerRemoveInventory(placedObjectTypeSOGuid);
        SpawnPlacedObject(placedObjectTypeSO, origin, dir);
        OnObjectPlacedEventClientRpc();
    }

    // A player picks up a placed object to move it; it goes back to the inventory right away,
    // so cancelling or quitting mid-move never loses it.
    [Rpc(SendTo.Server)]
    public void PickUpPlacedObjectServerRpc(NetworkObjectReference placedObjectReference, RpcParams rpcParams = default)
    {
        if (!placedObjectReference.TryGet(out NetworkObject placedObjectNetworkObject)) return;
        PlacedObjectView placedObjectView = placedObjectNetworkObject.GetComponent<PlacedObjectView>();
        if (placedObjectView == null || GameManager.Instance.GameData == null) return;

        IPlaceable placeable = placedObjectView.GetComponent<IPlaceable>();
        if (placeable != null && !placeable.CanRemove())
        {
            UIManager.Instance.ShowAlert(rpcParams.Receive.SenderClientId, "Item is used, cannot remove.");
            return;
        }

        string placedObjectTypeSOGuid = placedObjectView.GetPlacedObjectTypeSOGuid();
        foreach (Vector2Int gridPosition in placedObjectView.GetGridPositionList())
        {
            GameManager.Instance.GameData.GridData.RemoveGridObjectData(gridPosition.x, gridPosition.y,
                placedObjectTypeSOGuid, placedObjectView.Origin, placedObjectView.Dir);
        }

        IDestroyable destroyable = placedObjectView.GetComponent<IDestroyable>();
        if (destroyable != null)
            destroyable.DestroySelf();
        else
            placedObjectNetworkObject.Despawn(true);

        GameManager.Instance.ServerAddInventory(placedObjectTypeSOGuid);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void OnObjectPlacedEventClientRpc()
    {
        BuildingPlacementManager.FireOnObjectPlacedEvent();
    }
}
