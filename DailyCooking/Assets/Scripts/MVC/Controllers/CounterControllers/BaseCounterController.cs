using MVC;
using Observer;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BaseCounterController : IKitchenObjectParent, IObserver
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
    public virtual void ConnectModel()
    {
        BaseCounterModel.Subscribe(EObserverEvent.ModelChange, this);
    }

    public virtual void ConnectView()
    {
        _baseCounterView.OnInteract += BaseCounterView_OnInteract;
        _baseCounterView.OnInteractAlternate += BaseCounterView_OnProcessKitchenObject;
        _baseCounterView.OnRestartGame += BaseCounterView_OnRestartGame;
        _baseCounterView.OnUpdate += BaseCounterView_OnUpdate;
    }
    
    protected virtual void BaseCounterView_OnUpdate()
    {

    }

    private void BaseCounterView_OnRestartGame(object sender, PlayerStateMachine e)
    {
        if(BaseCounterModel.KitchenObject != null)
            BaseCounterModel.KitchenObject.DestroySelf();

        ClearKitchenObject();
        BaseCounterModel.NotifySubscribers(EObserverEvent.ModelChange);
    }

    private void BaseCounterView_OnProcessKitchenObject(object sender, PlayerStateMachine e)
    {
        ProcessKitchenObject(e);
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
    public virtual void ProcessKitchenObject(PlayerStateMachine playerStateMachine)
    {
        Debug.Log("BaseCounter.ProcessKitchenObject();");
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