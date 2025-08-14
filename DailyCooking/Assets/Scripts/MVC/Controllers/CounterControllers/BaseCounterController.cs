
using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseCounterController : MonoBehaviour, IKitchenObjectParent, IInteractable
{
    [SerializeField] private BaseCounterView _baseCounterView;
    [SerializeField] private BaseCounterModel _baseCounterModel;

    public BaseCounterView BaseCounterView { get => _baseCounterView; set => _baseCounterView = value; }
    public BaseCounterModel BaseCounterModel { get => _baseCounterModel; set => _baseCounterModel = value; }

    protected virtual void Awake()
    {
        ConnectView();
        _baseCounterModel = new BaseCounterModel();
    }

    public virtual void ConnectView()
    {
        _baseCounterView.OnRestartGame += OnRestartGame;

    }



    protected virtual void OnRestartGame(object sender, PlayerStateMachine e)
    {
        if (_baseCounterModel.KitchenObject != null)
            _baseCounterModel.KitchenObject.DestroySelf();

        ClearKitchenObject();

    }

    public KitchenObject GetKitchenObject()
    {
        return BaseCounterModel.KitchenObject;
    }
    public void ClearKitchenObject()
    {
        BaseCounterModel.KitchenObject = null;
    }
    public bool HasKitchenObject()
    {
        return BaseCounterModel.KitchenObject != null;
    }
    public virtual void InteractEvent(PlayerStateMachine playerStateMachine)
    {
    }

    public virtual void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
    }
   
    public Transform GetKitchenObjectFollowTransform()
    {
        return _baseCounterView.CounterTopPoint;
    }
    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        _baseCounterModel.KitchenObject = kitchenObject;

        if (kitchenObject != null || kitchenObject.GetKitchenObjectOptionalProcessSO() != null)
        {
            UIPopupManager.Instance.ShowPopup(
                UIPopupType.UIOptionMenuPopup.ToString(),
                new UIOptionMenuPopup.Param
                {
                    sender = this,
                    objectSO = kitchenObject.GetKitchenObjectSO(),
                }
            );
        }
    }

}
