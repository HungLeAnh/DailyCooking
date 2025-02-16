using System;

[Serializable]
public class DeliveryCounterController : BaseCounterController
{
    public static DeliveryCounterController Instance { get; private set; }
    private DeliveryCounterModel _model;
    private DeliveryCounterView _view;
    public void Init()
    {
        Instance = this;
    }
    public DeliveryCounterController(DeliveryCounterView view,DeliveryCounterModel model) : base(view,model)
    {
        _model = model;
        _view = view;
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