using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.SinglePlayer;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerManager : NetworkPersistentSingleton<MultiplayerManager>
{
    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    public event EventHandler OnPlayerDataNetworkListChanged;
    public event EventHandler OnDataSyncToNewClient;

    public const int MAX_PLAYER_AMOUNT = 4;
    [SerializeField] private UnityTransport unityTransport;
    [SerializeField] private SinglePlayerTransport singlePlayerTransport;
    [SerializeField] private NetworkManager networkManager;

    private string playerName;
    private NetworkList<PlayerData> playerDataNetworkList;
    private bool isSinglePlayerMode = false;
    public bool IsSinglePlayerMode => isSinglePlayerMode;
    public string GetPlayerName()
    {
        return playerName;
    }
    protected override void Awake()
    {
        base.Awake();
        playerName = "PlayerName" + UnityEngine.Random.Range(100, 1000);
        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
    }
    private void Start()
    {

    }
    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }
    private void NetworkManager_Server_OnClientConnectedCallback(ulong clientId)
    {
        playerDataNetworkList.Add(new PlayerData
        {
            clientId = clientId,
        });
        SetplayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }
    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            PlayerData playerData = playerDataNetworkList[i];
            if (playerData.clientId == clientId)
            {
                //Disconnected
                playerDataNetworkList.RemoveAt(i);
            }
        }
    }
    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        // Host-authoritative gate: capacity + duplicate-client guard.
        // Payload validation (PlayerId) happens in SetplayerIdServerRpc via SenderClientId.
        if (playerDataNetworkList.Count >= MAX_PLAYER_AMOUNT)
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Room full";
            connectionApprovalResponse.CreatePlayerObject = false;
            return;
        }
        connectionApprovalResponse.Approved = true;
        connectionApprovalResponse.CreatePlayerObject = false;
    }
    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId)
    {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }
    private void NetworkManager_Client_OnClientConnectCallback(ulong clientId)
    {
        SetplayerNameServerRpc(GetPlayerName());
        SetplayerIdServerRpc(AuthenticationService.Instance.PlayerId);
        SyncDataToNewClientServerRpc(clientId);
    }
    [Rpc(SendTo.Server)]
    private void SyncDataToNewClientServerRpc(ulong clientId)
    {
        if (GameManager.Instance?.GameData == null || GameManager.Instance.DataHandler == null) return;
        string jsonData = GameManager.Instance.DataHandler.ConvertGameDataToJson(GameManager.Instance.GameData);
        if (string.IsNullOrEmpty(jsonData)) return;

        LoadGameDataClientRpc(jsonData, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }
    [Rpc(SendTo.SpecifiedInParams)]
    private void LoadGameDataClientRpc(string jsonData, RpcParams rpcParams = default)
    {
        if (GameManager.Instance == null) return;
        if (string.IsNullOrEmpty(jsonData)) return;
        try
        {
        var snapshotHandler = GameManager.Instance.DataHandler ?? new FileDataHandler(Application.persistentDataPath, "GameData_temp");
        GameManager.Instance.ReplaceGameDataFromHost(snapshotHandler.LoadFromJson(jsonData));
        if (GameManager.Instance.GameData == null) return;
        OnDataSyncToNewClient?.Invoke(this, EventArgs.Empty);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LoadGameDataClientRpc failed: {e.Message}");
        }
    }
    [Rpc(SendTo.Server)]
    private void SetplayerNameServerRpc(string playerName, RpcParams serverRpcParams = default)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;
        string clean = playerName.Trim();
        if (clean.Length > 32) clean = clean.Substring(0, 32);
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
        if (playerDataIndex < 0 || playerDataIndex >= playerDataNetworkList.Count) return;

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerName = clean;

        playerDataNetworkList[playerDataIndex] = playerData;
    }
    [Rpc(SendTo.Server)]
    private void SetplayerIdServerRpc(string playerId, RpcParams serverRpcParams = default)
    {
        if (string.IsNullOrEmpty(playerId) || playerId.Length > 128) return;
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);
        if (playerDataIndex < 0 || playerDataIndex >= playerDataNetworkList.Count) return;
        // Prevent duplicate PlayerIds (one account, one slot).
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if (i != playerDataIndex && playerDataNetworkList[i].playerId.ToString() == playerId) return;
        }

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerId = playerId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }
    public int GetPlayerDataIndexFromClientId(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if (playerDataNetworkList[i].clientId == clientId)
            {
                return i;
            }
        }
        return -1;
    }
    public PlayerData GetPlayerDataFromClientId(ulong clientId)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.clientId == clientId)
            {
                return playerData;
            }
        }
        return default;
    }
    public IEnumerable<PlayerData> GetAllPlayerData()
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            yield return playerData;
        }
    }
    public void StartSinglePlayer()
    {
        // Use the single player transport when starting a single player session.
        networkManager.NetworkConfig.NetworkTransport = singlePlayerTransport;
        isSinglePlayerMode = true;
        if (!networkManager.StartHost())
        {
            NetworkLog.LogError("Failed to start single player session!");
        }
    }

    public async Task<string> StartHostSessionAsync()
    {
        try
        {
            if (!SessionManager.Instance.IsSignedIn())
            {
                await SessionManager.Instance.SignInAnonymouslyAsync();
            }

            NetworkManager.Singleton.ConnectionApprovalCallback = NetworkManager_ConnectionApprovalCallback;
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Server_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;

            string joinCode = await SessionManager.Instance.StartHostWithRelay(MAX_PLAYER_AMOUNT, "dtls");

            if (string.IsNullOrEmpty(joinCode))
            {
                NetworkLog.LogError("Failed to start host with Relay");
            }

            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create session: {e.Message}");
            return null;
        }
    }
    public async Task<bool> StartClientSession(string joinCode)
    {
        try
        {
            if (!SessionManager.Instance.IsSignedIn())
            {
                UIManager.Instance.ShowAlertMessage("You must be signed in to join.");
                return false;
            }

            OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

            // The host's snapshot replaces GameData; never keep a local save attached.
            GameManager.Instance.ClearActiveSave();

            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;

            return await SessionManager.Instance.StartClientWithRelay(joinCode, "dtls");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join session: {e.Message}");
            return false;
        }
    }
    public void ShutdownAndReset()
    {
        // Flush the host's save while it is still the server, then detach the file.
        if (GameManager.Instance != null)
            GameManager.Instance.ClearActiveSave(discardData: false);

        if (NetworkManager.Singleton != null)
        {
            // 1. Unsubscribe from global network events to prevent duplicates
            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_Server_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_Client_OnClientConnectCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Server_OnClientDisconnectCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Client_OnClientDisconnectCallback;

            // 2. Clear out the approval delegate entirely
            NetworkManager.Singleton.ConnectionApprovalCallback = null;

            // 3. Tell the underlying engine to close sockets, ports, and structures
            NetworkManager.Singleton.Shutdown();
        }

        // 4. Clear your runtime lists so they don't hold stale player profiles
        if (playerDataNetworkList != null)
        {
            playerDataNetworkList.Clear();
        }

        isSinglePlayerMode = false;
        Debug.Log("MultiplayerManager fully reset for next run.");
    }
}
public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
    public ulong clientId;
    public FixedString64Bytes playerName;
    public FixedString64Bytes playerId;

    public bool Equals(PlayerData other)
    {
        return clientId == other.clientId &&
            playerName == other.playerName &&
            playerId == other.playerId;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref playerId);
    }
}
