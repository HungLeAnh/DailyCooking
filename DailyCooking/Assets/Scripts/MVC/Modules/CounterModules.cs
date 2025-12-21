using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CounterModules : PersistentSingleton<CounterModules>, IModules
{
    private List<BaseCounterController> baseCounterControllers = new List<BaseCounterController>();
    public List<BaseCounterController> BaseCounterControllers { get => baseCounterControllers; set => baseCounterControllers = value; }

    protected override void Awake()
    {
        base.Awake();
        baseCounterControllers = new List<BaseCounterController>();
    }
    public void DestroyItem(BaseCounterController baseCounterController)
    {
        if (baseCounterController == null) return;

        var controller = baseCounterControllers.FindLast(x => x == baseCounterController);

        if (controller != null)
        {
            RemoveCounterController(controller);
        }
    }
    public void AddController(BaseCounterController controller)
    {
        baseCounterControllers.Add(controller);
        KitchenGameManager.Instance.AddUnlockIngredient(controller);
        controller.OnDestroySelf += () => DestroyItem(controller);

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
