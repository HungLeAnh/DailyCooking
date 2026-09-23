// No longer used: kitchen objects are spawned and despawned directly by the server
// (KitchenGameManager.SpawnKitchenObject / KitchenObject.DestroySelf). Pooling NetworkObjects
// needs an INetworkPrefabInstanceHandler, and the old pool never prewarmed anyway.
// Kept only so the scene reference doesn't become a missing script; remove the component
// from the scene, then delete this file.
public class KitchenObjectPool : NetworkPersistentSingleton<KitchenObjectPool>
{
}
