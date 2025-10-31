
using System;
using System.Collections.Generic;
using UnityEngine;

public class BaseCounterController : MonoBehaviour, IKitchenObjectParent, IInteractable
{
    [SerializeField] private BaseCounterView _baseCounterView;
    private BaseCounterModel _baseCounterModel;

    public BaseCounterView BaseCounterView { get => _baseCounterView; set => _baseCounterView = value; }
    public BaseCounterModel BaseCounterModel { get => _baseCounterModel; set => _baseCounterModel = value; }

    

    protected virtual void Start()
    {
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
    }
    protected virtual void OnDestroy()
    {
        KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
    }
    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (KitchenGameManager.Instance.IsGameOver() ||
            KitchenGameManager.Instance.IsEditing())
        {
            BaseCounterView.Hide();
            OnRestartGame(this, PlayerStateMachine.Instance);
        }
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
        BaseCounterModel.ClearKitchenObject();
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
        BaseCounterModel.SetKitchenObject(kitchenObject);

        if (kitchenObject != null && kitchenObject.GetKitchenObjectOptionalProcessSO() != null)
        {
            UIPopupManager.Instance.ShowPopup(
                UIPopupType.UIOptionMenuPopup,
                new UIOptionMenuPopup.Param
                {
                    sender = this,
                    objectSO = kitchenObject.GetKitchenObjectSO(),
                }
            );
        }
    }

    public void OnSelected()
    {
        BaseCounterView.Show();
    }

    public void OnDeselected()
    {
        BaseCounterView.Hide();
    }
}
