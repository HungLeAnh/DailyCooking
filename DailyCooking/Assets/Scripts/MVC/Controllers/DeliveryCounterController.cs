using System;

[Serializable]
public class DeliveryCounterController : BaseCounterController
{
    public static DeliveryCounterController Instance { get; private set; }

    public void Init()
    {
        Instance = this;
    }
    public DeliveryCounterController(DeliveryCounterView view,DeliveryCounterModel model) : base(view,model)
    {
        Init();
    }

    public override void Interact(PlayerStateMachine playerStateMachine)
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