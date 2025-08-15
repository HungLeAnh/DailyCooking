
using System;
using System.Collections.Generic;
using UnityEngine;

public class CuttingCounterController : BaseCounterController, IHasProgress, IHasOptionalSO
{
    [SerializeField] private CuttingRecipeSO[] cuttingRecipeSOArray;
    public event EventHandler OnCut;

    private CuttingCounterModel _cuttingCounterModel;
    private CuttingCounterView _cuttingCounterView;

    protected override void Awake()
    {
        base.Awake();
        _cuttingCounterModel = new CuttingCounterModel();
        BaseCounterModel = _cuttingCounterModel;
        _cuttingCounterView = (CuttingCounterView)BaseCounterView;
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!HasKitchenObject())
        {
            //There is no kitchen object
            if (playerStateMachine.HasKitchenObject())
            {
                //Player is carrying something
                if (HasRecipeWithInput(playerStateMachine.GetKitchenObject().GetKitchenObjectSO()))
                {
                    //Player is carrying something that can be cut
                    playerStateMachine.GetKitchenObject().SetKitchenObjectParent(this);

                    if (GetKitchenObject().GetKitchenObjectOptionalProcessSO() != null)
                    {
                        //Show optional recipe menu
                        _cuttingCounterModel.CuttingRecipeSO = null;
                    }
                    else
                    {
                        _cuttingCounterModel.CuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
                        UpdateProgressUI();
                    }
                }
            }
        }
        else
        {
            if (_cuttingCounterModel.CuttingProgress == 0)
            {
                //There is kitchen object here not process / done process
                base.HandleInteraction(playerStateMachine);
            }
        }
    }

    private void UpdateProgressUI()
    {
        var progressNormalized = (float)_cuttingCounterModel.CuttingProgress / _cuttingCounterModel.CuttingRecipeSO.cuttingProgressMax;
        _cuttingCounterView.UpdateProgressBar(progressNormalized);
    }

    public override void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            //there is kitchen object here
            _cuttingCounterModel.CuttingProgress++;

            OnCut?.Invoke(this, EventArgs.Empty);

            if (_cuttingCounterModel.CuttingRecipeSO == null)
                _cuttingCounterModel.CuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());

            UpdateProgressUI();

            if (_cuttingCounterModel.CuttingProgress >= _cuttingCounterModel.CuttingRecipeSO.cuttingProgressMax)
            {
                GetKitchenObject().DestroySelf();
                KitchenObject.SpawnKitchenObject(_cuttingCounterModel.CuttingRecipeSO.output, this);
                _cuttingCounterModel.CuttingProgress = 0;
                _cuttingCounterModel.CuttingRecipeSO = null;
            }
        }
    }

    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return GetCuttingRecipeSOWithInput(inputKitchenObjectSO) != null;
    }

    private CuttingRecipeSO GetCuttingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (CuttingRecipeSO cuttingRecipeSO in cuttingRecipeSOArray)
        {
            if (cuttingRecipeSO.input == inputKitchenObjectSO)
            {
                return cuttingRecipeSO;
            }
        }
        return null;
    }

    private CuttingRecipeSO GetCuttingRecipeSOWithOutput(int outputKitchenObjectSOIndex)
    {
        var outputKitchenObjectSO = GetKitchenObject().GetKitchenObjectOptionalProcessSO().processListOutput[outputKitchenObjectSOIndex];
        foreach (CuttingRecipeSO cuttingRecipeSO in cuttingRecipeSOArray)
        {
            if (cuttingRecipeSO.output == outputKitchenObjectSO)
            {
                return cuttingRecipeSO;
            }
        }
        return null;
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        _cuttingCounterModel.CuttingRecipeSO = GetCuttingRecipeSOWithOutput(index);
        UpdateProgressUI();
    }

    public bool IsDone()
    {
        return _cuttingCounterModel.CuttingProgress == 0 &&
               _cuttingCounterModel.CuttingRecipeSO == null &&
               _cuttingCounterModel.KitchenObject != null;
    }

    public float GetProgress()
    {
        return _cuttingCounterModel.CuttingProgress;
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        throw new NotImplementedException();
    }
}
