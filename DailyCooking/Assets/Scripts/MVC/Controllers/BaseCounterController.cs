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
    public BaseCounterController()
    {

    }
    public BaseCounterController(BaseCounterView view,BaseCounterModel model)
    {
        _baseCounterView = view;
        _baseCounterModel = model;

        ConnectModel();
        ConnectView();
    }
    internal virtual void ConnectModel()
    {
        _baseCounterModel.Subscribe(EObserverEvent.ModelChange, this);
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
        _baseCounterView.UpdateView(_baseCounterModel);
    }
    public KitchenObject GetKitchenObject()
    {
        return _baseCounterModel.KitchenObject;
    }
    public void ClearKitchenObject()
    {
        _baseCounterModel.KitchenObject = null;
    }
    public bool HasKitchenObject()
    {
        return _baseCounterModel.KitchenObject != null;
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
        _baseCounterModel.KitchenObject = kitchenObject;
        _baseCounterModel.NotifySubscribers(EObserverEvent.ModelChange);
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