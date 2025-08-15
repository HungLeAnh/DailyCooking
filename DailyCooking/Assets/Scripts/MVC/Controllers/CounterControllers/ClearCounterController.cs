public class ClearCounterController : BaseCounterController
{
    protected override void Awake()
    {
        base.Awake();
        BaseCounterModel = new BaseCounterModel();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        HandleInteraction(playerStateMachine);
    }
}