using System;
using System.Collections.Generic;
using UnityEngine;

public class OptionalContainerCounterController : BaseCounterController, IHasOptionalSO
{
    public event EventHandler OnPlayreGrabbedObject;

    private PlayerStateMachine _playerStateMachine;


    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        var view = (OptionalContainerCounterView)BaseCounterView;
        Debug.Log("Interact optional Counter");
        _playerStateMachine = playerStateMachine;
        OnShowOptionMenu(view.KitchenObjectSOList);
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        var view = (OptionalContainerCounterView)BaseCounterView;
        if (!_playerStateMachine.HasKitchenObject())
        {
            //Player is not carrying anything
            KitchenObject.SpawnKitchenObject(view.KitchenObjectSOList[index], _playerStateMachine);


            OnPlayreGrabbedObject?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            //Player is carrying something
            if (_playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                //Player is holding a plate
                if (tablewareKitchenObject.TryAddIngredient(view.KitchenObjectSOList[index]))
                {
                }
            }
        }
        _playerStateMachine = null;
    }
    public  void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        UIPopupManager.Instance.ShowPopup(
            UIPopupType.UIOptionMenuPopup.ToString(),
            new UIOptionMenuPopup.Param
            {
                sender = this,
                optionalList = kitchenObjectSOList
            }
        );
    }

}