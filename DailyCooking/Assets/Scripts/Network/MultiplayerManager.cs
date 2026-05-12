using System;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.SinglePlayer;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.LowLevelPhysics2D.PhysicsLayers;

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
        if (SceneManager.GetActiveScene().name != Loader.Scene.GameScene.ToString())
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game has already started";
            Debug.Log("Game has already started");
            return;
        }

        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_AMOUNT)
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game is full";
            Debug.Log("Game is full");
            return;
        }

        connectionApprovalResponse.Approved = true;
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

    public void StartHostSession()
    {
        try
        {
            if (!SessionManager.Instance.IsSignedIn())
            {
                UIManager.Instance.ShowAlertMessage("You must be signed in to start a multiplayer session."); return;
            }
            // Use the network transport when starting a multiplayer session.
            networkManager.NetworkConfig.NetworkTransport = unityTransport;
            NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Server_OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
            if (!networkManager.StartHost())
            {
                NetworkLog.LogError("Failed to start hosted session!");
            }
            Debug.Log("NetworkManager started as Host.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create session: {e.Message}");
        }
    }
    public void StartClientSession()
    {
        try
        {
            if (!SessionManager.Instance.IsSignedIn())
            {
                UIManager.Instance.ShowAlertMessage("You must be signed in to start a multiplayer session."); return;
            }
            // Use the network transport when starting a multiplayer session.
            OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);
            networkManager.NetworkConfig.NetworkTransport = unityTransport;
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
            if (!networkManager.StartClient())
            {
                NetworkLog.LogError("Failed to start client session!");
            }
            Debug.Log("NetworkManager started as Client.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create session: {e.Message}");
        }
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
