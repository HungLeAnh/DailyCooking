using System;
using System.Collections.Generic;
using UnityEngine;

public class OptionalContainerCounterController : BaseCounterController, IHasOptionalSO, IContainerCounter
{
    [SerializeField] private List<KitchenObjectSO> kitchenObjectSOList;

    public event EventHandler OnPlayreGrabbedObject;

    private PlayerStateMachine _playerStateMachine;

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        Debug.Log("Interact optional Counter");
        _playerStateMachine = playerStateMachine;
        OnShowOptionMenu(kitchenObjectSOList);
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        if (!_playerStateMachine.HasKitchenObject())
        {
            KitchenObject.SpawnKitchenObject(kitchenObjectSOList[index], _playerStateMachine);
            OnPlayreGrabbedObject?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            if (_playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
            {
                if (tablewareKitchenObject.TryAddIngredient(kitchenObjectSOList[index]))
                {
                }
            }
        }
        _playerStateMachine = null;
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        if(GridBuildingSystem.Instance.BuildingPlacementManager.IsBuilding)
            return;
        if (IsPlaced.Value)
        {
            UIPopupManager.Instance.ShowPopup(
            UIPopupType.UIOptionMenuPopup,
            new UIOptionMenuPopup.Param
            {
                sender = this,
                optionalList = kitchenObjectSOList
            });

        }
    }

    public List<KitchenObjectSO> GetContainerKitchenObjectType()
    {
        return kitchenObjectSOList;
    }

    public void Refill(float fillAmount, string kitchenObjectGuid)
    {
        
    }
}
