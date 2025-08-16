public class ClearCounterController : BaseCounterController
{
    private ClearCounterService _clearCounterService;

    private void Awake()
    {
        BaseCounterModel = new BaseCounterModel();
        _clearCounterService = new ClearCounterService();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        _clearCounterService.Interact(this, playerStateMachine);
    }
}