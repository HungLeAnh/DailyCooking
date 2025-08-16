using UnityEngine;

public class DeliveryCounterService
{
    public void Interact(IKitchenObjectParent player)
    {
        if (player.HasKitchenObject())
        {
            if (player.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                DeliveryManager.Instance.DeliverRecipe(tablewareKitchenObject);
                player.GetKitchenObject().DestroySelf();
            }
        }
    }
}
