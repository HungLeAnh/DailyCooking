using System;
public class TrashCounterController : BaseCounterController
{
    private void Awake()
    {
        BaseCounterModel = new BaseCounterModel();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (playerStateMachine.HasKitchenObject())
        {
            playerStateMachine.GetKitchenObject().DestroySelf();
        }
    }
}