using Unity.Netcode;
using UnityEngine;

public static class PlacedObjectFactory
{
    public static void Create(Vector3 worldPosition, Vector2Int origin, Dir dir, 
        PlacedObjectTypeSO placedObjectTypeSO,ulong clientId, bool isPreview)
    {
        KitchenGameManager.Instance.CreatePlacedObjectViewServerRpc(worldPosition,
            placedObjectTypeSO.Guid,origin,dir,clientId, isPreview);
    }
}