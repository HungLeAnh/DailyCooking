using System;
using UnityEngine;
public class ContainerCounterView : BaseCounterView
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    public KitchenObjectSO KitchenObjectSO  => kitchenObjectSO;

    public override object CreateControllerFromView()
    {
        return new ContainerCounterController(this,new ContainerCounterModel());
    }
    
}