using System;
using UnityEngine;
public class ContainerCounterView : BaseCounterView, IContainerCounter
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    public KitchenObjectSO KitchenObjectSO  => kitchenObjectSO;

    public override object CreateControllerFromView()
    {
        return new ContainerCounterController(this,new ContainerCounterModel());
    }
    public KitchenObjectSO GetContainerKitchenObjectType()
    {
        return kitchenObjectSO;
    }
}