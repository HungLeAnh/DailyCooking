using System;
using Unity.Netcode;
using UnityEngine;

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
        //Debug.Log("kitchenObjectSO refillingAmount: " + kitchenObjectSO.refillingAmount);
        containerCounter.TryGet(out NetworkBehaviour containerCounterNetworkBehaviour);
        if (containerCounterNetworkBehaviour is IContainerCounter containerCounterInterface)
        {
            //Debug.Log("RefillContainerClientRpc called with containerCounter: " + containerCounterInterface);
            //Debug.Log("RefillContainerClientRpc called with kitchenObjectSO.refillingAmount: " + kitchenObjectSO.refillingAmount);
            //Debug.Log("RefillContainerClientRpc called with refillKitchenObjectSO.Guid: " + refillKitchenObjectSO.Guid);
            containerCounterInterface.Refill(kitchenObjectSO.refillingAmount, refillKitchenObjectSO.Guid);
        }
    }
}