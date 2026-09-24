using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

// Networked side of player interactions. A click is sent to the server, which runs the
// counter/bot/plate logic with this player as the actor; UI feedback goes back to the owner only.
public partial class PlayerStateMachine
{
    // Slack on top of the highlight radius for movement lag between owner and server.
    private const float INTERACT_RANGE_TOLERANCE = 1.5f;
    private const char OPTION_GUID_SEPARATOR = ',';

    public static PlayerStateMachine LocalInstance { get; private set; }

    // Server: the avatar of a connected client, e.g. to send feedback for one of its RPCs.
    public static PlayerStateMachine FindForClient(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer ||
            !networkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) || client.PlayerObject == null)
            return null;
        return client.PlayerObject.GetComponent<PlayerStateMachine>();
    }

    private PlayerStats subscribedOwnerStats;
    private GameData subscribedGameData;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsOwner)
            LocalInstance = this;
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += MultiplayerManager_OnPlayerDataNetworkListChanged;
        SubscribeToOwnerStats();
        SetCharacterMesh();
    }

    public override void OnNetworkDespawn()
    {
        if (LocalInstance == this)
            LocalInstance = null;
        if (MultiplayerManager.Instance != null)
            MultiplayerManager.Instance.OnPlayerDataNetworkListChanged -= MultiplayerManager_OnPlayerDataNetworkListChanged;
        UnsubscribeFromOwnerStats();
        base.OnNetworkDespawn();
    }

    // The owner's stats (and its account id) can arrive after the avatar spawns; keep trying
    // until they exist, then follow their changes to refresh the look.
    private void SubscribeToOwnerStats()
    {
        if (subscribedOwnerStats != null)
            return;
        GameData gameData = GameManager.Instance != null ? GameManager.Instance.GameData : null;
        if (gameData == null)
            return;
        PlayerStats stats = GetOwnerStats();
        if (stats == null)
        {
            if (subscribedGameData != gameData)
            {
                UnsubscribeFromOwnerStats();
                subscribedGameData = gameData;
                gameData.OnPlayerStatsAdded += GameData_OnPlayerStatsAdded;
            }
            return;
        }
        UnsubscribeFromOwnerStats();
        subscribedOwnerStats = stats;
        stats.OnResourceChange += OnResourceChanged;
    }

    private void UnsubscribeFromOwnerStats()
    {
        if (subscribedOwnerStats != null)
            subscribedOwnerStats.OnResourceChange -= OnResourceChanged;
        subscribedOwnerStats = null;
        if (subscribedGameData != null)
            subscribedGameData.OnPlayerStatsAdded -= GameData_OnPlayerStatsAdded;
        subscribedGameData = null;
    }

    private void GameData_OnPlayerStatsAdded(PlayerStats playerStats)
    {
        RetryOwnerStats();
    }

    private void MultiplayerManager_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        RetryOwnerStats();
    }

    private void RetryOwnerStats()
    {
        if (subscribedOwnerStats != null)
            return;
        SubscribeToOwnerStats();
        if (subscribedOwnerStats != null)
            SetCharacterMesh();
    }

    // Account id of the player who owns this avatar (not necessarily the local player).
    public string GetOwnerPlayerId()
    {
        if (IsOwner && SessionManager.Instance != null)
            return SessionManager.Instance.PlayerId;
        if (MultiplayerManager.Instance == null)
            return null;
        return MultiplayerManager.Instance.GetPlayerDataFromClientId(OwnerClientId).playerId.ToString();
    }

    public PlayerStats GetOwnerStats()
    {
        GameData gameData = GameManager.Instance != null ? GameManager.Instance.GameData : null;
        string playerId = GetOwnerPlayerId();
        if (gameData == null || string.IsNullOrEmpty(playerId))
            return null;
        return gameData.GetPlayerStatsById(playerId);
    }

    private void RequestInteract(IInteractable interactable, bool alternate)
    {
        if (!(interactable is NetworkBehaviour target) || !target.IsSpawned)
            return;
        InteractServerRpc(target, alternate);
    }

    [Rpc(SendTo.Server)]
    private void InteractServerRpc(NetworkBehaviourReference targetReference, bool alternate, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;
        if (!targetReference.TryGet(out NetworkBehaviour target) || !(target is IInteractable interactable))
            return;
        if (!IsInInteractRange(target))
            return;

        if (alternate)
            interactable.InteractAlternateEvent(this);
        else
            interactable.InteractEvent(this);
    }

    // Called on the owning client when the player picks an entry in the option menu.
    public void RequestOption(IHasOptionalSO sender, int index)
    {
        if (!(sender is NetworkBehaviour target) || !target.IsSpawned)
            return;
        ChooseOptionServerRpc(target, index);
    }

    [Rpc(SendTo.Server)]
    private void ChooseOptionServerRpc(NetworkBehaviourReference targetReference, int index, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
            return;
        if (!targetReference.TryGet(out NetworkBehaviour target) || !(target is IHasOptionalSO optional))
            return;
        if (!IsInInteractRange(target))
            return;
        optional.ApplyOption(this, index);
    }

    private bool IsInInteractRange(NetworkBehaviour target)
    {
        Vector3 position = transform.position;
        Vector3 closest = target.transform.position;
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++)
                bounds.Encapsulate(colliders[i].bounds);
            closest = bounds.ClosestPoint(position);
        }
        Vector2 offset = new Vector2(closest.x - position.x, closest.z - position.z);
        return offset.magnitude <= radius + INTERACT_RANGE_TOLERANCE;
    }

    // Server: shows a message on this player's screen only.
    public void ShowAlert(string message)
    {
        ShowAlertOwnerRpc(message);
    }

    [Rpc(SendTo.Owner)]
    private void ShowAlertOwnerRpc(string message)
    {
        UIManager.Instance.ShowAlertMessage(message);
    }

    // Server: opens the option menu on this player's screen only. The chosen index comes
    // back through RequestOption and is validated against the same list by the sender.
    public void ShowOptionMenu(IHasOptionalSO sender, List<KitchenObjectSO> options, string title)
    {
        if (!(sender is NetworkBehaviour target) || options == null || options.Count == 0 || options.Any(o => o == null))
            return;
        string optionGuids = string.Join(OPTION_GUID_SEPARATOR.ToString(), options.Select(o => o.Guid));
        ShowOptionMenuOwnerRpc(target, optionGuids, title);
    }

    [Rpc(SendTo.Owner)]
    private void ShowOptionMenuOwnerRpc(NetworkBehaviourReference senderReference, string optionGuids, string title)
    {
        if (!senderReference.TryGet(out NetworkBehaviour sender))
            return;
        if (GridBuildingSystem.Instance != null && GridBuildingSystem.Instance.BuildingPlacementManager != null &&
            GridBuildingSystem.Instance.BuildingPlacementManager.IsBuilding)
            return;

        List<KitchenObjectSO> options = optionGuids.Split(OPTION_GUID_SEPARATOR)
            .Select(guid => KitchenGameManager.Instance.GetKitchenObjectSOByGuid(guid))
            .ToList();
        if (options.Any(o => o == null))
            return;

        UIPopupManager.Instance.ShowPopup(UIPopupType.UIOptionMenuPopup, new UIOptionMenuPopup.Param
        {
            sender = sender,
            optionalList = options,
            Title = title
        });
    }
}
