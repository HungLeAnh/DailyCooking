using UnityEngine;
using Unity.Netcode;

public class RefillerKitchenObject : KitchenObject
{
    private KitchenObjectSO refillKitchenObjectSO;

    public KitchenObjectSO RefillKitchenObjectSO { get => refillKitchenObjectSO; set => refillKitchenObjectSO = value; }

    public void SetRefillKitchenObject(KitchenObjectSO value)
    {
        refillKitchenObjectSO = value;
        Debug.Log("RefillKitchenObjectSO set to: " + refillKitchenObjectSO);
    }
    [Rpc(SendTo.Server)]
    public void RefillContainerServerRpc(NetworkBehaviourReference containerCounter)
    {
        var kitchenObjectSO = GetKitchenObjectSO() as RefillerKitchenObjectSO;
        RefillClientsAndHostRpc(containerCounter, kitchenObjectSO.refillingAmount, refillKitchenObjectSO.Guid);
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void RefillClientsAndHostRpc(NetworkBehaviourReference containerCounter, float fillAmount, string kitchenObjectGuid)
    {
        Debug.Log("RefillContainerClientRpc called with containerCounter: " + containerCounter);
        containerCounter.TryGet(out NetworkBehaviour containerCounterNetworkBehaviour);
        if (containerCounterNetworkBehaviour is IContainerCounter containerCounterInterface)
        {
            containerCounterInterface.Refill(fillAmount, kitchenObjectGuid);
        }
    }
}