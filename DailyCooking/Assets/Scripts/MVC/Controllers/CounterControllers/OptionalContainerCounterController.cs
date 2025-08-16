using System;
using System.Collections.Generic;
using UnityEngine;

public class OptionalContainerCounterController : BaseCounterController, IHasOptionalSO
{
    [SerializeField] private List<KitchenObjectSO> kitchenObjectSOList;

    public event EventHandler OnPlayreGrabbedObject;

    private OptionalContainerCounterService _optionalContainerCounterService;
    private PlayerStateMachine _playerStateMachine;

    private void Awake()
    {
        BaseCounterModel = new BaseCounterModel();
        _optionalContainerCounterService = new OptionalContainerCounterService();

        _optionalContainerCounterService.OnPlayerGrabbedObject += () => OnPlayreGrabbedObject?.Invoke(this, EventArgs.Empty);
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        Debug.Log("Interact optional Counter");
        _playerStateMachine = playerStateMachine;
        OnShowOptionMenu(kitchenObjectSOList);
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        _optionalContainerCounterService.SetOption(index, _playerStateMachine, kitchenObjectSOList);
        _playerStateMachine = null;
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        UIPopupManager.Instance.ShowPopup(
            UIPopupType.UIOptionMenuPopup,
            new UIOptionMenuPopup.Param
            {
                sender = this,
                optionalList = kitchenObjectSOList
            }
        );
    }
}
