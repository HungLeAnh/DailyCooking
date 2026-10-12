using UnityEngine;

public class TablewareCounterController : BaseCounterController
{
    // Fixed-content counter: the SO is baked on the prefab on purpose.
    // Counters whose contents change at runtime (fridge / refillable) must go through
    // PlacedObjectView GUID + NetworkVariable — see BaseContainerCounterController.
    [SerializeField] private KitchenObjectSO _tablewareKitchenObjectSO;

    // Unlimited source: the counter restocks nothing and never runs out.
    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            // Without this, SpawnKitchenObject returns null on a missing SO and the player
            // presses the counter with no feedback as to why no plate comes out.
            if (_tablewareKitchenObjectSO == null)
            {
                Debug.LogError("TablewareCounterController: _tablewareKitchenObjectSO is not assigned!", this);
                return;
            }
            KitchenObject.SpawnKitchenObject(_tablewareKitchenObjectSO, playerStateMachine);
        }
    }
}
