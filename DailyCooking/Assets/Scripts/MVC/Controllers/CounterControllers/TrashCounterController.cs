using System;
public class TrashCounterController : BaseCounterController
{
    protected override void Awake()
    {
        base.Awake();
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