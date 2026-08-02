using System;
using Unity.Netcode;
using UnityEngine;

public class PrefabSpawnService : NetworkPersistentSingleton<PrefabSpawnService>
{
    public event Action<NetworkObject> OnSpawnRequestCompleted;

    protected override void Awake()
    {
        base.Awake();
    }

    public void SpawnPlacedObjectDirect(Vector3 worldPosition, string placeObjectTypeSOGuid,
        Vector2Int origin, Dir dir, ulong targetClientId, bool isPreview)
    {
        if (GridBuildingSystem.Instance == null) return;

        var placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(placeObjectTypeSOGuid);
        if (placedObjectTypeSO == null) return;

        Transform placedObjectTransform = Instantiate(placedObjectTypeSO.prefab, worldPosition,
            Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0),
            GridBuildingSystem.Instance.Container).transform;
        var networkObject = placedObjectTransform.GetComponent<NetworkObject>();
        PlacedObjectView placedObjectView = networkObject.GetComponent<PlacedObjectView>();
        placedObjectView.Intialize(placeObjectTypeSOGuid, origin, dir, isPreview);

        networkObject.Spawn();
        networkObject.ChangeOwnership(targetClientId);

        NotifyClientOfSpawnClientRpc(networkObject, RpcTarget.Single(targetClientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void NotifyClientOfSpawnClientRpc(NetworkObjectReference spawnedObjectRef, RpcParams rpcParams)
    {
        if (spawnedObjectRef.TryGet(out NetworkObject netObj))
        {
            OnSpawnRequestCompleted?.Invoke(netObj);
        }
    }
}