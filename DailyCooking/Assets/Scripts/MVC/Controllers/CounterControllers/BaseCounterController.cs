using MVC;
using Observer;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BaseCounterController : IKitchenObjectParent,IObserver
{
    public static event EventHandler<KitchenObjectSO> OnAnyObjectPlacedHere;
    public static event EventHandler<OnShowOptionalMenuArgs> OnShowOptionalMenu;
    public class OnShowCombineRecipeArgs : EventArgs
    {
        public List<KitchenObjectSO> combineRecipeOutputList;
    }
    public class OnShowOptionalMenuArgs : EventArgs
    {
        public List<KitchenObjectSO> optionalList;
    }
    public static void ResetStaticData()
    {
        OnAnyObjectPlacedHere = null;
    }

    private BaseCounterView _baseCounterView;
    private BaseCounterModel _baseCounterModel;

    public BaseCounterView BaseCounterView { get => _baseCounterView; set => _baseCounterView = value; }
    public BaseCounterModel BaseCounterModel { get => _baseCounterModel; set => _baseCounterModel = value; }

    public BaseCounterController()
    {

    }
    public BaseCounterController(BaseCounterView view,BaseCounterModel model)
    {
        _baseCounterView = view;
        BaseCounterModel = model;

        ConnectModel();
        ConnectView();
    }
    internal virtual void ConnectModel()
    {
        BaseCounterModel.Subscribe(EObserverEvent.ModelChange, this);
    }

    internal virtual void ConnectView()
    {
        _baseCounterView.OnInteract += BaseCounterView_OnInteract;
        _baseCounterView.OnInteractAlternate += BaseCounterView_OnInteractAlternate;
    }

    private void BaseCounterView_OnInteractAlternate(object sender, PlayerStateMachine e)
    {
        InteractAlternate(e);
}

    private void BaseCounterView_OnInteract(object sender, PlayerStateMachine e)
    {
        Interact(e);
    }

    public void OnNotify()
    {
        _baseCounterView.UpdateView(BaseCounterModel);
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
    public virtual void Interact(PlayerStateMachine playerStateMachine)
    {
        Debug.Log("BaseCounter.Interact();");
    }
    public virtual void InteractAlternate(PlayerStateMachine playerStateMachine)
    {
        Debug.Log("BaseCounter.InteractAlternate();");
    }

    public void FireOnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        OnShowOptionalMenu?.Invoke(this, new OnShowOptionalMenuArgs
        {
            optionalList = kitchenObjectSOList
        });
    }

    public Transform GetKitchenObjectFollowTransform()
    {
        return _baseCounterView.CounterTopPoint;
    }
    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        BaseCounterModel.KitchenObject = kitchenObject;
        BaseCounterModel.NotifySubscribers(EObserverEvent.ModelChange);
        if (kitchenObject != null)
        {
            if (kitchenObject.GetKitchenObjectOptionalProcessSO() != null)
            {
                OnAnyObjectPlacedHere?.Invoke(this, kitchenObject.GetKitchenObjectSO());
            }
            else
            {

            }
        }
    }
}