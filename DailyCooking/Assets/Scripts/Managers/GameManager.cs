using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public partial class GameManager : NetworkPersistentSingleton<GameManager>, IGameManager
{
    private const string SavedDataFileName = "SavedData";
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

    private const float SAVE_INTERVAL = 1f;
    private float lastSaveTime;
    private bool isSaveDirty;

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
}
