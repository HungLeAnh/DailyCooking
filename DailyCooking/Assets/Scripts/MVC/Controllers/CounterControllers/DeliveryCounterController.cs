using System;

public class DeliveryCounterController : BaseCounterController
{
    public static DeliveryCounterController Instance { get; private set; }

    public void Init()
    {
        Instance = this;
    }
    protected override void Awake()
    {
        base.Awake();
        Init();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (playerStateMachine.HasKitchenObject())
        {
            if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                DeliveryManager.Instance.DeliverRecipe(tablewareKitchenObject);
                playerStateMachine.GetKitchenObject().DestroySelf();

            }
        }
    }
}