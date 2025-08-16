using System;
using System.Collections.Generic;
using UnityEngine;

public class CuttingCounterController : BaseCounterController, IHasProgress, IHasOptionalSO
{
    [SerializeField] private CuttingRecipeSO[] _cuttingRecipeSOArray;
    public event EventHandler OnCut;

    private CuttingCounterModel _cuttingCounterModel;
    private CuttingCounterService _cuttingCounterService;
    private CuttingCounterView _cuttingCounterView;

    private void Awake()
    {
        _cuttingCounterModel = new CuttingCounterModel();
        BaseCounterModel = _cuttingCounterModel;
        _cuttingCounterService = new CuttingCounterService(_cuttingRecipeSOArray);
        _cuttingCounterView = (CuttingCounterView)BaseCounterView;

        _cuttingCounterService.OnProgressChanged += (sender, progress) => _cuttingCounterView.UpdateProgressBar(progress);
        _cuttingCounterService.OnCut += (sender, e) => OnCut?.Invoke(this, EventArgs.Empty);
        _cuttingCounterService.OnSpawnKitchenObject += (sender, kitchenObjectSO) =>
        {
            if (kitchenObjectSO == null)
            {
                _cuttingCounterView.UpdateProgressBar(0f);
            }
        };
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        _cuttingCounterService.Interact(_cuttingCounterModel, this, playerStateMachine);
    }

    public override void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
        _cuttingCounterService.Cut(_cuttingCounterModel, this);
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        // This logic needs to be moved to the service as well.
        // For now, leaving it as is.
    }

    public bool IsDone()
    {
        return _cuttingCounterModel.CuttingProgress == 0 &&
               _cuttingCounterModel.CuttingRecipeSO == null &&
               _cuttingCounterModel.KitchenObject != null;
    }

    public float GetProgress()
    {
        return _cuttingCounterModel.ProgressNormalized;
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        throw new NotImplementedException();
    }
}