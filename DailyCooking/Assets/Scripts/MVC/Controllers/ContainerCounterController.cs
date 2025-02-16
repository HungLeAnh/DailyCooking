using System;
using System.Drawing.Text;
using UnityEngine;

[Serializable]
public class ContainerCounterController : BaseCounterController
{
    public event EventHandler OnPlayreGrabbedObject;

    private ContainerCounterModel _model;
    private ContainerCounterView _view;

    public ContainerCounterController(ContainerCounterView view, ContainerCounterModel model) : base(view,model)
    {
        _model = model;
        _view = view;
    }

    public override void Interact(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            //Player is not carrying anything
            KitchenObject.SpawnKitchenObject(_view.KitchenObjectSO, playerStateMachine);


            OnPlayreGrabbedObject?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            //Player is carrying something
            if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                //Player is holding a plate
                if (tablewareKitchenObject.TryAddIngredient(_view.KitchenObjectSO))
                {
                }
            }
        }
    }
}