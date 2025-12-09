public class CuttingCounterModel: BaseCounterModel
{
    private float cuttingProgress;
    private CuttingRecipeSO cuttingRecipeSO;

    public float CuttingProgress { get => cuttingProgress; set => cuttingProgress = value; }
    public CuttingRecipeSO CuttingRecipeSO { get => cuttingRecipeSO; set => cuttingRecipeSO = value; }
    public float ProgressNormalized 
    { 
        get => cuttingRecipeSO != null ? (float)cuttingProgress / cuttingRecipeSO.cuttingProgressMax: -1; 
    }
    public void ResetModel()
    {
        cuttingProgress = -1;
        cuttingRecipeSO = null;
    }
}