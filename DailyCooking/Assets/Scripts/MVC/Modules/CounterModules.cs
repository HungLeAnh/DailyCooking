using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CounterModules : PersistentSingleton<CounterModules>
{
    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public class OnSelectedCounterChangedEventArgs : EventArgs
    {
        public BaseCounterView selectedCounterView;
    }

    private List<BaseCounterController> baseCounterControllers = new List<BaseCounterController>();
    private bool isInited = false;
    public bool IsInited => isInited;
    public List<BaseCounterController> BaseCounterControllers { get => baseCounterControllers; set => baseCounterControllers = value; }

    public void Initialize()
    {
        isInited = true;
        var counterViews = GridBuildingSystem.Instance.Container.GetComponentsInChildren<BaseCounterView>();
        foreach (var counterView in counterViews)
        {
            //Debug.Log("Type: " + counterView.GetType());
            var controller = counterView.CreateControllerFromView();
            baseCounterControllers.Add(controller as BaseCounterController);
        }
    }
    public void AddNewCounterController(BaseCounterView baseCounterView)
    {
        var controller = baseCounterView.CreateControllerFromView()as BaseCounterController;
        baseCounterControllers.Add(controller) ;
        DeliveryManager.Instance.AddUnlockIngredient(controller);
    }
    public void DestroyCounter(BaseCounterView baseCounterView)
    {
       var controller =  baseCounterControllers.FindLast(x => x.BaseCounterView == baseCounterView);


        if(controller != null)
        {
            baseCounterControllers.Remove(controller);
            controller.BaseCounterView.UnsubEvent();
            controller.BaseCounterModel.Unsubscribe(Observer.EObserverEvent.ModelChange, controller);
            GridBuildingSystem.Instance.DestroyPlaceObject(baseCounterView.GetComponent<PlacedObjectView>());
        }
    }
    public void FireOnSelectedCounterChanged(OnSelectedCounterChangedEventArgs args)
    {
        OnSelectedCounterChanged?.Invoke(this, args);
    }
    public BaseCounterController GetCounterController(BaseCounterView baseCounterView)
    {
        return baseCounterControllers.Find(x => x.BaseCounterView == baseCounterView);
    }
    public bool TryGetCounterController(BaseCounterView baseCounterView, out BaseCounterController baseCounterController)
    {
        baseCounterController = baseCounterControllers.Find(x => x.BaseCounterView == baseCounterView);
        if(baseCounterController != null)
        {
            return true;
        }
        else
        {
            baseCounterController = null;
            return false;
        }
    }
}
