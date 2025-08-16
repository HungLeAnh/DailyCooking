using UnityEngine;

public class ClearCounterService
{
    public void Interact(IKitchenObjectParent counter, IKitchenObjectParent player)
    {
        if (!counter.HasKitchenObject() && player.HasKitchenObject())
        {
            player.GetKitchenObject().SetKitchenObjectParent(counter);
        }
        else if (counter.HasKitchenObject() && !player.HasKitchenObject())
        {
            counter.GetKitchenObject().SetKitchenObjectParent(player);
        }
        else if (counter.HasKitchenObject() && player.HasKitchenObject())
        {
            if (player.GetKitchenObject().TryGetTableware(out TablewareKitchenObject plateKitchenObject))
            {
                if (plateKitchenObject.TryAddIngredient(counter.GetKitchenObject().GetKitchenObjectSO()))
                {
                    counter.GetKitchenObject().DestroySelf();
                }
            }
            else
            {
                if (counter.GetKitchenObject().TryGetTableware(out plateKitchenObject))
                {
                    if (plateKitchenObject.TryAddIngredient(player.GetKitchenObject().GetKitchenObjectSO()))
                    {
                        player.GetKitchenObject().DestroySelf();
                    }
                }
            }
        }
    }
}
