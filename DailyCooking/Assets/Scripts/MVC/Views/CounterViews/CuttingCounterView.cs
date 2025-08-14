using System;
using UnityEngine;

public class CuttingCounterView : BaseCounterView
{
    [SerializeField] private CuttingRecipeSO[] cuttingRecipeSOArray;
    [SerializeField] private ProgressBarUI progressBarUI;
    public CuttingRecipeSO[] CuttingRecipeSOArray { get => cuttingRecipeSOArray; set => cuttingRecipeSOArray = value; }

    public void UpdateProgressBar(float progressPrecentage)
    {
        progressBarUI.OnProgressChanged(progressPrecentage);

    }
}