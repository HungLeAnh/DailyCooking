using System;

public class DeliveryCounterController : BaseCounterController
{
    public static DeliveryCounterController Instance { get; private set; }

    private DeliveryCounterService _deliveryCounterService;

    public void Init()
    {
        Instance = this;
    }
    private void Awake()
    {
        BaseCounterModel = new BaseCounterModel();
        _deliveryCounterService = new DeliveryCounterService();
        Init();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        _deliveryCounterService.Interact(playerStateMachine);
    }
}