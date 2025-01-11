public class DeliveryCounter : BaseCounter
{
    public static DeliveryCounter Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
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
