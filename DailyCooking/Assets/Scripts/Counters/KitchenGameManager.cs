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
    [SerializeField] private List<CuttingRecipeSO> cuttingRecipeSOList;
    [SerializeField] private List<FryingRecipeSO> fryingRecipeSOList;

    private State state;

    private long earnGoal;
    private long serveGoal;
    private long earnCount;
    private long serveCount;
    private List<KitchenObjectSO> unlockIngredient;

    public long EarnCount => earnCount;
    public long ServeCount => serveCount;
    public long EarnGoal { get => earnGoal; set => earnGoal = value; }
    public long ServeGoal { get => serveGoal; set => serveGoal = value; }

    public List<CuttingRecipeSO> CuttingRecipeSOList { get => cuttingRecipeSOList; set => cuttingRecipeSOList = value; }
    public List<FryingRecipeSO> FryingRecipeSOList { get => fryingRecipeSOList; set => fryingRecipeSOList = value; }
    public State CurrentState => state;
    protected override void Awake()
    {
        base.Awake();
        state = State.Editing;
        unlockIngredient = new List<KitchenObjectSO>();

        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.buildIndex == 0)
            return;
        BotManager.Instance.Initialize();
        ChangeState(State.Open);
        if (GameManager.Instance.GameData.TutorialData.HasPlayedFirstTime)
        {
            BotManager.Instance.StartSpawnBot();
        }
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
        foreach (var counterController in CounterModules.Instance.BaseCounterControllers)
        {
            if (counterController == null)
                continue;
            AddUnlockIngredient(counterController);
        }
    }
    public void ChangeState(State newState)
    {
        state = newState;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }
    public void CollectCash(int cash, int exp)
    {
        GameManager.Instance.GameData.RestaurantData.UpdatePlayerCoins(cash);
        GameManager.Instance.GameData.RestaurantData.UpdatePlayerExp(exp);

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


    public void AddUnlockIngredient(BaseCounterController counterController)
    {
        IContainerCounter containerCounter = counterController as IContainerCounter;
        if (containerCounter == null) return;

        List<KitchenObjectSO> kitchenObjectSOList = containerCounter.GetContainerKitchenObjectType();
        if (kitchenObjectSOList != null)
        {
            foreach (var item in kitchenObjectSOList)
            {
                if (!unlockIngredient.Contains(item))
                    unlockIngredient.Add(item);
            }
        }
        GetUnlockFood();

    }

    private void GetUnlockFood()
    {
        foreach (var foodSO in ConfigManager.Instance.ConfigFood.FoodItems)
        {
            bool isUnlocked = true;
            foreach (var ingredient in foodSO.kitchenObjectSOList)
            {
                isUnlocked = isUnlocked && IsIngredientUnlocked(ingredient);
                if (!isUnlocked)
                {
                    break;
                }
            }
            if (isUnlocked)
            {
                GameManager.Instance.GameData.MenuData.AddUnlockedDish(foodSO);

            }
        }
    }

    private bool IsIngredientUnlocked(KitchenObjectSO ingredient)
    {
        foreach (var unlockIngredient in unlockIngredient)
        {
            if (unlockIngredient == ingredient)
            {
                return true;
            }
            else
            {
                var cuttingRecipeSO = KitchenGameManager.Instance.CuttingRecipeSOList.Find(x => x.input == unlockIngredient &&
                                                                       x.output == ingredient);
                var fryingRecipeSO = KitchenGameManager.Instance.FryingRecipeSOList.Find(x => x.input == unlockIngredient &&
                                                                       x.output == ingredient);
                if (cuttingRecipeSO != null || fryingRecipeSO != null)
                    return true;
            }

        }
        return false;
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
    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO)
    {
        return kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }
    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex)
    {
        return kitchenObjectSOList[kitchenObjectSOIndex];

    }
    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent, int index = 0)
    {
        SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSO), kitchenObjectParent.GetNetworkObject(), index);
    }
    [Rpc(SendTo.Server)]
    private void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference networkObjectReference, int index = 0)
    {
        KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        networkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        if (kitchenObjectParent.HasKitchenObject())
        {
            //Parent already spawn an object
            return;
        }

        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);

        NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
        kitchenObjectNetworkObject.Spawn(true);

        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();

        kitchenObject.SetKitchenObjectParent(kitchenObjectParent, index);
    }

    public void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }

    [Rpc(SendTo.Server)]
    private void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);

        if (kitchenObjectNetworkObject == null)
        {
            //This object is already destroyed
            return;
        }

        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        kitchenObject.ClearKitchenObjectOnParentClientRpc();
        kitchenObject.DestroySelf();
    }
    [Rpc(SendTo.Server)]
    public void CreatePlacedObjectViewServerRpc(Vector3 worldPosition, string placeObjectTypeSOGuid, 
        Vector2Int origin, Dir dir, ulong targetClientId, bool isPreview)
    {
        PlacedObjectTypeSO placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(placeObjectTypeSOGuid);
        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, worldPosition, Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0), GridBuildingSystem.Instance.Container).transform;
        var networkObject = placedObjectTransform.GetComponent<NetworkObject>();
        PlacedObjectView placedObjectView = networkObject.GetComponent<PlacedObjectView>();
        placedObjectView.GetComponent<IPlaceable>().IsPlaced.Value = false;
        placedObjectView.Intialize(placeObjectTypeSOGuid, origin, dir,isPreview);

        networkObject.Spawn();
        networkObject.ChangeOwnership(targetClientId);

        Debug.Log("Object is created on server " + placeObjectTypeSOGuid +" pass to target "+ targetClientId);
        NotifyClientOfSpawnClientRpc(networkObject, RpcTarget.Single(targetClientId,RpcTargetUse.Temp));

    }
    [Rpc(SendTo.SpecifiedInParams)]
    private void NotifyClientOfSpawnClientRpc(NetworkObjectReference spawnedObjectRef, 
        RpcParams rpcParams)
    {
        if (spawnedObjectRef.TryGet(out NetworkObject netObj))
        {
            //Debug.Log($"Client received my new object: {netObj.name}");
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
