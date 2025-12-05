using System;
using System.Collections.Generic;
using UnityEngine;

public class ContainerCounterController : BaseCounterController, IContainerCounter
{
    [SerializeField] private KitchenObjectSO _kitchenObjectSO;

    private void Awake()
    {
        BaseCounterModel = new BaseCounterModel();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            KitchenObject.SpawnKitchenObject(_kitchenObjectSO, playerStateMachine);
        }
        else
        {
            if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                if (tablewareKitchenObject.TryAddIngredient(_kitchenObjectSO))
                {
                    // Indicate ingredient added to plate
                    //OnSpawnKitchenObject?.Invoke(this, null); 
                }
            }
        }
    }

    public List<KitchenObjectSO> GetContainerKitchenObjectType()
    {
        return new List<KitchenObjectSO> { _kitchenObjectSO };
    }
}
