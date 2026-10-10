using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public partial class GameManager : NetworkPersistentSingleton<GameManager>, IGameManager
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
    }

    public void ShowPlayer()
    {
        //playerGameObject.SetActive(true);
    }

    public void DestroyPlayer()
    {
        Destroy(playerGameObject);
    }

    public void SwitchState(GameManagerBaseState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
        OnStateChanged?.Invoke(this, EventArgs.Empty);
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
