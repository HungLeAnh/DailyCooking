using System.Collections.Generic;

public class ContainerCounterController : BaseCounterController, IContainerCounter
{
    private ContainerData containerData;

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if(containerData.FillAmount <= 0f)
        {
            return;
        }

        if (!playerStateMachine.HasKitchenObject())
        {
            KitchenObject.SpawnKitchenObject(containerData.KitchenObjectSO, playerStateMachine);
        }
        else
        {
            if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                if (tablewareKitchenObject.TryAddIngredient(containerData.KitchenObjectSO))
                {
                    containerData.Empty(1f);

                }
            }
        }
    }

    public List<KitchenObjectSO> GetContainerKitchenObjectType()
    {
        return new List<KitchenObjectSO> { containerData.KitchenObjectSO };
    }
}
