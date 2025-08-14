using System;
using UnityEngine;
public class ContainerCounterView : BaseCounterView, IContainerCounter
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    public KitchenObjectSO KitchenObjectSO  => kitchenObjectSO;

    public KitchenObjectSO GetContainerKitchenObjectType()
    {
        return kitchenObjectSO;
    }
}