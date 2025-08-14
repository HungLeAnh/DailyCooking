using System;
using System.Collections.Generic;
using UnityEngine;
public class OptionalContainerCounterView : BaseCounterView
{
    [SerializeField] private List<KitchenObjectSO> kitchenObjectSOList;

    public List<KitchenObjectSO> KitchenObjectSOList  => kitchenObjectSOList; 

}