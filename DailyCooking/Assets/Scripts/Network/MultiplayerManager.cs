using System;
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
        string jsonData = GameManager.Instance.DataHandler.ConvertGameDataToJson(GameManager.Instance.GameData);

        LoadGameDataClientRpc(jsonData, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }
    [Rpc(SendTo.SpecifiedInParams)]
    private void LoadGameDataClientRpc(string jsonData, RpcParams rpcParams = default)
    {
        // The code inside remains the same
        GameManager.Instance.GameData = GameManager.Instance.DataHandler.LoadFromJson(jsonData);
        OnDataSyncToNewClient?.Invoke(this, EventArgs.Empty);
    }
    [Rpc(SendTo.Server)]
    private void SetplayerNameServerRpc(string playerName, RpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerName = playerName;

        playerDataNetworkList[playerDataIndex] = playerData;
    }
    [Rpc(SendTo.Server)]
    private void SetplayerIdServerRpc(string playerId, RpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

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
    public PlayerData GetLatestPlayerData()
    {
        if (playerDataNetworkList.Count > 0)
        {
            return playerDataNetworkList[playerDataNetworkList.Count - 1];
        }
        return default;
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
