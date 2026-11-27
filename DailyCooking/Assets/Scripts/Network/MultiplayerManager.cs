using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.SinglePlayer;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class MultiplayerManager : NetworkPersistentSingleton<MultiplayerManager>
{
    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;
    public event EventHandler OnPlayerDataNetworkListChanged;
    public event EventHandler OnDataSyncToNewClient;

    public const int MAX_PLAYER_AMOUNT = 4;
    // Kept well under UnityTransport's Max Payload Size (6144 in MainMenuScene).
    private const int SNAPSHOT_CHUNK_SIZE = 4000;
    private const int MAX_PLAYER_NAME_LENGTH = 32;
    [SerializeField] private UnityTransport unityTransport;
    [SerializeField] private SinglePlayerTransport singlePlayerTransport;
    [SerializeField] private NetworkManager networkManager;

    private string playerName;
    private const float SNAPSHOT_TIMEOUT_SECONDS = 15f;
    private const float HOST_CLOSING_DELAY_SECONDS = 0.2f;
    private const string ROOM_FULL_REASON = "Room full";
    private const string LOG = "[Net] ";

    private NetworkList<PlayerData> playerDataNetworkList;
    private bool isSinglePlayerMode = false;
    private int nextSnapshotId;
    private readonly SnapshotAssembler snapshotAssembler = new SnapshotAssembler();
    private Coroutine snapshotTimeout;
    private string pendingEndMessage;
    public bool IsSinglePlayerMode => isSinglePlayerMode;
    // This machine's session; reaching Ended runs the single exit (RunExit).
    public SessionStateMachine Session { get; } = new SessionStateMachine();
    // The Relay code of the session this machine is in (host or client); null outside a session.
    public string CurrentJoinCode { get; private set; }
    // Both ends tolerate a silent phone (an ad) for this long before calling the connection lost.
    private const int DISCONNECT_TIMEOUT_MS = 60000;
    // Server: seats by player id (live + held for 60 s after a drop).
    public SeatRegistry Seats { get; } = new SeatRegistry();
    private readonly Dictionary<ulong, string> playerIdByClient = new Dictionary<ulong, string>();
    private readonly Dictionary<ulong, string> approvedPlayerIds = new Dictionary<ulong, string>();
    private readonly HashSet<string> leavingPlayers = new HashSet<string>();
    // Server: the player id a connection was approved with (null if unknown).
    public string PlayerIdFor(ulong clientId) =>
        playerIdByClient.TryGetValue(clientId, out string id) ? id : null;
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
        Session.OnChanged += Session_OnChanged;
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    #region Session lifecycle
    // Player-initiated leave. The host first tells clients it is closing so they show
    // "The restaurant has closed" instead of "Connection lost".
    public void LeaveSession()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (Session.State == SessionState.Idle || Session.State == SessionState.Ended)
        {
            RunExit(SessionEndReason.Quit);
            return;
        }
        if (nm != null && nm.IsServer && nm.IsListening && !isSinglePlayerMode && nm.ConnectedClientsIds.Count > 1)
        {
            SessionEndingRpc();
            StartCoroutine(QuitAfterDelay());
            return;
        }
        if (nm != null && nm.IsConnectedClient && !nm.IsServer)
        {
            // Leaving on purpose: the host frees the seat at once instead of holding it 60 s.
            LeavingServerRpc();
            StartCoroutine(QuitAfterDelay());
            return;
        }
        Session.UserQuit();
    }

    [Rpc(SendTo.Server)]
    private void LeavingServerRpc(RpcParams rpcParams = default)
    {
        string playerId = PlayerIdFor(rpcParams.Receive.SenderClientId);
        if (playerId == null) return;
        Debug.Log(LOG + $"{playerId} is leaving on purpose");
        leavingPlayers.Add(playerId);
    }

    private System.Collections.IEnumerator QuitAfterDelay()
    {
        yield return new WaitForSecondsRealtime(HOST_CLOSING_DELAY_SECONDS);
        Session.UserQuit();
    }

    [Rpc(SendTo.NotServer)]
    private void SessionEndingRpc()
    {
        Debug.Log(LOG + "host is closing the session");
        Session.HostClosing();
    }

    private void Session_OnChanged(SessionState state)
    {
        Debug.Log(LOG + $"session -> {state}" + (state == SessionState.Ended ? $" ({Session.EndReason})" : ""));
        switch (state)
        {
            case SessionState.Syncing:
                if (snapshotTimeout != null) StopCoroutine(snapshotTimeout);
                snapshotTimeout = StartCoroutine(SnapshotTimeout());
                break;
            case SessionState.Reconnecting:
                BeginReconnect();
                break;
            case SessionState.Ended:
                RunExit(Session.EndReason);
                break;
        }
    }

    private System.Collections.IEnumerator SnapshotTimeout()
    {
        yield return new WaitForSecondsRealtime(SNAPSHOT_TIMEOUT_SECONDS);
        snapshotTimeout = null;
        if (Session.State == SessionState.Syncing)
        {
            Debug.Log(LOG + "snapshot did not arrive in time");
            Session.SnapshotTimedOut();
        }
    }

    // Until the reconnect loop exists, a lost client ends the session at once.
    private void BeginReconnect()
    {
        Session.RetryWindowOver();
    }

    // The one way out of a session, whatever ended it.
    private void RunExit(SessionEndReason reason)
    {
        if (snapshotTimeout != null)
        {
            StopCoroutine(snapshotTimeout);
            snapshotTimeout = null;
        }
        pendingEndMessage = SessionEndText.Message(reason);
        if (reason == SessionEndReason.HostConnectionLost && GameManager.Instance != null)
            GameManager.Instance.SaveGameImmediate();
        ShutdownAndReset();
        CurrentJoinCode = null;
        snapshotAssembler.Reset();
        Session.Reset();
        if (UIPopupManager.Instance != null)
            UIPopupManager.Instance.HidePopup(UIPopupType.UISettingPopup);
        Loader.Load(Loader.Scene.MainMenuScene);
        if (GameManager.Instance != null)
            GameManager.Instance.SwitchState(new MainMenuState(GameManager.Instance));
    }

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingEndMessage == null || scene.name != Loader.Scene.MainMenuScene.ToString())
            return;
        string message = pendingEndMessage;
        pendingEndMessage = null;
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameNotiPopup,
            new UIGameNotiPopup.Param { Title = "Session ended", Message = message });
    }

    private void SubscribeTransportEvents()
    {
        NetworkManager.Singleton.OnClientStopped -= NetworkManager_OnClientStopped;
        NetworkManager.Singleton.OnTransportFailure -= NetworkManager_OnTransportFailure;
        NetworkManager.Singleton.OnClientStopped += NetworkManager_OnClientStopped;
        NetworkManager.Singleton.OnTransportFailure += NetworkManager_OnTransportFailure;
    }

    private void UnsubscribeTransportEvents()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientStopped -= NetworkManager_OnClientStopped;
        NetworkManager.Singleton.OnTransportFailure -= NetworkManager_OnTransportFailure;
    }

    private void NetworkManager_OnClientStopped(bool wasHost)
    {
        Debug.Log(LOG + $"network stopped (host {wasHost})");
        Session.TransportLost();
    }

    private void NetworkManager_OnTransportFailure()
    {
        Debug.Log(LOG + "transport failure");
        Session.TransportLost();
    }
    #endregion
    private void Start()
    {

    }
    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }
    private void NetworkManager_Server_OnClientConnectedCallback(ulong clientId)
    {
        var playerData = new PlayerData { clientId = clientId };
        // The player id comes from the approval payload; the host's own name is filled in directly.
        approvedPlayerIds.TryGetValue(clientId, out string playerId);
        approvedPlayerIds.Remove(clientId);
        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            playerId = AuthenticationService.Instance.PlayerId;
            playerData.playerName = GetPlayerName();
        }
        if (!string.IsNullOrEmpty(playerId))
        {
            playerData.playerId = playerId;
            playerIdByClient[clientId] = playerId;
            Seats.Connected(playerId, clientId);
        }
        playerDataNetworkList.Add(playerData);
        StartHearing(clientId);
        Debug.Log(LOG + $"client {clientId} connected ({playerId}), seats taken {Seats.TakenSeats}");
    }
    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId)
    {
        RemovePlayerData(clientId);
        StopHearing(clientId);
        approvedPlayerIds.Remove(clientId);
        if (!playerIdByClient.TryGetValue(clientId, out string playerId)) return;
        playerIdByClient.Remove(clientId);
        Seats.Disconnected(playerId, clientId, Time.unscaledTime);
        if (Seats.IsHeld(playerId))
        {
            Debug.Log(LOG + $"client {clientId} dropped ({playerId}), seat held {SeatRegistry.HoldSeconds:0} s");
            OnPlayerAway?.Invoke(playerId);
            if (leavingPlayers.Remove(playerId) && Seats.Release(playerId))
            {
                Debug.Log(LOG + $"{playerId} left on purpose: seat freed now");
                OnSeatExpired?.Invoke(playerId);
            }
        }
    }
    private void RemovePlayerData(ulong clientId)
    {
        for (int i = playerDataNetworkList.Count - 1; i >= 0; i--)
        {
            if (playerDataNetworkList[i].clientId == clientId)
                playerDataNetworkList.RemoveAt(i);
        }
    }
    // Host-authoritative gate by player id: held seats count as taken, a known player always gets
    // back in, and a player who reconnects while its old connection lingers replaces it.
    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        ulong clientId = connectionApprovalRequest.ClientNetworkId;
        string playerId = ConnectionPayload.Decode(connectionApprovalRequest.Payload);
        connectionApprovalResponse.CreatePlayerObject = false;
        if (clientId == NetworkManager.ServerClientId)
        {
            connectionApprovalResponse.Approved = true;
            return;
        }
        SeatRegistry.ApprovalResult result = Seats.Approve(playerId, MAX_PLAYER_AMOUNT);
        if (result == SeatRegistry.ApprovalResult.RoomFull)
        {
            Debug.Log(LOG + $"refused {playerId}: room full");
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = ROOM_FULL_REASON;
            return;
        }
        if (result == SeatRegistry.ApprovalResult.ApprovedReplaceLive)
        {
            ulong? oldClient = Seats.LiveClient(playerId);
            if (oldClient.HasValue && oldClient.Value != clientId)
                StartCoroutine(ReplaceLiveConnection(oldClient.Value, playerId));
        }
        if (playerId != null)
            approvedPlayerIds[clientId] = playerId;
        Debug.Log(LOG + $"approved {playerId} as client {clientId} ({result})");
        connectionApprovalResponse.Approved = true;
    }
    // The same player is back on a new connection: drop the old one (next frame, outside approval).
    private System.Collections.IEnumerator ReplaceLiveConnection(ulong oldClientId, string playerId)
    {
        yield return null;
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) yield break;
        Debug.Log(LOG + $"replaced live connection {oldClientId} of {playerId}");
        if (playerIdByClient.TryGetValue(oldClientId, out string oldId) && oldId == playerId)
            playerIdByClient.Remove(oldClientId);
        RemovePlayerData(oldClientId);
        if (nm.ConnectedClients.ContainsKey(oldClientId))
            nm.DisconnectClient(oldClientId);
    }
    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId)
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && clientId != nm.LocalClientId && nm.IsConnectedClient) return;
        string reason = nm != null ? nm.DisconnectReason : null;
        Debug.Log(LOG + $"disconnected from host" + (string.IsNullOrEmpty(reason) ? "" : $": {reason}"));
        if (reason == ROOM_FULL_REASON)
            Session.Refused(SessionEndReason.RoomFull);
        else
            Session.TransportLost();
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }
    private void NetworkManager_Client_OnClientConnectCallback(ulong clientId)
    {
        // Only this local client's own connection starts the handshake.
        if (clientId != NetworkManager.Singleton.LocalClientId) return;
        Session.Connected();
        SetplayerNameServerRpc(GetPlayerName());
        SetplayerIdServerRpc(AuthenticationService.Instance.PlayerId);
        SyncDataToNewClientServerRpc();
    }

    // The host's GameData is sent to the joining client as compressed chunks: a whole restaurant
    // does not fit in one message (UnityTransport Max Payload Size).
    [Rpc(SendTo.Server)]
    private void SyncDataToNewClientServerRpc(RpcParams rpcParams = default)
    {
        if (GameManager.Instance?.GameData == null || GameManager.Instance.DataHandler == null) return;
        string jsonData = GameManager.Instance.DataHandler.ConvertGameDataToJson(GameManager.Instance.GameData);
        if (string.IsNullOrEmpty(jsonData)) return;

        byte[] compressed = Compress(Encoding.UTF8.GetBytes(jsonData));
        int chunkCount = (compressed.Length + SNAPSHOT_CHUNK_SIZE - 1) / SNAPSHOT_CHUNK_SIZE;
        int snapshotId = nextSnapshotId++;
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"Sending game data to client {clientId}: {jsonData.Length} chars, {compressed.Length} bytes compressed, {chunkCount} chunk(s).");

        for (int i = 0; i < chunkCount; i++)
        {
            int length = Math.Min(SNAPSHOT_CHUNK_SIZE, compressed.Length - i * SNAPSHOT_CHUNK_SIZE);
            byte[] chunk = new byte[length];
            Buffer.BlockCopy(compressed, i * SNAPSHOT_CHUNK_SIZE, chunk, 0, length);
            ReceiveGameDataChunkClientRpc(snapshotId, i, chunkCount, chunk, RpcTarget.Single(clientId, RpcTargetUse.Temp));
        }
    }
    [Rpc(SendTo.SpecifiedInParams)]
    private void ReceiveGameDataChunkClientRpc(int snapshotId, int chunkIndex, int chunkCount, byte[] chunk, RpcParams rpcParams = default)
    {
        if (snapshotAssembler.Add(snapshotId, chunkIndex, chunkCount, chunk, out byte[][] chunks))
            LoadGameDataFromHost(chunks);
    }
    private void LoadGameDataFromHost(byte[][] chunks)
    {
        if (GameManager.Instance == null) return;
        try
        {
            using var joined = new MemoryStream();
            foreach (byte[] chunk in chunks)
                joined.Write(chunk, 0, chunk.Length);
            string jsonData = Encoding.UTF8.GetString(Decompress(joined.ToArray()));

            var snapshotHandler = GameManager.Instance.DataHandler ?? new FileDataHandler(Application.persistentDataPath, "GameData_temp");
            GameManager.Instance.ReplaceGameDataFromHost(snapshotHandler.LoadFromJson(jsonData));
            if (GameManager.Instance.GameData == null) return;
            OnDataSyncToNewClient?.Invoke(this, EventArgs.Empty);
            hostSilence.Reset(Time.unscaledTime);
            Session.SnapshotApplied();
        }
        catch (Exception e)
        {
            Debug.LogError($"Loading game data from host failed: {e.Message}");
        }
    }
    private static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal))
            gzip.Write(data, 0, data.Length);
        return output.ToArray();
    }
    private static byte[] Decompress(byte[] data)
    {
        using var input = new GZipStream(new MemoryStream(data), CompressionMode.Decompress);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }
    [Rpc(SendTo.Server)]
    private void SetplayerNameServerRpc(string playerName, RpcParams serverRpcParams = default)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;
        string clean = playerName.Trim();
        if (clean.Length > MAX_PLAYER_NAME_LENGTH) clean = clean.Substring(0, MAX_PLAYER_NAME_LENGTH);
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
        // Set from the approval payload already; a different id later is ignored.
        if (!playerDataNetworkList[playerDataIndex].playerId.IsEmpty) return;
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
        if (Session.State != SessionState.Idle) Session.Reset();
        if (!networkManager.StartHost())
        {
            NetworkLog.LogError("Failed to start single player session!");
            return;
        }
        Session.StartHosting();
    }

    public async Task<string> StartHostSessionAsync()
    {
        try
        {
            if (!SessionManager.Instance.IsSignedIn())
            {
                await SessionManager.Instance.SignInAnonymouslyAsync();
            }

            PrepareHostCallbacks();
            if (Session.State != SessionState.Idle) Session.Reset();
            string joinCode = await SessionManager.Instance.StartHostWithRelay(MAX_PLAYER_AMOUNT, "dtls");

            if (string.IsNullOrEmpty(joinCode))
            {
                NetworkLog.LogError("Failed to start host with Relay");
                return joinCode;
            }

            CurrentJoinCode = joinCode;
            SubscribeTransportEvents();
            Session.StartHosting();
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create session: {e.Message}");
            return null;
        }
    }
    // Both ends set the same connection settings before starting (approval is part of NGO's config check).
    private void PrepareConnection(string playerId)
    {
        NetworkConfig config = NetworkManager.Singleton.NetworkConfig;
        config.ConnectionApproval = true;
        config.ConnectionData = ConnectionPayload.Encode(playerId);
        unityTransport.DisconnectTimeoutMS = DISCONNECT_TIMEOUT_MS;
        ResetHeartbeat();
    }

    private void PrepareHostCallbacks()
    {
        Seats.Clear();
        playerIdByClient.Clear();
        approvedPlayerIds.Clear();
        leavingPlayers.Clear();
        PrepareConnection(SessionManager.Instance != null && SessionManager.Instance.IsSignedIn()
            ? AuthenticationService.Instance.PlayerId : null);
        NetworkManager.Singleton.ConnectionApprovalCallback = NetworkManager_ConnectionApprovalCallback;
        // Remove first: a failed or repeated attempt must not leave the handlers attached twice.
        NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_Server_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Server_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
    }

