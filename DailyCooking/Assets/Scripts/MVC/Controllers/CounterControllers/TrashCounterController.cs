using System;
public class TrashCounterController : BaseCounterController
{
    private TrashCounterService _trashCounterService;

    private void Awake()
    {
        BaseCounterModel = new BaseCounterModel();
        _trashCounterService = new TrashCounterService();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        _trashCounterService.Interact(playerStateMachine);
    }
}