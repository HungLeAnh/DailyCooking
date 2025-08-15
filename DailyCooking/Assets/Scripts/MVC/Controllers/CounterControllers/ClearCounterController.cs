public class ClearCounterController : BaseCounterController
{
    protected override void Awake()
    {
        base.Awake();
        BaseCounterModel = new BaseCounterModel();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        //Debug.Log("ClearCounter.Interact();");

        if (!HasKitchenObject() && playerStateMachine.HasKitchenObject())
        {
            //Place object player carrying on the counter
            playerStateMachine.GetKitchenObject().SetKitchenObjectParent(this);
            
        }
        else
        {
            //There is kitchen object here
            if (playerStateMachine.HasKitchenObject())
            {
                //Player is carrying something
                if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject plateKitchenObject))
                {
                    //Player is holding a plate
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        GetKitchenObject().DestroySelf();
                    }
                }
                else
                {
                    //Player is not carrying plate
                    if (GetKitchenObject().TryGetTableware(out plateKitchenObject))
                    {
                        //Counter is holding the plate
                        if (plateKitchenObject.TryAddIngredient(playerStateMachine.GetKitchenObject().GetKitchenObjectSO()))
                        {
                            playerStateMachine.GetKitchenObject().DestroySelf();
                        }
                    }
                }
            }
            else
            {
                //Player is not carrying anything
                GetKitchenObject().SetKitchenObjectParent(playerStateMachine);
            }
        }
    }
}