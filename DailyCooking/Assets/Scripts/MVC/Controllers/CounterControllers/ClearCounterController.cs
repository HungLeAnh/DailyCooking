public class ClearCounterController : BaseCounterController
{
    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!HasKitchenObject() && playerStateMachine.HasKitchenObject())
        {
            playerStateMachine.GetKitchenObject().SetKitchenObjectParent(this);
        }
        else if (HasKitchenObject() && !playerStateMachine.HasKitchenObject())
        {
            GetKitchenObject().SetKitchenObjectParent(playerStateMachine);
        }
        else if (HasKitchenObject() && playerStateMachine.HasKitchenObject())
        {
            if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject plateKitchenObject))
            {
                if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                {
                    GetKitchenObject().DestroySelf();
                }
            }
            else
            {
                if (GetKitchenObject().TryGetTableware(out plateKitchenObject))
                {
                    if (plateKitchenObject.TryAddIngredient(playerStateMachine.GetKitchenObject().GetKitchenObjectSO()))
                    {
                        playerStateMachine.GetKitchenObject().DestroySelf();
                    }
                }
            }
        }
    }
}