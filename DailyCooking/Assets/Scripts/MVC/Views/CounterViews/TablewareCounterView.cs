using System;
using System.Collections.Generic;
using UnityEngine;
public class TablewareCounterView : BaseCounterView
{

    [SerializeField] private KitchenObjectSO _tablewareKitchenObjectSO;

    public KitchenObjectSO TablewareKitchenObjectSO { get => _tablewareKitchenObjectSO; set => _tablewareKitchenObjectSO = value; }
    public override object CreateControllerFromView()
    {
        return new TablewareCounterController(this,new TablewareCounterModel());
    }
}