#if UNITY_EDITOR
    // Editor / Multiplayer Play Mode testing without Relay: the join code "LOCAL" means 127.0.0.1.
    public const string LOCAL_JOIN_CODE = "LOCAL";
    private const ushort LOCAL_PORT = 7777;

    public bool StartLocalHostSession()
    {
        PrepareHostCallbacks();
        if (Session.State != SessionState.Idle) Session.Reset();
        networkManager.NetworkConfig.NetworkTransport = unityTransport;
        unityTransport.SetConnectionData("127.0.0.1", LOCAL_PORT, "0.0.0.0");
        if (!networkManager.StartHost())
            return false;
        CurrentJoinCode = LOCAL_JOIN_CODE;
        SubscribeTransportEvents();
        Session.StartHosting();
        return true;
    }

    private bool StartLocalClient()
    {
        networkManager.NetworkConfig.NetworkTransport = unityTransport;
        unityTransport.SetConnectionData("127.0.0.1", LOCAL_PORT);
        return networkManager.StartClient();
    }
#endif

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

            NetworkManager.Singleton.OnClientConnectedCallback -= NetworkManager_Client_OnClientConnectCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= NetworkManager_Client_OnClientDisconnectCallback;
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;

            if (Session.State == SessionState.Ended) Session.Reset();
            CurrentJoinCode = joinCode;
            PrepareConnection(AuthenticationService.Instance.PlayerId);
            Session.StartJoining();
            SubscribeTransportEvents();
            bool started;
#if UNITY_EDITOR
            if (joinCode == LOCAL_JOIN_CODE)
                started = StartLocalClient();
            else
#endif
            started = await SessionManager.Instance.StartClientWithRelay(joinCode, "dtls");
            if (!started)
                Session.Refused(SessionEndReason.JoinFailed);
            return started;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join session: {e.Message}");
            Session.Refused(SessionEndReason.JoinFailed);
            return false;
        }
    }
    public void ShutdownAndReset()
    {
        bool wasServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        // Flush the host's save while it is still the server, then detach the file.
        if (GameManager.Instance != null)
            GameManager.Instance.ClearActiveSave(discardData: false);

        // A deliberate shutdown is not a lost connection.
        UnsubscribeTransportEvents();
        Seats.Clear();
        playerIdByClient.Clear();
        approvedPlayerIds.Clear();
        leavingPlayers.Clear();
        ResetHeartbeat();

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

        // 4. NetworkList is server-authoritative: only the host may clear it.
        // A client calling Clear() logs "Write permissions (Server) ... not allowed".
        if (playerDataNetworkList != null && wasServer)
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
