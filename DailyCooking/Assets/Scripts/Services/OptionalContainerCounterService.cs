using System;
using System.Collections.Generic;
using UnityEngine;

public class OptionalContainerCounterService
{
    public event Action OnPlayerGrabbedObject;

    public void SetOption(int index, IKitchenObjectParent player, List<KitchenObjectSO> kitchenObjectSOList)
    {
        if (!player.HasKitchenObject())
        {
            KitchenObject.SpawnKitchenObject(kitchenObjectSOList[index], player);
            OnPlayerGrabbedObject?.Invoke();
        }
        else
        {
            if (player.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                if (tablewareKitchenObject.TryAddIngredient(kitchenObjectSOList[index]))
                {
                }
            }
        }
    }
}