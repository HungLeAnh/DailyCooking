using System;

public class ContainerCounterController : BaseCounterController
{
    protected override void Awake()
    {
        base.Awake();
        BaseCounterModel = new BaseCounterModel();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        var view = (ContainerCounterView)BaseCounterView;
        if (!playerStateMachine.HasKitchenObject())
        {
            //Player is not carrying anything
            KitchenObject.SpawnKitchenObject(view.KitchenObjectSO, playerStateMachine);
        }
        else
        {
            //Player is carrying something
            if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                //Player is holding a plate
                if (tablewareKitchenObject.TryAddIngredient(view.KitchenObjectSO))
                {
                }
            }
        }
    }
}