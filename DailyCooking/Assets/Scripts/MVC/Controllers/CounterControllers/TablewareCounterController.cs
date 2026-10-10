using UnityEngine;

public class TablewareCounterController : BaseCounterController
{
    [SerializeField] private KitchenObjectSO _tablewareKitchenObjectSO;

    // Unlimited source: the counter restocks nothing and never runs out.
    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            KitchenObject.SpawnKitchenObject(_tablewareKitchenObjectSO, playerStateMachine);
        }
    }
}
