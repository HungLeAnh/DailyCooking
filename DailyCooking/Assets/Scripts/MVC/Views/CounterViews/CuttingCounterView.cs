using System;
using UnityEngine;

public class CuttingCounterView : BaseCounterView
{
    [SerializeField] private CuttingRecipeSO[] cuttingRecipeSOArray;
    [SerializeField] private ProgressBarUI progressBarUI;
    public CuttingRecipeSO[] CuttingRecipeSOArray { get => cuttingRecipeSOArray; set => cuttingRecipeSOArray = value; }
    public override object CreateControllerFromView()
    {
        return new CuttingCounterController(this,new CuttingCounterModel());
    }
    public override void UpdateView(object cuttingCounterModel)
    {
        base.UpdateView(cuttingCounterModel);
        CuttingCounterModel model = (CuttingCounterModel)cuttingCounterModel;
        progressBarUI.OnProgressChanged(model.ProgressNormalized);
    }
}