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
        var counterViews = GridBuildingSystem.Instance.Container.GetComponentsInChildren<BaseCounterController>();
        foreach (var counter in counterViews)
        {
            if (counter != null)
            {
                counter.OnDestroySelf += ()=> DestroyCounter(counter);
                baseCounterControllers.Add(counter);
            }
        }
    }
    public void DestroyCounter(BaseCounterController baseCounterController)
    {
        if (baseCounterController == null) return;

        var controller = baseCounterControllers.FindLast(x => x == baseCounterController);

        if (controller != null)
        {
            RemoveCounterController(controller);
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
