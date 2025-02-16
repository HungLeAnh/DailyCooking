using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OptionalContainerCounterController : BaseCounterController, IHasOptionalSO
{
    public event EventHandler OnPlayreGrabbedObject;

    private PlayerStateMachine _playerStateMachine;
    private OptionalContainerCounterModel _model;
    private OptionalContainerCounterView _view;
    public OptionalContainerCounterController(OptionalContainerCounterView view,OptionalContainerCounterModel model) 
        : base(view,model)
    {
        _model = model;
        _view = view;
    }

    public override void Interact(PlayerStateMachine playerStateMachine)
    {
        if(_playerStateMachine != null)     
            return;
        Debug.Log("Interact optional Counter");
        _playerStateMachine = playerStateMachine;
        FireOnShowOptionMenu(_view.KitchenObjectSOList);
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        if (!_playerStateMachine.HasKitchenObject())
        {
            //Player is not carrying anything
            KitchenObject.SpawnKitchenObject(_view.KitchenObjectSOList[index], _playerStateMachine);


            OnPlayreGrabbedObject?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            //Player is carrying something
            if (_playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                //Player is holding a plate
                if (tablewareKitchenObject.TryAddIngredient(_view.KitchenObjectSOList[index]))
                {
                }
            }
        }
        _playerStateMachine = null;
    }
}