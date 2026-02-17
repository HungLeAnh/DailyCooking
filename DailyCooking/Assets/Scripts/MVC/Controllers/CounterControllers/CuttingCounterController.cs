using System;
using System.Collections.Generic;
using UnityEngine;

public class CuttingCounterController : BaseCounterController, IHasProgress, IHasOptionalSO
{
    [SerializeField] private CuttingRecipeSO[] _cuttingRecipeSOArray;
    [SerializeField] private ProgressBarUI progressBarUI;

    private float cuttingProgress;
    private CuttingRecipeSO cuttingRecipeSO;

    public float CuttingProgress { get => cuttingProgress; set => cuttingProgress = value; }
    public CuttingRecipeSO CuttingRecipeSO { get => cuttingRecipeSO; set => cuttingRecipeSO = value; }
    public float ProgressNormalized
    {
        get => cuttingRecipeSO != null ? (float)cuttingProgress / cuttingRecipeSO.cuttingProgressMax : -1;
    }

    private void Awake()
    {
        // No more model and view
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
                    CuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
                    CuttingProgress = 0;
                    UpdateProgressBar((float)CuttingProgress / CuttingRecipeSO.cuttingProgressMax);
                }
            }
        }
        else
        {
            if (CuttingProgress == 0)
            {
                if (playerStateMachine.HasKitchenObject())
                {
                    if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
                    {
                        if (tablewareKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                        {
                            GetKitchenObject().DestroySelf();
                            UpdateProgressBar(0f);
                            ResetCuttingState();
                        }
                    }
                }
                else
                {
                    GetKitchenObject().SetKitchenObjectParent(playerStateMachine);
                    UpdateProgressBar(0f);
                    ResetCuttingState();
                }
            }
        }
    }
    public void Cut()
    {
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            CuttingProgress += (int)GameManager.Instance.GameData.PlayerStats.statsData.CookingSpeed;

            if (CuttingRecipeSO == null)
            {
                CuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
            }

            UpdateProgressBar((float)CuttingProgress / CuttingRecipeSO.cuttingProgressMax);

            if (CuttingProgress >= CuttingRecipeSO.cuttingProgressMax)
            {
                GetKitchenObject().DestroySelf();
                KitchenObject.SpawnKitchenObject(CuttingRecipeSO.output, this);
                CuttingProgress = 0;
                CuttingRecipeSO = null;
                UpdateProgressBar(0f);
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
        Cut();
    }

    public void SetOptionKitchenObjectSO(int index)
    {

    }

    public bool IsDone()
    {
        return CuttingProgress == 0 &&
               CuttingRecipeSO == null &&
               KitchenObject != null;
    }

    public float GetProgress()
    {
        return ProgressNormalized;
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        throw new NotImplementedException();
    }

    public void UpdateProgressBar(float progressPrecentage)
    {
        progressBarUI.OnProgressChanged(progressPrecentage);
    }

    public void ResetCuttingState()
    {
        cuttingProgress = -1;
        cuttingRecipeSO = null;
    }
}