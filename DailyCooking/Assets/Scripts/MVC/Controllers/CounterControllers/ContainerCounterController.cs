using System;
using System.Drawing.Text;
using UnityEngine;

public class ContainerCounterController : BaseCounterController
{
    public event EventHandler OnPlayreGrabbedObject;

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        var view = (ContainerCounterView)BaseCounterView;
        if (!playerStateMachine.HasKitchenObject())
        {
            //Player is not carrying anything
            KitchenObject.SpawnKitchenObject(view.KitchenObjectSO, playerStateMachine);


            OnPlayreGrabbedObject?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            //Player is carrying something
            if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                //Player is holding a plate
                if (tablewareKitchenObject.TryAddIngredient(view.KitchenObjectSO))
                {
                }
            }
        }
    }
}