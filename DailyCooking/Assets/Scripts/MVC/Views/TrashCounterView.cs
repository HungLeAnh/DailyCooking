using System;
using UnityEngine;
public class TrashCounterView : BaseCounterView
{
    public override object CreateControllerFromView()
    {
        return new TrashCounterController(this,new TrashCounterModel());
    }
}