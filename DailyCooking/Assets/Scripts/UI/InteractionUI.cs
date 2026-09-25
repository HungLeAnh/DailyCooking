using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

// Server-owned channel for the option menu: interaction code runs on the server and opens the
// menu on the acting player's screen only. The option the player picks comes back here and is
// applied on the server with that player as the actor. Alerts go through UIManager.ShowAlert.
// Lives on the KitchenGameManager object (MainMenuScene).
public class InteractionUI : NetworkPersistentSingleton<InteractionUI>
{
    private const char OPTION_GUID_SEPARATOR = ',';

    // Server: opens the option menu on this player's screen only. The chosen index comes
    // back through RequestOption and is validated against the same list by the sender.
    public void ShowOptionMenu(PlayerStateMachine actor, IHasOptionalSO sender, List<KitchenObjectSO> options, string title)
    {
        if (!IsServer || !IsSpawned || actor == null)
            return;
        if (!(sender is NetworkBehaviour target) || options == null || options.Count == 0 || options.Any(o => o == null))
            return;
        string optionGuids = string.Join(OPTION_GUID_SEPARATOR.ToString(), options.Select(o => o.Guid));
        ShowOptionMenuRpc(target, optionGuids, title, RpcTarget.Single(actor.OwnerClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowOptionMenuRpc(NetworkBehaviourReference senderReference, string optionGuids, string title, RpcParams rpcParams)
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

    // Called on a client when the local player picks an entry in the option menu.
    public void RequestOption(IHasOptionalSO sender, int index)
    {
        if (!IsSpawned || !(sender is NetworkBehaviour target) || !target.IsSpawned)
            return;
        ChooseOptionServerRpc(target, index);
    }

    // Any client may call this (the object is server-owned); the actor is the sender's avatar.
    [Rpc(SendTo.Server)]
    private void ChooseOptionServerRpc(NetworkBehaviourReference targetReference, int index, RpcParams rpcParams = default)
    {
        PlayerStateMachine actor = PlayerStateMachine.FindForClient(rpcParams.Receive.SenderClientId);
        if (actor == null)
            return;
        if (!targetReference.TryGet(out NetworkBehaviour target) || !(target is IHasOptionalSO optional))
            return;
        if (!actor.IsInInteractRange(target))
            return;
        optional.ApplyOption(actor, index);
    }
}
