using System;
using System.Collections.Generic;
using UnityEngine;

public class OptionalContainerCounter : BaseCounter,IHasOptionalSO
{
    public event EventHandler OnPlayreGrabbedObject;

    [SerializeField] private List<KitchenObjectSO> kitchenObjectSOList;
    private PlayerStateMachine _playerStateMachine;
    public override void Interact(PlayerStateMachine playerStateMachine)
    {
        _playerStateMachine = playerStateMachine;
        FireOnShowOptionMenu(kitchenObjectSOList);
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        if (!_playerStateMachine.HasKitchenObject())
        {
            //Player is not carrying anything
            KitchenObject.SpawnKitchenObject(kitchenObjectSOList[index], _playerStateMachine);


            OnPlayreGrabbedObject?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            //Player is carrying something
            if (_playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                //Player is holding a plate
                if (tablewareKitchenObject.TryAddIngredient(kitchenObjectSOList[index]))
                {
                }
            }
        }
    }
}