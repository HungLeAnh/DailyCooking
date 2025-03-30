using System;
using System.Collections.Generic;
using UnityEngine;
public class OptionalContainerCounterView : BaseCounterView
{
    [SerializeField] private List<KitchenObjectSO> kitchenObjectSOList;

    public List<KitchenObjectSO> KitchenObjectSOList  => kitchenObjectSOList; 

    public override object CreateControllerFromView()
    {
        return new OptionalContainerCounterController(this,new OptionalContainerCounterModel());
    }
}