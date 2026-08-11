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

    public Action<NetworkObject> OnSpawnRequestCompleted;
    public event EventHandler OnStateChanged;
    public Action OnSpawnKitchenObjectCompleted;

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
    [SerializeField] private PopupDatabase popupDatabase;

    private State state;

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

    public State CurrentState => state;

    public Dictionary<string, KitchenObjectSO> KitchenObjectSODic { get => kitchenObjectSODic; set => kitchenObjectSODic = value; }
    public RecipeDatabaseSO RecipeDatabase { get => recipeDatabase; }
    public PopupDatabase PopupDatabase { get => popupDatabase; }

    protected override void Awake()
    {
        base.Awake();
        state = State.Editing;
        unlockIngredient = new List<KitchenObjectSO>();
        kitchenObjectSODic = new Dictionary<string, KitchenObjectSO>();
        foreach (var kitchenObjectSO in kitchenObjectSOList)
        {
            kitchenObjectSODic[kitchenObjectSO.Guid] = kitchenObjectSO;
        }
        recipeDatabase?.Initialize();
        popupDatabase?.Initialize();

        if (IsServer && KitchenObjectPool.Instance != null)
        {
            KitchenObjectPool.Instance.PrewarmPools(kitchenObjectSOList);
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        BotManager.Instance.Initialize();
        ChangeState(State.Open);
        BotManager.Instance.StartSpawnBot();
    }

    public void OnDestroy()
    {
        unlockIngredient.Clear();
    }
    public void Start()
    {
    }
    public void Init()
    {
        unlockIngredient.Clear();
    }
    public void ChangeState(State newState)
    {
        state = newState;
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
        return state == State.Open;
    }
    public bool IsClosing()
    {
        return state == State.Close;
    }
    public bool IsEditing()
    {
        return state == State.Editing;

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
    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent, int index = 0)
    {
        SpawnKitchenObjectServerRpc(kitchenObjectSO.Guid, kitchenObjectParent.GetNetworkObject(), index);
    }
    [Rpc(SendTo.Server)]
    private void SpawnKitchenObjectServerRpc(string kitchenObjectSOGuid, NetworkObjectReference networkObjectReference, int index = 0)
    {
        KitchenObjectSO kitchenObjectSO = kitchenObjectSODic[kitchenObjectSOGuid];

        networkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponentInChildren<CookingTool>();
        if (kitchenObjectParent == null)
        {
            kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();
        }

        if (kitchenObjectParent.HasKitchenObject())
        {
            //Parent already spawn an object
            return;
        }

        GameObject kitchenObjectGO;
        if (KitchenObjectPool.Instance != null && KitchenObjectPool.Instance.HasPool(kitchenObjectSOGuid))
        {
            kitchenObjectGO = KitchenObjectPool.Instance.GetKitchenObject(kitchenObjectSOGuid);
        }
        else
        {
            kitchenObjectGO = Instantiate(kitchenObjectSO.prefab).gameObject;
        }

        Transform kitchenObjectTransform = kitchenObjectGO.transform;
        NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
        if (!kitchenObjectNetworkObject.IsSpawned)
        {
            kitchenObjectNetworkObject.Spawn(true);
        }

        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();

        kitchenObject.SetKitchenObjectParent(kitchenObjectParent, index);
        OnSpawnKitchenObjectCompleted?.Invoke();
    }

    [Rpc(SendTo.Server)]
    public void CreatePlacedObjectViewServerRpc(Vector3 worldPosition, string placeObjectTypeSOGuid, 
        Vector2Int origin, Dir dir, ulong targetClientId, bool isPreview)
    {
        if (PrefabSpawnService.Instance != null)
        {
            PrefabSpawnService.Instance.SpawnPlacedObjectDirect(worldPosition, placeObjectTypeSOGuid, origin, dir, targetClientId, isPreview);
            PrefabSpawnService.Instance.OnSpawnRequestCompleted += HandleSpawnRequestCompleted;
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
        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, worldPosition, Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0), GridBuildingSystem.Instance.Container).transform;
        var networkObject = placedObjectTransform.GetComponent<NetworkObject>();
        PlacedObjectView placedObjectView = networkObject.GetComponent<PlacedObjectView>();
        placedObjectView.Intialize(placeObjectTypeSOGuid, origin, dir, isPreview);

        networkObject.Spawn();
        networkObject.ChangeOwnership(targetClientId);

        NotifyClientOfSpawnClientRpc(networkObject, RpcTarget.Single(targetClientId, RpcTargetUse.Temp));
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
        DestroyPlacedObjectServerRpc(networkObject);
    }
    [Rpc(SendTo.Server)]
    private void DestroyPlacedObjectServerRpc(NetworkObjectReference placedObjectNetworkObjectReference)
    {
        placedObjectNetworkObjectReference.TryGet(out NetworkObject placedObjectNetworkObject);
        if (placedObjectNetworkObject == null)
        {
            //This object is already destroyed
            return;
        }
        placedObjectNetworkObject.GetComponent<IDestroyable>().DestroySelf();
    }

}
