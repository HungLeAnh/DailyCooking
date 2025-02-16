using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CounterModules : MonoBehaviour
{
    [SerializeReference] private List<BaseCounterController> baseCounterControllers = new List<BaseCounterController>();
    public void Start()
    {
        Initialize();
    }
    public void Initialize()
    {
        var counterViews = GetComponentsInChildren<BaseCounterView>();
        foreach (var counterView in counterViews)
        {
            //Debug.Log("Type: " + counterView.GetType());
            var controller = counterView.CreateControllerFromView();
            baseCounterControllers.Add(controller as BaseCounterController);
        }
    }
}
