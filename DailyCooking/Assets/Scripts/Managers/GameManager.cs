using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class GameManager : NetworkPersistentSingleton<GameManager>, IGameManager
{
    public event EventHandler OnPlayerSpawned;
    public event EventHandler OnStateChanged;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 playerSpawnPosition;
    [SerializeField] private UIJoyStick joyStick;

    private GameData gameData;
    private FileDataHandler dataHandler;
    private GameManagerBaseState currentState;

    [Header("Settings")]
    [SerializeField] private string fileName = "GameData";
    
    private GameObject playerGameObject;
    public FileDataHandler DataHandler => dataHandler;

    public GameData GameData { get => gameData; set => gameData = value; }
    public GameManagerBaseState State => currentState;
    protected override void Awake()
    {
        base.Awake();
        dataHandler = new FileDataHandler(
            Application.persistentDataPath,
            fileName
        );
    }
    private void Start()
    {
        SwitchState(new MainMenuState(this));
        MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += Instance_OnPlayerDataNetworkListChanged;
    }

    private void ShowLevelUpPopup(int level)
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UILevelUpPopup,
        new UILevelUpPopup.Param
        {
            reward = new RewardData[]
        { new RewardData(RewardData.RewardType.Coin.ToString(), level * 100) }
        });
    }
    private void Update()
    {
        currentState?.Update();
    }

    public void InitializePlayer()
    {
        // If we are a client, we CANNOT call Spawn(). We must ask the server.
        if (!IsServer)
        {
            RequestSpawnPlayerServerRpc(NetworkManager.Singleton.LocalClientId);
            return;
        }

        // If we ARE the server (or Host), we can spawn it directly
        ExecutePlayerSpawn(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server)]
    private void RequestSpawnPlayerServerRpc(ulong clientId)
    {
        ExecutePlayerSpawn(clientId);
    }

    private void ExecutePlayerSpawn(ulong clientId)
    {
        playerGameObject = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
        playerGameObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
        OnPlayerSpawned?.Invoke(this, EventArgs.Empty);
    }
    public void HidePlayer()
    {
        //playerGameObject.SetActive(false);
    }public void ShowPlayer()
    {
        //playerGameObject.SetActive(true);
    }
    public void DestroyPlayer()
    {
        Destroy(playerGameObject);
    }
    public void NewGame()
    {
        gameData = new GameData();
    }

    public void LoadGame()
    {
        gameData = dataHandler.Load();

        if (gameData == null) NewGame();

        gameData.MenuData.LoadMenuData();
        gameData.RestaurantData.OnLevelChange += SaveGame;
        gameData.RestaurantData.OnExpChange += SaveGame;
        gameData.RestaurantData.OnLevelUp += ShowLevelUpPopup;
        gameData.RestaurantData.OnResourceChange += SaveGame;
        if(gameData.PlayersStats != null)
        {
            foreach(var player in gameData.PlayersStats)            
            {
                player.OnResourceChange += SaveGame;
            }
        }
        gameData.InventoryData.OnInventoryDataChanged += SaveGame;
        gameData.GridData.OnGridDataChanged += SaveGame;
        gameData.TutorialData.OnTutorialDataChanged += SaveGame;
        gameData.MenuData.OnMenuDataChanged += SaveGame;
        gameData.ShopData.OnResourceChange += SaveGame;
        gameData.PostBoxData.OnResourceChange += SaveGame;
    }

    public void SaveGame()
    {
        dataHandler.Save(gameData);
    }

    public void SwitchState(GameManagerBaseState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
        OnStateChanged?.Invoke(this,EventArgs.Empty);
    }
    [Rpc(SendTo.Server)]
    public void UpdateRestaurantNameServerRpc(string name)
    {
        UpdateRestaurantNameClientRpc(name);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateRestaurantNameClientRpc(string name)
    {
        GameData.RestaurantData.UpdateRestaurantName(name);
    }
    [Rpc(SendTo.Server)]
    public void UpdateRestaurantCoinServerRpc(int addCoins)
    {
        UpdateRestaurantCoinClientRpc(addCoins);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateRestaurantCoinClientRpc(int addCoins)
    {
        GameData.RestaurantData.UpdateRestaurantCoins(addCoins);
    }
    [Rpc(SendTo.Server)]
    public void UpdateRestaurantExpServerRpc(int addExp)
    {
        UpdateRestaurantExpClientRpc(addExp);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateRestaurantExpClientRpc(int addExp)
    {
        GameData.RestaurantData.UpdateRestaurantExp(addExp);
    }
    [Rpc(SendTo.Server)]
    public void UpdateRestaurantGemsServerRpc(int addGems)
    {
        UpdateRestaurantGemsClientRpc(addGems);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateRestaurantGemsClientRpc(int addGems)
    {
        GameData.RestaurantData.UpdateRestaurantGems(addGems);
    }
    [Rpc(SendTo.Server)]
    public void AddInventoryDataServerRpc(string guid)
    {
        AddInventoryDataClientRpc(guid);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void AddInventoryDataClientRpc(string guid)
    {
        gameData.AddInventoryData(guid);
    } 
    [Rpc(SendTo.Server)]
    public void RemoveInventoryDataServerRpc(string guid)
    {
        RemoveInventoryDataClientRpc(guid);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void RemoveInventoryDataClientRpc(string guid)
    {
        gameData.RemoveInventoryData(guid);
    }
    [Rpc(SendTo.Server)]
    public void AddDishToMenuServerRpc(int dishIndex)
    {
        AddDishToMenuClientRpc(dishIndex);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void AddDishToMenuClientRpc(int dishIndex)
    {
        var dish = ConfigManager.Instance.ConfigFood.FoodItems.ElementAt(dishIndex);
        if(dish == null) 
        {
            Debug.LogError($"Dish with index {dishIndex} not found in config.");
            return;
        }
        GameData.AddDishToMenu(dish);
    }
    [Rpc(SendTo.Server)]
    public void RemoveDishFromMenuServerRpc(int dishIndex)
    {
        RemoveDishFromMenuClientRpc(dishIndex);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void RemoveDishFromMenuClientRpc(int dishIndex)
    {
        var dish = ConfigManager.Instance.ConfigFood.FoodItems.ElementAt(dishIndex);
        if(dish == null) 
        {
            Debug.LogError($"Dish with index {dishIndex} not found in config.");
            return;
        }
        GameData.RemoveDishFromMenu(dish);
    }
    [Rpc(SendTo.Server)]
    public void UnlockDishServerRpc(string guid)
    {
        UnlockDishClientRpc(guid);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void UnlockDishClientRpc(string guid)
    {
        GameData.UnlockDish(guid);
    }
    [Rpc(SendTo.Server)]
    public void PurchaseUpgradeServerRpc(int upgradeIndex)
    {
        PurchaseUpgradeClientRpc(upgradeIndex);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void PurchaseUpgradeClientRpc(int upgradeIndex)
    {
        var upgrade = ConfigManager.Instance.ConfigUpgrade.Upgrades.ElementAt(upgradeIndex);
        if(upgrade == null) 
        {
            Debug.LogError($"Upgrade with index {upgradeIndex} not found in config.");
            return;
        }
        GameData.PurchaseUpgrade(upgrade);
    }
    [Rpc(SendTo.Server)]
    public void UpdatePostBoxDataServerRpc(string kitchenObjectSOGuid)
    {
        GridBuildingSystem.Instance.PostBox.AddPackage(kitchenObjectSOGuid);
    }
    [Rpc(SendTo.Server)]
    public void RemovePostBoxDataServerRpc(string kitchenObjectSOGuid)
    {
        RemovePostBoxDataClientRpc(kitchenObjectSOGuid);
    } 
    [Rpc(SendTo.ClientsAndHost)]
    private void RemovePostBoxDataClientRpc(string kitchenObjectSOGuid)
    {
        GameData.PostBoxData.RemovePackage(kitchenObjectSOGuid);
    }

    private void Instance_OnPlayerDataNetworkListChanged(object sender, EventArgs e)
    {
        gameData.TryAddPlayerStats(MultiplayerManager.Instance.GetLatestPlayerData().playerId.ToString());
    }
    public void HideJoyStick()
    {
       joyStick.gameObject.SetActive(false);
    }
    public void ShowJoyStick()
    {
        joyStick.gameObject.SetActive(true);
    }
}
