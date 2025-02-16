using UnityEngine;
public class ClearCounterView : BaseCounterView
{
    public override object CreateControllerFromView()
    {
        return new ClearCounterController(this,new ClearCounterModel());
    }

}
