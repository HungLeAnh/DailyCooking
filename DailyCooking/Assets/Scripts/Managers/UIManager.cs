
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;

// A NetworkBehaviour (with a NetworkObject, MainMenuScene) so the server can show an alert on
// one client's screen. ShowAlertMessage stays local and works without a session.
public class UIManager : NetworkPersistentSingleton<UIManager>
{
    [SerializeField] private UIAlert uiAlert;

    public void ShowAlertMessage(string message)
    {
        uiAlert.StartAlert(message);
    }

    // Server: shows a message on this player's screen only.
    public void ShowAlert(PlayerStateMachine actor, string message)
    {
        if (actor != null)
            ShowAlert(actor.OwnerClientId, message);
    }

    // Server: shows a message on this client's screen only.
    public void ShowAlert(ulong clientId, string message)
    {
        if (!IsServer || !IsSpawned)
            return;
        ShowAlertRpc(message, RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void ShowAlertRpc(string message, RpcParams rpcParams)
    {
        ShowAlertMessage(message);
    }
}