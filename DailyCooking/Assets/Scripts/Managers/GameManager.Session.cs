using Unity.Netcode;
using UnityEngine;

// Server: what happens to a player's avatar while the player is away (dropped or quiet), and when
// the 60 s seat hold runs out.
public partial class GameManager
{
    private void SubscribeSessionEvents()
    {
        MultiplayerManager mm = MultiplayerManager.Instance;
        if (mm == null) return;
        UnsubscribeSessionEvents();
        mm.OnPlayerAway += Session_OnPlayerAway;
        mm.OnPlayerBack += Session_OnPlayerBack;
        mm.OnSeatExpired += Session_OnSeatExpired;
    }

    private void UnsubscribeSessionEvents()
    {
        MultiplayerManager mm = MultiplayerManager.Instance;
        if (mm == null) return;
        mm.OnPlayerAway -= Session_OnPlayerAway;
        mm.OnPlayerBack -= Session_OnPlayerBack;
        mm.OnSeatExpired -= Session_OnSeatExpired;
    }

    private void Session_OnPlayerAway(string playerId)
    {
        if (!IsServer) return;
        PlayerStateMachine avatar = PlayerStateMachine.FindByPlayerId(playerId);
        if (avatar != null) avatar.ServerSetAway(true);
    }

    // Heard again while still connected: the avatar is theirs again.
    private void Session_OnPlayerBack(string playerId)
    {
        if (!IsServer) return;
        PlayerStateMachine avatar = PlayerStateMachine.FindByPlayerId(playerId);
        if (avatar != null && avatar.OwnerClientId != NetworkManager.ServerClientId)
            avatar.ServerSetAway(false);
    }

    // Nobody came back: the held food goes to the nearest free counter (else it is destroyed) and
    // the avatar leaves.
    private void Session_OnSeatExpired(string playerId)
    {
        if (!IsServer) return;
        PlayerStateMachine avatar = PlayerStateMachine.FindByPlayerId(playerId);
        if (avatar == null || !avatar.IsAway) return;
        KitchenObject held = avatar.GetKitchenObject();
        if (held != null)
        {
            ClearCounterController counter = NearestFreeCounter(avatar.transform.position);
            if (counter == null || !held.SetKitchenObjectParent(counter))
            {
                held.DestroySelf();
                Debug.Log($"[Net] {playerId}'s food destroyed (no free counter)");
            }
            else
            {
                Debug.Log($"[Net] {playerId}'s food placed on {counter.name}");
            }
        }
        avatar.NetworkObject.Despawn(true);
        Debug.Log($"[Net] avatar of {playerId} removed after the seat hold");
    }

    private static ClearCounterController NearestFreeCounter(Vector3 from)
    {
        ClearCounterController best = null;
        float bestDistance = float.MaxValue;
        foreach (ClearCounterController counter in Object.FindObjectsByType<ClearCounterController>(FindObjectsSortMode.None))
        {
            if (!AvatarRules.AcceptsExpiredFood(counter) || !counter.IsSpawned || counter.HasKitchenObject()) continue;
            float distance = (counter.transform.position - from).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = counter;
            }
        }
        return best;
    }
}
