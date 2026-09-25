using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class GameManager : NetworkPersistentSingleton<GameManager>, IGameManager
{
    private const string SavedDataFileName = "SavedData";    
    private const float SAVE_INTERVAL = 1f;
    private const int MAX_RESTAURANT_NAME_LENGTH = 24;
    private const char CUSTOMIZATION_ENTRY_SEPARATOR = ';';
    private const char CUSTOMIZATION_VALUE_SEPARATOR = '=';

    public event EventHandler OnPlayerSpawned;
    public event EventHandler OnStateChanged;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 playerSpawnPosition;
    [SerializeField] private UIJoyStick joyStick;

    private List<SavedData> savedDataList = new List<SavedData>();
    private GameData gameData;
    private FileDataHandler dataHandler;
    private SavedDataHandler savedDataHandler;
    private GameManagerBaseState currentState;
    private float lastSaveTime;
    private bool isSaveDirty;

    [Header("Settings")]
    [SerializeField] private string fileName = "GameData";
    
    private GameObject playerGameObject;
    public FileDataHandler DataHandler => dataHandler;

    public GameData GameData => gameData;
    public GameManagerBaseState State => currentState;

    public List<SavedData> SavedDataList { get => savedDataList; set => savedDataList = value; }
    protected override void Awake()
    {
        base.Awake();
        savedDataHandler = new SavedDataHandler(
            Application.persistentDataPath,
            SavedDataFileName
        );
        SavedDataList = savedDataHandler.Load();
        if (SavedDataList == null)
        {
            SavedDataList = new List<SavedData>();
        }
    }
    private void Start()
    {
        SwitchState(new MainMenuState(this));
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += Instance_OnPlayerDataNetworkListChanged;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Gem purchases wait for a hosted restaurant to credit; pick up any left pending.
        if (IsServer && gameData != null && IAPManager.Instance != null)
            IAPManager.Instance.RetryPendingPurchases();
    }

    public override void OnDestroy()
    {
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged -= Instance_OnPlayerDataNetworkListChanged;
        UnsubscribeSaveEvents();
        base.OnDestroy();
    }

    private void SubscribeSaveEvents()
    {
        if (gameData == null) return;
        UnsubscribeSaveEvents();
        gameData.RestaurantData.OnLevelChange += SaveGame;
        gameData.RestaurantData.OnExpChange += SaveGame;
        gameData.RestaurantData.OnLevelUp += ShowLevelUpPopup;
        gameData.RestaurantData.OnResourceChange += SaveGame;
        if (gameData.PlayersStats != null)
        {
            foreach (var player in gameData.PlayersStats)
                player.OnResourceChange += SaveGame;
        }
        gameData.OnPlayerStatsAdded += GameData_OnPlayerStatsAdded;
        gameData.UpgradeData.OnMenuDataChanged += SaveGame;
        gameData.CosmeticData.OnCosmeticDataChanged += SaveGame;
        gameData.InventoryData.OnInventoryDataChanged += SaveGame;
        gameData.GridData.OnGridDataChanged += SaveGame;
        gameData.TutorialData.OnTutorialDataChanged += SaveGame;
        gameData.MenuData.OnMenuDataChanged += SaveGame;
        gameData.ShopData.OnResourceChange += SaveGame;
        gameData.PostBoxData.OnResourceChange += SaveGame;
    }

    private void UnsubscribeSaveEvents()
    {
        if (gameData == null) return;
        gameData.RestaurantData.OnLevelChange -= SaveGame;
        gameData.RestaurantData.OnExpChange -= SaveGame;
        gameData.RestaurantData.OnLevelUp -= ShowLevelUpPopup;
        gameData.RestaurantData.OnResourceChange -= SaveGame;
        if (gameData.PlayersStats != null)
        {
            foreach (var player in gameData.PlayersStats)
                player.OnResourceChange -= SaveGame;
        }
        gameData.OnPlayerStatsAdded -= GameData_OnPlayerStatsAdded;
        gameData.UpgradeData.OnMenuDataChanged -= SaveGame;
        gameData.CosmeticData.OnCosmeticDataChanged -= SaveGame;
        gameData.InventoryData.OnInventoryDataChanged -= SaveGame;
        gameData.GridData.OnGridDataChanged -= SaveGame;
        gameData.TutorialData.OnTutorialDataChanged -= SaveGame;
        gameData.MenuData.OnMenuDataChanged -= SaveGame;
        gameData.ShopData.OnResourceChange -= SaveGame;
        gameData.PostBoxData.OnResourceChange -= SaveGame;
    }

    private void GameData_OnPlayerStatsAdded(PlayerStats playerStats)
    {
        playerStats.OnResourceChange += SaveGame;
        SaveGame();
    }

    // Display only: the server already granted the reward (see ServerAddExp).
    private void ShowLevelUpPopup(int level)
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UILevelUpPopup,
        new UILevelUpPopup.Param
        {
            reward = new RewardData[]
        { new RewardData(RewardData.RewardType.Coin.ToString(), EconomyRules.GetLevelUpRewardCoins(level)) }
        });
    }

    // A joining client replaces its GameData with the host's snapshot.
    public void ReplaceGameDataFromHost(GameData hostGameData)
    {
        if (gameData != null)
            gameData.RestaurantData.OnLevelUp -= ShowLevelUpPopup;
        gameData = hostGameData;
        if (gameData != null)
        {
            gameData.MenuData.LoadMenuData();
            gameData.RestaurantData.OnLevelUp += ShowLevelUpPopup;
        }
    }
    private void Update()
    {
        currentState?.Update();
        FlushPendingSave();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            SaveGameImmediate();
    }

    private void OnApplicationQuit()
    {
        SaveGameImmediate();
    }

    public void InitializePlayer()
    {
        // If we are a client, we CANNOT call Spawn(). We must ask the server.
        if (!IsServer)
        {
            RequestSpawnPlayerServerRpc();
            return;
        }

        // If we ARE the server (or Host), we can spawn it directly
        ExecutePlayerSpawn(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server)]
    private void RequestSpawnPlayerServerRpc(RpcParams rpcParams = default)
    {
        ExecutePlayerSpawn(rpcParams.Receive.SenderClientId);
    }

    // One avatar per client: a repeated request (e.g. from a re-run initialization) is ignored.
    private void ExecutePlayerSpawn(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out NetworkClient client) && client.PlayerObject != null)
            return;
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
    public bool NewGame(string gameDataName, string password)
    {
        if (!IsValidGameDataName(gameDataName))
        {
            UIManager.Instance.ShowAlertMessage("Invalid restaurant name.");
            return false;
        }
        if (SavedDataList.Any(s => s.GameDataName == gameDataName))
        {
            // Creating it again would overwrite the existing save file.
            UIManager.Instance.ShowAlertMessage("A restaurant with this name already exists.");
            return false;
        }
        ClearActiveSave();
        gameData = GameData.CreateNew();
        dataHandler = new FileDataHandler(
            Application.persistentDataPath,
            fileName + "_" + gameDataName
        );
        SavedDataList.Add(new SavedData(gameDataName, password));
        savedDataHandler.Save(SavedDataList);
        SubscribeSaveEvents();
        SaveGameImmediate();
        return true;
    }

    public bool LoadGame(string gameDataName, string password)
    {
        var savedData = SavedDataList.FirstOrDefault(s => s.GameDataName == gameDataName && s.Password == password);
        if (savedData == null)
        {
            Debug.LogError("Invalid game data name or password.");
            UIManager.Instance.ShowAlertMessage("Invalid restaurant name or password.");
            return false;
        }
        ClearActiveSave();
        var handler = new FileDataHandler(
            Application.persistentDataPath,
            fileName + "_" + gameDataName
        );
        var loadedData = handler.Load();
        if (loadedData == null)
        {
            Debug.LogError($"Save file for '{gameDataName}' is missing or unreadable.");
            UIManager.Instance.ShowAlertMessage("This restaurant's save could not be loaded.");
            return false;
        }
        dataHandler = handler;
        gameData = loadedData;
        gameData.Migrate();
        gameData.MenuData.LoadMenuData();
        SubscribeSaveEvents();
        return true;
    }
    public void DeleteSavedData(SavedData savedData)
    {
        SavedDataList.Remove(savedData);
        savedDataHandler.Save(SavedDataList);
    }

    // Flushes the active save (host only) and forgets its file, so joining someone else's
    // restaurant can never write their data over this device's save file.
    // discardData=false keeps GameData readable while the game scene is still tearing down.
    public void ClearActiveSave(bool discardData = true)
    {
        SaveGameImmediate();
        UnsubscribeSaveEvents();
        dataHandler = null;
        isSaveDirty = false;
        if (discardData)
            gameData = null;
    }

    private static bool IsValidGameDataName(string gameDataName)
    {
        return !string.IsNullOrWhiteSpace(gameDataName) &&
            gameDataName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0;
    }

    // Marks the save dirty; FlushPendingSave writes it at most once per SAVE_INTERVAL,
    // so bursts of data events cost one write and the last change is never dropped.
    public void SaveGame()
    {
        if (!CanSave()) return;
        isSaveDirty = true;
    }

    public void SaveGameImmediate()
    {
        if (!CanSave()) return;
        WriteSave();
    }

    private void FlushPendingSave()
    {
        if (!isSaveDirty || Time.unscaledTime - lastSaveTime < SAVE_INTERVAL) return;
        if (!CanSave())
        {
            isSaveDirty = false;
            return;
        }
        WriteSave();
    }

    private void WriteSave()
    {
        isSaveDirty = false;
        lastSaveTime = Time.unscaledTime;
        try
        {
            dataHandler.Save(gameData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game data: {e}");
        }
    }

    // Only the host owns the restaurant save.
    private bool CanSave()
    {
        if (dataHandler == null || gameData == null) return false;
        var networkManager = NetworkManager.Singleton;
        return networkManager == null || !networkManager.IsListening || networkManager.IsServer;
    }

    public void SwitchState(GameManagerBaseState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
        OnStateChanged?.Invoke(this,EventArgs.Empty);
    }
    private void Instance_OnPlayerDataNetworkListChanged(object sender, EventArgs e)
    {
        if (gameData == null) return;
        // Entries arrive with an empty id and get it later, and several clients can join
        // at once, so check every connected player instead of only the latest entry.
        // Clients mirror new entries locally; only the host's copy is saved.
        foreach (var playerData in MultiplayerManager.Instance.GetAllPlayerData())
        {
            string playerId = playerData.playerId.ToString();
            if (!string.IsNullOrEmpty(playerId))
                gameData.TryAddPlayerStats(playerId);
        }
    }
    public void HideJoyStick()
    {
       joyStick.gameObject.SetActive(false);
    }
    public void ShowJoyStick()
    {
        joyStick.gameObject.SetActive(true);
    }

    // ---- Economy ----
    // Restaurant economy and shared restaurant data. Clients only send intents ("buy shop item 7");
    // the server looks prices up in config, checks the balance, applies the change to its own
    // GameData (the save) and mirrors it to the clients with SendTo.NotServer RPCs.
    #region Balances (server only)

    public void ServerAddCoins(int amount)
    {
        if (!IsServer || GameData == null || amount <= 0) return;
        ApplyCoins(amount);
    }

    public void ServerAddGems(int amount)
    {
        if (!IsServer || GameData == null || amount <= 0) return;
        ApplyGems(amount);
    }

    // Also pays the level-up reward, so it is granted exactly once, by the server.
    public void ServerAddExp(int amount)
    {
        if (!IsServer || GameData == null || amount <= 0) return;
        int levelBefore = GameData.RestaurantData.Level;
        GameData.RestaurantData.UpdateRestaurantExp(amount);
        UpdateRestaurantExpClientRpc(amount);
        int levelAfter = GameData.RestaurantData.Level;
        if (levelAfter > levelBefore)
            ApplyCoins(EconomyRules.GetLevelUpRewardCoins(levelAfter));
    }

    private bool TrySpendCoins(int cost)
    {
        if (cost < 0 || GameData.RestaurantData.Coins < cost) return false;
        if (cost > 0) ApplyCoins(-cost);
        return true;
    }

    private bool TrySpendGems(int cost)
    {
        if (cost < 0 || GameData.RestaurantData.Gems < cost) return false;
        if (cost > 0) ApplyGems(-cost);
        return true;
    }

    private void ApplyCoins(int delta)
    {
        GameData.RestaurantData.UpdateRestaurantCoins(delta);
        UpdateRestaurantCoinClientRpc(delta);
    }

    private void ApplyGems(int delta)
    {
        GameData.RestaurantData.UpdateRestaurantGems(delta);
        UpdateRestaurantGemsClientRpc(delta);
    }

    // Changes that happened before a joining client's snapshot arrived are already in it.
    [Rpc(SendTo.NotServer)]
    private void UpdateRestaurantCoinClientRpc(int delta)
    {
        GameData?.RestaurantData.UpdateRestaurantCoins(delta);
    }
    [Rpc(SendTo.NotServer)]
    private void UpdateRestaurantGemsClientRpc(int delta)
    {
        GameData?.RestaurantData.UpdateRestaurantGems(delta);
    }
    [Rpc(SendTo.NotServer)]
    private void UpdateRestaurantExpClientRpc(int amount)
    {
        GameData?.RestaurantData.UpdateRestaurantExp(amount);
    }

    #endregion

    #region Purchases

    [Rpc(SendTo.Server)]
    public void BuyShopItemServerRpc(int shopItemId, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (GameData == null || ConfigManager.Instance == null) return;
        ConfigShopItem item = ConfigManager.Instance.ConfigShop.ShopItems.Find(x => x.Id == shopItemId);
        if (item == null || (item.Type != ShopItemType.Item && item.Type != ShopItemType.Ingredient)) return;
        if (item.UnlockLevel > GameData.RestaurantData.Level)
        {
            AlertClient(sender, $"Reach Level {item.UnlockLevel} to buy this item.");
            return;
        }
        if (item.Type == ShopItemType.Ingredient && GridBuildingSystem.Instance?.PostBox == null)
            return;
        if (!TrySpendCoins(item.Price))
        {
            AlertClient(sender, "Not enough money to buy this item.");
            return;
        }

        foreach (ShopReward reward in item.Rewards)
        {
            if (item.Type == ShopItemType.Item)
            {
                for (int i = 0; i < reward.Amount; i++)
                    ServerAddInventory(reward.Guid);
            }
            else if (KitchenGameManager.Instance.GetKitchenObjectSOByGuid(reward.Guid) != null)
            {
                GridBuildingSystem.Instance.PostBox.AddPackage(reward.Guid);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void ExchangeCurrencyServerRpc(ShopItemType rewardType, int gemCost, int rewardAmount, RpcParams rpcParams = default)
    {
        if (GameData == null || ShopManager.Instance == null) return;
        if (ShopManager.Instance.FindExchangeOffer(rewardType, gemCost, rewardAmount) == null) return;
        if (!TrySpendGems(gemCost))
        {
            AlertClient(rpcParams.Receive.SenderClientId, "Not enough gems to buy this currency.");
            return;
        }
        if (rewardType == ShopItemType.Coin)
            ApplyCoins(rewardAmount);
        else if (rewardType == ShopItemType.Gem)
            ApplyGems(rewardAmount);
    }

    [Rpc(SendTo.Server)]
    public void UnlockDishServerRpc(string foodGuid, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        FoodSO dish = FindDish(foodGuid);
        if (GameData == null || dish == null || GameData.MenuData.unlockedDishes.Contains(dish)) return;
        if (GameData.RestaurantData.Level < dish.unlockLevel)
        {
            AlertClient(sender, $"Reach Level {dish.unlockLevel} to unlock this dish.");
            return;
        }
        if (!TrySpendCoins(dish.unlockPrice))
        {
            AlertClient(sender, "Not enough coins to unlock this dish.");
            return;
        }
        GameData.UnlockDish(foodGuid);
        UnlockDishClientRpc(foodGuid);
    }
    [Rpc(SendTo.NotServer)]
    private void UnlockDishClientRpc(string foodGuid)
    {
        GameData?.UnlockDish(foodGuid);
    }

    [Rpc(SendTo.Server)]
    public void PurchaseUpgradeServerRpc(string upgradeGuid, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (GameData == null || string.IsNullOrEmpty(upgradeGuid)) return;
        UpgradeSO upgrade = ConfigManager.Instance?.ConfigUpgrade?.Upgrades?.Find(x => x.Guid == upgradeGuid);
        if (upgrade == null || GameData.IsUpgradePurchased(upgrade)) return;
        if (GameData.RestaurantData.Level < upgrade.LevelUnlocked)
        {
            AlertClient(sender, $"Reach Level {upgrade.LevelUnlocked} to unlock this upgrade.");
            return;
        }

        string playerId = GetPlayerIdForClient(sender);
        PlayerStats buyerStats = string.IsNullOrEmpty(playerId) ? null : GameData.GetPlayerStatsById(playerId);
        if (upgrade.UpgradeTarget != UpgradeTarget.ExpansionRestaurant && buyerStats == null) return;

        if (!TrySpendCoins(upgrade.UpgradeCosts))
        {
            AlertClient(sender, "Not enough money to buy this item.");
            return;
        }
        GameData.PurchaseUpgrade(upgrade);
        PurchaseUpgradeClientRpc(upgradeGuid);

        if (upgrade.UpgradeTarget == UpgradeTarget.ExpansionRestaurant)
        {
            GridBuildingSystem.Instance.ExpandGrid(upgrade.UpgradeValue);
        }
        else
        {
            // Skill upgrades belong to the player who bought them.
            ApplyUpgradeToStats(buyerStats, upgrade.UpgradeTarget, upgrade.UpgradeValue);
            ApplyUpgradeToStatsClientRpc(playerId, upgrade.UpgradeTarget, upgrade.UpgradeValue);
        }
    }
    [Rpc(SendTo.NotServer)]
    private void PurchaseUpgradeClientRpc(string upgradeGuid)
    {
        UpgradeSO upgrade = ConfigManager.Instance.ConfigUpgrade.Upgrades.Find(x => x.Guid == upgradeGuid);
        if (upgrade != null)
            GameData?.PurchaseUpgrade(upgrade);
    }
    [Rpc(SendTo.NotServer)]
    private void ApplyUpgradeToStatsClientRpc(string playerId, UpgradeTarget upgradeTarget, float amount)
    {
        PlayerStats stats = GameData?.GetPlayerStatsById(playerId);
        if (stats != null)
            ApplyUpgradeToStats(stats, upgradeTarget, amount);
    }
    private static void ApplyUpgradeToStats(PlayerStats stats, UpgradeTarget upgradeTarget, float amount)
    {
        switch (upgradeTarget)
        {
            case UpgradeTarget.MoveSpeed:
                stats.UpdatePlayerMoveSpeed(amount);
                break;
            case UpgradeTarget.CookingSpeed:
                stats.UpdatePlayerCookingSpeed(amount);
                break;
            case UpgradeTarget.CarryingCapacity:
                stats.UpdatePlayerCarryingCapacity(amount);
                break;
            case UpgradeTarget.TipIncrease:
                stats.UpdatePlayerTipIncrease(amount);
                break;
        }
    }

    [Rpc(SendTo.Server)]
    public void UnlockCosmeticServerRpc(string cosmeticType, int index, RpcParams rpcParams = default)
    {
        if (GameData == null || ConfigManager.Instance == null) return;
        CosmeticsData cosmeticsData = ConfigManager.Instance.CustomizationData.CosmeticDatas.Find(x => x.Type == cosmeticType);
        if (cosmeticsData == null || index < 0 || index >= cosmeticsData.Cosmetics.Count) return;
        if (GameData.CosmeticData.IsCosmeticUnlocked(cosmeticType, index)) return;
        if (!TrySpendCoins(cosmeticsData.Cosmetics[index].Price))
        {
            AlertClient(rpcParams.Receive.SenderClientId, "Not enough coins to unlock this cosmetic.");
            return;
        }
        GameData.CosmeticData.UnlockCosmetic(cosmeticType, index);
        UnlockCosmeticClientRpc(cosmeticType, index);
    }
    [Rpc(SendTo.NotServer)]
    private void UnlockCosmeticClientRpc(string cosmeticType, int index)
    {
        GameData?.CosmeticData.UnlockCosmetic(cosmeticType, index);
    }

    #endregion

    #region Rewards

    // The rewarded ad itself is watched on the player's device and cannot be verified here;
    // the server enforces the daily count (by the host's clock) and the reward amount.
    [Rpc(SendTo.Server)]
    public void ClaimDailyFreeServerRpc(ShopItemType rewardType, RpcParams rpcParams = default)
    {
        if (GameData == null || ShopManager.Instance == null) return;
        DailyFreeCurrency offer = ShopManager.Instance.DailyFreeCurrency.Find(x => x.Id == rewardType);
        if (offer == null || !int.TryParse(offer.Reward, out int amount) || amount <= 0) return;

        GameData.ShopData.RefreshDailyShopOffer();
        if (GameData.GetShopDailyFreeWatchCount(rewardType) >= offer.Count)
        {
            AlertClient(rpcParams.Receive.SenderClientId, "No free rewards left today.");
            return;
        }
        GameData.IncreaseShopDailyFreeWatchCount(rewardType);
        IncreaseDailyFreeCountClientRpc(rewardType);

        if (rewardType == ShopItemType.Coin)
            ApplyCoins(amount);
        else if (rewardType == ShopItemType.Gem)
            ApplyGems(amount);
    }
    [Rpc(SendTo.NotServer)]
    private void IncreaseDailyFreeCountClientRpc(ShopItemType rewardType)
    {
        if (GameData == null) return;
        GameData.ShopData.RefreshDailyShopOffer();
        GameData.IncreaseShopDailyFreeWatchCount(rewardType);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Rpc(SendTo.Server)]
    public void CheatAddServerRpc(int coins, int exp)
    {
        ServerAddCoins(coins);
        ServerAddExp(exp);
    }
#endif

    #endregion

    #region Restaurant data

    [Rpc(SendTo.Server)]
    public void UpdateRestaurantNameServerRpc(string name)
    {
        if (GameData == null || string.IsNullOrWhiteSpace(name)) return;
        string clean = name.Trim();
        if (clean.Length > MAX_RESTAURANT_NAME_LENGTH) clean = clean.Substring(0, MAX_RESTAURANT_NAME_LENGTH);
        GameData.RestaurantData.UpdateRestaurantName(clean);
        UpdateRestaurantNameClientRpc(clean);
    }
    [Rpc(SendTo.NotServer)]
    private void UpdateRestaurantNameClientRpc(string name)
    {
        GameData?.RestaurantData.UpdateRestaurantName(name);
    }

    [Rpc(SendTo.Server)]
    public void AddDishToMenuServerRpc(string foodGuid)
    {
        FoodSO dish = FindDish(foodGuid);
        if (GameData == null || dish == null || !GameData.MenuData.unlockedDishes.Contains(dish)) return;
        if (GameData.AddDishToMenu(dish))
            AddDishToMenuClientRpc(foodGuid);
    }
    [Rpc(SendTo.NotServer)]
    private void AddDishToMenuClientRpc(string foodGuid)
    {
        FoodSO dish = FindDish(foodGuid);
        if (dish != null)
            GameData?.AddDishToMenu(dish);
    }

    [Rpc(SendTo.Server)]
    public void RemoveDishFromMenuServerRpc(string foodGuid)
    {
        FoodSO dish = FindDish(foodGuid);
        if (GameData == null || dish == null) return;
        if (GameData.RemoveDishFromMenu(dish))
            RemoveDishFromMenuClientRpc(foodGuid);
    }
    [Rpc(SendTo.NotServer)]
    private void RemoveDishFromMenuClientRpc(string foodGuid)
    {
        FoodSO dish = FindDish(foodGuid);
        if (dish != null)
            GameData?.RemoveDishFromMenu(dish);
    }

    // Server only: inventory changes decided by the server (placing, picking up, buying).
    public void ServerAddInventory(string guid)
    {
        if (!IsServer || GameData == null || string.IsNullOrEmpty(guid)) return;
        GameData.AddInventoryData(guid);
        AddInventoryDataClientRpc(guid);
    }
    public void ServerRemoveInventory(string guid)
    {
        if (!IsServer || GameData?.InventoryData == null || string.IsNullOrEmpty(guid)) return;
        GameData.RemoveInventoryData(guid);
        RemoveInventoryDataClientRpc(guid);
    }
    [Rpc(SendTo.NotServer)]
    private void AddInventoryDataClientRpc(string guid)
    {
        GameData?.AddInventoryData(guid);
    }
    [Rpc(SendTo.NotServer)]
    private void RemoveInventoryDataClientRpc(string guid)
    {
        GameData?.RemoveInventoryData(guid);
    }

    // Server only: a package was taken out of the PostBox.
    public void ServerRemovePostBoxPackage(string kitchenObjectSOGuid)
    {
        if (!IsServer || GameData?.PostBoxData == null || string.IsNullOrEmpty(kitchenObjectSOGuid)) return;
        GameData.PostBoxData.RemovePackage(kitchenObjectSOGuid);
        RemovePostBoxDataClientRpc(kitchenObjectSOGuid);
    }
    [Rpc(SendTo.NotServer)]
    private void RemovePostBoxDataClientRpc(string kitchenObjectSOGuid)
    {
        GameData?.PostBoxData.RemovePackage(kitchenObjectSOGuid);
    }

    #endregion

    #region Player customization

    // Called on the owning client after the player saves their look.
    public void RequestSaveCustomization(Dictionary<string, int> customizations)
    {
        var encoded = new StringBuilder();
        foreach (var entry in customizations)
        {
            if (encoded.Length > 0) encoded.Append(CUSTOMIZATION_ENTRY_SEPARATOR);
            encoded.Append(entry.Key).Append(CUSTOMIZATION_VALUE_SEPARATOR).Append(entry.Value);
        }
        SaveCustomizationServerRpc(encoded.ToString());
    }

    [Rpc(SendTo.Server)]
    private void SaveCustomizationServerRpc(string encodedCustomizations, RpcParams rpcParams = default)
    {
        string playerId = GetPlayerIdForClient(rpcParams.Receive.SenderClientId);
        PlayerStats stats = string.IsNullOrEmpty(playerId) ? null : GameData?.GetPlayerStatsById(playerId);
        Dictionary<string, int> customizations = DecodeCustomizations(encodedCustomizations);
        if (stats == null || customizations == null) return;
        stats.UpdatePlayerCustomization(customizations);
        SaveCustomizationClientRpc(playerId, encodedCustomizations);
    }
    [Rpc(SendTo.NotServer)]
    private void SaveCustomizationClientRpc(string playerId, string encodedCustomizations)
    {
        PlayerStats stats = GameData?.GetPlayerStatsById(playerId);
        Dictionary<string, int> customizations = DecodeCustomizations(encodedCustomizations);
        if (stats != null && customizations != null)
            stats.UpdatePlayerCustomization(customizations);
    }

    // Returns null when any entry names an unknown part or an out-of-range index (-1 = cleared).
    private static Dictionary<string, int> DecodeCustomizations(string encoded)
    {
        var result = new Dictionary<string, int>();
        if (string.IsNullOrEmpty(encoded)) return result;
        List<CosmeticsData> cosmeticDatas = ConfigManager.Instance.CustomizationData.CosmeticDatas;
        foreach (string entry in encoded.Split(CUSTOMIZATION_ENTRY_SEPARATOR))
        {
            string[] parts = entry.Split(CUSTOMIZATION_VALUE_SEPARATOR);
            if (parts.Length != 2 || !int.TryParse(parts[1], out int index)) return null;
            CosmeticsData cosmeticsData = cosmeticDatas.Find(x => x.Type == parts[0]);
            if (cosmeticsData == null || index < -1 || index >= cosmeticsData.Cosmetics.Count) return null;
            result[parts[0]] = index;
        }
        return result;
    }

    #endregion

    private static FoodSO FindDish(string foodGuid)
    {
        if (string.IsNullOrEmpty(foodGuid) || ConfigManager.Instance?.ConfigFood?.FoodItems == null) return null;
        return ConfigManager.Instance.ConfigFood.FoodItems.Find(x => x.Guid == foodGuid);
    }

    private string GetPlayerIdForClient(ulong clientId)
    {
        if (clientId == NetworkManager.LocalClientId && SessionManager.Instance != null)
            return SessionManager.Instance.PlayerId;
        return MultiplayerManager.Instance != null
            ? MultiplayerManager.Instance.GetPlayerDataFromClientId(clientId).playerId.ToString()
            : null;
    }

    private static void AlertClient(ulong clientId, string message)
    {
        UIManager.Instance.ShowAlert(clientId, message);
    }
}
