using System;
using UnityEngine;

public class ContainerCounterService
{
    public event EventHandler<KitchenObjectSO> OnSpawnKitchenObject;

    public void Interact(IKitchenObjectParent player, KitchenObjectSO kitchenObjectSO)
    {
        if (!player.HasKitchenObject())
        {
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, player);
            OnSpawnKitchenObject?.Invoke(this, kitchenObjectSO);
        }
        else
        {
            if (player.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                if (tablewareKitchenObject.TryAddIngredient(kitchenObjectSO))
                {
                    OnSpawnKitchenObject?.Invoke(this, null); // Indicate ingredient added to plate
                }
            }
        }
    }
}