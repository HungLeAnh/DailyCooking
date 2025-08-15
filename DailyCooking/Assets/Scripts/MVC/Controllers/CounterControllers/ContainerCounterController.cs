
using System;
using UnityEngine;

public class ContainerCounterController : BaseCounterController, IContainerCounter
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;


    protected override void Awake()
    {
        base.Awake();
        BaseCounterModel = new BaseCounterModel();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            //Player is not carrying anything
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, playerStateMachine);
        }
        else
        {
            //Player is carrying something
            if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                //Player is holding a plate
                if (tablewareKitchenObject.TryAddIngredient(kitchenObjectSO))
                {
                }
            }
        }
    }

    public KitchenObjectSO GetContainerKitchenObjectType()
    {
        return kitchenObjectSO; 
    }
}
