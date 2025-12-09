using System;
using System.Collections.Generic;
using UnityEngine;

public class CuttingCounterController : BaseCounterController, IHasProgress, IHasOptionalSO
{
    [SerializeField] private CuttingRecipeSO[] _cuttingRecipeSOArray;

    private CuttingCounterModel _cuttingCounterModel;
    private CuttingCounterView _cuttingCounterView;

    private void Awake()
    {
        _cuttingCounterModel = new CuttingCounterModel();
        BaseCounterModel = _cuttingCounterModel;
        _cuttingCounterView = (CuttingCounterView)BaseCounterView;
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!HasKitchenObject())
        {
            if (playerStateMachine.HasKitchenObject())
            {
                if (HasRecipeWithInput(playerStateMachine.GetKitchenObject().GetKitchenObjectSO()))
                {
                    playerStateMachine.GetKitchenObject().SetKitchenObjectParent(this);
                    _cuttingCounterModel.CuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
                    _cuttingCounterModel.CuttingProgress = 0;
                    _cuttingCounterView.UpdateProgressBar((float)_cuttingCounterModel.CuttingProgress 
                        / _cuttingCounterModel.CuttingRecipeSO.cuttingProgressMax);
                    
                }
            }
        }
        else
        {
            if (_cuttingCounterModel.CuttingProgress == 0)
            {
                if (playerStateMachine.HasKitchenObject())
                {
                    if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
                    {
                        if (tablewareKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                        {
                            GetKitchenObject().DestroySelf();
                            _cuttingCounterView.UpdateProgressBar(0f);
                            _cuttingCounterModel.ResetModel();
                        }
                    }
                }
                else
                {
                    GetKitchenObject().SetKitchenObjectParent(playerStateMachine);
                    _cuttingCounterView.UpdateProgressBar(0f);
                    _cuttingCounterModel.ResetModel();
                }
            }
        }
    }
    public void Cut(CuttingCounterModel model, IKitchenObjectParent counter)
    {
        if (counter.HasKitchenObject() && HasRecipeWithInput(counter.GetKitchenObject().GetKitchenObjectSO()))
        {
            model.CuttingProgress += (int)GameManager.Instance.GameData.PlayerStats.statsData.CookingSpeed;

            if (model.CuttingRecipeSO == null)
            {
                model.CuttingRecipeSO = GetCuttingRecipeSOWithInput(counter.GetKitchenObject().GetKitchenObjectSO());
            }

            _cuttingCounterView.UpdateProgressBar((float)model.CuttingProgress / model.CuttingRecipeSO.cuttingProgressMax);

            if (model.CuttingProgress >= model.CuttingRecipeSO.cuttingProgressMax)
            {
                counter.GetKitchenObject().DestroySelf();
                KitchenObject.SpawnKitchenObject(model.CuttingRecipeSO.output, counter);
                model.CuttingProgress = 0;
                model.CuttingRecipeSO = null;
                _cuttingCounterView.UpdateProgressBar(0f);
            }
        }
    }
    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return GetCuttingRecipeSOWithInput(inputKitchenObjectSO) != null;
    }

    private CuttingRecipeSO GetCuttingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (CuttingRecipeSO cuttingRecipeSO in _cuttingRecipeSOArray)
        {
            if (cuttingRecipeSO.input == inputKitchenObjectSO)
            {
                return cuttingRecipeSO;
            }
        }
        return null;
    }

    public override void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
        Cut(_cuttingCounterModel, this);
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