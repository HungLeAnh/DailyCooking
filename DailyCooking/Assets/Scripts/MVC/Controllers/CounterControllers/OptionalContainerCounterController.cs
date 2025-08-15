
using System;
using System.Collections.Generic;
using UnityEngine;

public class OptionalContainerCounterController : BaseCounterController, IHasOptionalSO
{
    [SerializeField] private List<KitchenObjectSO> kitchenObjectSOList;

    public event EventHandler OnPlayreGrabbedObject;

    private PlayerStateMachine _playerStateMachine;

    protected override void Awake()
    {
        base.Awake();
        BaseCounterModel = new BaseCounterModel();
    }

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
        _playerStateMachine = null;
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
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
