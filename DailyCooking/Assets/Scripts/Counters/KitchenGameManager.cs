using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameManager : NetworkPersistentSingleton<KitchenGameManager>
{
    private const string PLAYER_DAY = "PlayerDay";
    private const float COUNTDOWN_TO_START_TIMER_INITIAL = 3f;
    private const float GAME_PLAYING_TIMER_MAX_INITIAL = 20f;
    private const int PLAYER_EXP_MULTIPLIER = 10;
    private const float TIME_SCALE_PAUSED = 0f;
    private const float TIME_SCALE_UNPAUSED = 1f;

    public event Action<NetworkObject> OnSpawnRequestCompleted;
    public event EventHandler OnStateChanged;

    public enum State
    {
        Editing,
        Open,
        Close
    }
    [SerializeField] private long earnGoalMultiply = 1000;
    [SerializeField] private long serveGoalMultiply = 10;
    [SerializeField] private long gamePlayingTimeMultiply = 60;
    [SerializeField] private List<KitchenObjectSO> kitchenObjectSOList;
    [SerializeField] private RecipeDatabaseSO recipeDatabase;

    private readonly NetworkVariable<State> state = new NetworkVariable<State>(
        State.Editing, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private long earnGoal;
    private long serveGoal;
    private long earnCount;
    private long serveCount;
    private List<KitchenObjectSO> unlockIngredient;
    private Dictionary<string,KitchenObjectSO> kitchenObjectSODic;

    public long EarnCount => earnCount;
    public long ServeCount => serveCount;
    public long EarnGoal { get => earnGoal; set => earnGoal = value; }
    public long ServeGoal { get => serveGoal; set => serveGoal = value; }

    public State CurrentState => state.Value;

    public Dictionary<string, KitchenObjectSO> KitchenObjectSODic { get => kitchenObjectSODic; set => kitchenObjectSODic = value; }
    public RecipeDatabaseSO RecipeDatabase { get => recipeDatabase; }

    protected override void Awake()
    {
        base.Awake();
        unlockIngredient = new List<KitchenObjectSO>();
        kitchenObjectSODic = new Dictionary<string, KitchenObjectSO>();
        foreach (var kitchenObjectSO in kitchenObjectSOList)
        {
            kitchenObjectSODic[kitchenObjectSO.Guid] = kitchenObjectSO;
        }
        recipeDatabase.Initialize();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        state.OnValueChanged += State_OnValueChanged;
        BotManager.Instance.Initialize();
        if (IsServer)
            state.Value = State.Open;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
        BotManager.Instance.StartSpawnBot();
        if (PrefabSpawnService.Instance != null)
        {
            PrefabSpawnService.Instance.OnSpawnRequestCompleted -= HandleSpawnRequestCompleted;
            PrefabSpawnService.Instance.OnSpawnRequestCompleted += HandleSpawnRequestCompleted;
        }
    }

    public override void OnNetworkDespawn()
    {
        state.OnValueChanged -= State_OnValueChanged;
        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        unlockIngredient.Clear();
        base.OnDestroy();
    }
    public void Start()
    {
    }
    public void Init()
    {
        unlockIngredient.Clear();
    }
    // Any player may open or close the restaurant; the server owns the value.
    public void ChangeState(State newState)
    {
        if (IsServer)
            state.Value = newState;
        else
            ChangeStateServerRpc(newState);
    }
    [Rpc(SendTo.Server)]
    private void ChangeStateServerRpc(State newState)
    {
        if (newState != State.Open && newState != State.Close) return;
        state.Value = newState;
    }
    private void State_OnValueChanged(State previousValue, State newValue)
    {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }
    public void CollectCash(int cash, int exp)
    {
        GameManager.Instance.UpdateRestaurantCoinServerRpc(cash);
        GameManager.Instance.UpdateRestaurantExpServerRpc(exp);

    }

    private void Update()
    {

    }
    public bool IsOpening()
    {
        return state.Value == State.Open;
    }
    public bool IsClosing()
    {
        return state.Value == State.Close;
    }
    public bool IsEditing()
    {
        return state.Value == State.Editing;

    }
    public FoodSO GetUnlockedFood()
    {
        if (GameManager.Instance.GameData.MenuData.menuDished.Count == 0)
            return null;
        else
            return GameManager.Instance.GameData.
                MenuData.menuDished[UnityEngine.Random.Range(0,
                    GameManager.Instance.GameData.MenuData.menuDished.Count)];
    }
    public int GetFoodIndex(FoodSO foodSO)
    {
        return GameManager.Instance.GameData.MenuData.menuDished.IndexOf(foodSO);
    }
    public FoodSO GetFoodByIndex(int index)
    {
        if (index < 0 || index >= GameManager.Instance.GameData.MenuData.menuDished.Count)
            return null;
        return GameManager.Instance.GameData.MenuData.menuDished[index];
    }
    public KitchenObjectSO GetKitchenObjectSOByGuid(string guid)
    {
        if (kitchenObjectSODic.ContainsKey(guid))
            return kitchenObjectSODic[guid];
        else
            return null;
    }
    // Server only. Spawns the object already parented, so clients never see it unparented.
    // Returns null when the parent slot is taken or the spawn is not allowed.
    public KitchenObject SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent, int index = 0)
    {
        if (!IsServer)
        {
            Debug.LogWarning("KitchenGameManager.SpawnKitchenObject must run on the server.");
            return null;
        }
        if (kitchenObjectSO == null || kitchenObjectSO.prefab == null || kitchenObjectParent == null) return null;
        if (!kitchenObjectSODic.ContainsKey(kitchenObjectSO.Guid)) return null;
        if (kitchenObjectParent.HasKitchenObject(index)) return null;

        KitchenObject kitchenObject = Instantiate(kitchenObjectSO.prefab).GetComponent<KitchenObject>();
        if (!kitchenObject.InitializeParent(kitchenObjectParent, index))
        {
            Destroy(kitchenObject.gameObject);
            return null;
        }
        kitchenObject.NetworkObject.Spawn(true);
        return kitchenObject;
    }


    [Rpc(SendTo.Server)]
    public void CreatePlacedObjectViewServerRpc(Vector3 worldPosition, string placeObjectTypeSOGuid,
        Vector2Int origin, Dir dir, ulong targetClientId, bool isPreview)
    {
        if (PrefabSpawnService.Instance != null)
        {
            PrefabSpawnService.Instance.OnSpawnRequestCompleted -= HandleSpawnRequestCompleted;
            PrefabSpawnService.Instance.OnSpawnRequestCompleted += HandleSpawnRequestCompleted;
            PrefabSpawnService.Instance.SpawnPlacedObjectDirect(worldPosition, placeObjectTypeSOGuid, origin, dir, targetClientId, isPreview);
        }
        else
        {
            SpawnPlacedObjectFallback(worldPosition, placeObjectTypeSOGuid, origin, dir, targetClientId, isPreview);
        }
    }

    private void SpawnPlacedObjectFallback(Vector3 worldPosition, string placeObjectTypeSOGuid,
        Vector2Int origin, Dir dir, ulong targetClientId, bool isPreview)
    {
        PlacedObjectTypeSO placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(placeObjectTypeSOGuid);
        if (placedObjectTypeSO == null) return;
        if (placedObjectTypeSO.prefab == null)
        {
            Debug.LogError($"KitchenGameManager: Missing prefab for PlacedObjectTypeSO '{placedObjectTypeSO.name}' Guid={placeObjectTypeSOGuid}", placedObjectTypeSO);
            return;
        }
        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, ResolveToolSlotPosition(worldPosition, placedObjectTypeSO, origin), Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0), GridBuildingSystem.Instance.Container).transform;
        var networkObject = placedObjectTransform.GetComponent<NetworkObject>();
        PlacedObjectView placedObjectView = networkObject.GetComponent<PlacedObjectView>();
        placedObjectView.Intialize(placeObjectTypeSOGuid, origin, dir, isPreview);

        networkObject.Spawn();
        networkObject.ChangeOwnership(targetClientId);

        NotifyClientOfSpawnClientRpc(networkObject, RpcTarget.Single(targetClientId, RpcTargetUse.Temp));
    }

    private static Vector3 ResolveToolSlotPosition(Vector3 worldPosition, PlacedObjectTypeSO placedObjectTypeSO, Vector2Int origin)
    {
        if (placedObjectTypeSO != null && placedObjectTypeSO.isTool &&
            GridBuildingSystem.Instance != null && GridBuildingSystem.Instance.GridManager != null &&
            ToolSlotResolver.TryFindUnderlyingCounter(GridBuildingSystem.Instance.GridManager.Grid, placedObjectTypeSO, origin, out PlacedObjectView counterView) &&
            ToolSlotResolver.TryGetToolSlotPosition(counterView, origin, out Vector3 slotPos))
        {
            return slotPos;
        }
        return worldPosition;
    }

    private void HandleSpawnRequestCompleted(NetworkObject spawnedObject)
    {
        if (PrefabSpawnService.Instance != null)
        {
            PrefabSpawnService.Instance.OnSpawnRequestCompleted -= HandleSpawnRequestCompleted;
        }
        OnSpawnRequestCompleted?.Invoke(spawnedObject);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void NotifyClientOfSpawnClientRpc(NetworkObjectReference spawnedObjectRef, RpcParams rpcParams)
    {
        if (spawnedObjectRef.TryGet(out NetworkObject netObj))
        {
            OnSpawnRequestCompleted?.Invoke(netObj);
        }
    }
    public void DestroyPlacedObject(NetworkObject networkObject)
    {
        if (networkObject == null) return;
        if (!networkObject.IsSpawned)
        {
            // Preview ghost not yet spawned or already despawned on client - destroy locally
            if (networkObject.gameObject != null)
            {
                UnityEngine.Object.Destroy(networkObject.gameObject);
            }
            return;
        }
        DestroyPlacedObjectServerRpc(networkObject);
    }
    [Rpc(SendTo.Server)]
    private void DestroyPlacedObjectServerRpc(NetworkObjectReference placedObjectNetworkObjectReference)
    {
        if (!placedObjectNetworkObjectReference.TryGet(out NetworkObject placedObjectNetworkObject) || placedObjectNetworkObject == null)
        {
            //This object is already destroyed or invalid reference
            return;
        }
        var destroyable = placedObjectNetworkObject.GetComponent<IDestroyable>();
        if (destroyable != null)
        {
            destroyable.DestroySelf();
        }
        else
        {
            // Fallback for PlacedObjectView / preview ghosts that don't implement IDestroyable
            if (placedObjectNetworkObject.IsSpawned)
            {
                placedObjectNetworkObject.Despawn(true);
            }
            else if (placedObjectNetworkObject.gameObject != null)
            {
                UnityEngine.Object.Destroy(placedObjectNetworkObject.gameObject);
            }
        }
    }

}
