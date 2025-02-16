public class CuttingCounterModel: BaseCounterModel
{
    private int cuttingProgress;
    private CuttingRecipeSO _cuttingRecipeSO;

    public int CuttingProgress { get => cuttingProgress; set => cuttingProgress = value; }
    public CuttingRecipeSO CuttingRecipeSO { get => _cuttingRecipeSO; set => _cuttingRecipeSO = value; }
    public float ProgressNormalized 
    { 
        get => _cuttingRecipeSO != null ? (float)cuttingProgress / _cuttingRecipeSO.cuttingProgressMax: 0; 
    }
}
