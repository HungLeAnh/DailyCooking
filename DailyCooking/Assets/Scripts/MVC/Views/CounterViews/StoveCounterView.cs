using System;
using UnityEngine;
public class StoveCounterView : BaseCounterView
{
    [SerializeField] private CookingTool _cookingTool;

    public CookingTool CookingTool { get => _cookingTool; set => _cookingTool = value; }

    public override object CreateControllerFromView()
    {
        return new StoveCounterController(this,new StoveCounterModel());
    }
    public override void UpdateView(object baseCounterModel)
    {
        base.UpdateView(baseCounterModel);

    }
}