using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CounterModules : PersistentSingleton<CounterModules>, ICounterModules
{
    private List<BaseCounterController> baseCounterControllers = new List<BaseCounterController>();
    private bool isInited = false;
    public bool IsInited => isInited;
    public List<BaseCounterController> BaseCounterControllers { get => baseCounterControllers; set => baseCounterControllers = value; }

    protected override void Awake()
    {
        base.Awake();
        baseCounterControllers = new List<BaseCounterController>();
    }
    public void Initialize()
    {
        isInited = true;
        var counterViews = GridBuildingSystem.Instance.Container.GetComponentsInChildren<BaseCounterView>();
        foreach (var counterView in counterViews)
        {
            var controller = counterView.GetController<BaseCounterController>();
            if (controller != null)
            {
                baseCounterControllers.Add(controller);
            }
        }
    }
    public void DestroyCounter(BaseCounterView baseCounterView)
    {
        if (baseCounterView == null) return;

        var controller = baseCounterControllers.FindLast(x => x.BaseCounterView == baseCounterView);

        if (controller != null)
        {
            RemoveCounterController(controller);
            GridBuildingSystem.Instance.DestroyPlaceObject(baseCounterView.GetComponent<PlacedObjectView>());
        }
    }
    public void AddCounterController(BaseCounterController controller)
    {
        baseCounterControllers.Add(controller);
        KitchenGameManager.Instance.AddUnlockIngredient(controller);

    }
    private void RemoveCounterController(BaseCounterController controller)
    {
        baseCounterControllers.Remove(controller);    
    }
    public bool IsContainerCounter(BaseCounterController controller)
    {
        return baseCounterControllers.Contains(controller);
    }
}
