using UnityEngine;

public class TrashCounterService
{
    public void Interact(IKitchenObjectParent player)
    {
        if (player.HasKitchenObject())
        {
            player.GetKitchenObject().DestroySelf();
        }
    }
}
