using System;
using UnityEngine;

public class CuttingCounterView : BaseCounterView
{
    [SerializeField] private ProgressBarUI progressBarUI;

    public void UpdateProgressBar(float progressPrecentage)
    {
        progressBarUI.OnProgressChanged(progressPrecentage);
    }
}