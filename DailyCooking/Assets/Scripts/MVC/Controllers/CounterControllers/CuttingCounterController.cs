using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CuttingCounterController : BaseCounterController, IHasProgress, IHasOptionalSO
{
    private NetworkVariable<float> cuttingProgress = new NetworkVariable<float>(-1);
    private CuttingRecipeSO cuttingRecipeSO;

    private CuttingRecipeSO[] CuttingRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetCuttingRecipes() ?? System.Array.Empty<CuttingRecipeSO>();
    [SerializeField] private ProgressBarUI progressBarUI;

    public NetworkVariable<float> CuttingProgress { get => cuttingProgress; set => cuttingProgress = value; }
    public CuttingRecipeSO CuttingRecipeSO { get => cuttingRecipeSO; set => cuttingRecipeSO = value; }
    public float ProgressNormalized
    {
        get => cuttingRecipeSO != null ? (float)cuttingProgress.Value / cuttingRecipeSO.cuttingProgressMax : -1;
    }

    private void Awake()
    {
        // No more model and view
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        cuttingProgress.OnValueChanged += (oldvalue,newvalue) =>
        {
            if(CuttingRecipeSO != null)
            {
                UpdateProgressBar((float)CuttingProgress.Value / CuttingRecipeSO.cuttingProgressMax);
            }
            else
            {
                UpdateProgressBar(0f);
            }
        };
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
                    int index = CuttingRecipes.ToList()
                        .IndexOf(GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO()));
                    SetCuttingRecipeSOServerRpc(index);
                    SetCuttingProgressServerRpc(0f);
                }
            }
        }
        else
        {
            if (CuttingProgress.Value == 0)
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
    [Rpc(SendTo.Server)]
    private void SetCuttingProgressServerRpc(float value)
    {
        cuttingProgress.Value = value;
    }
    [Rpc(SendTo.Server)]
    private void SetCuttingRecipeSOServerRpc(int index)
    {
        SetCuttingRecipeSOClientRpc(index);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void SetCuttingRecipeSOClientRpc(int index)
    {
        if(index < 0 || index >= CuttingRecipes.Length)
        {
            CuttingRecipeSO = null;
        }
        else
        {
            CuttingRecipeSO = CuttingRecipes[index];

        }
    }
    public void Cut()
    {
        CutServerRpc(GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId).CookingSpeed);
    }
    [Rpc(SendTo.Server)]
    private void CutServerRpc(float coodingSpeed)
    {
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            CuttingProgress.Value += (int)coodingSpeed;

            if (CuttingRecipeSO == null)
            {
                int index = CuttingRecipes.ToList().IndexOf(GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO()));
                SetCuttingRecipeSOServerRpc(index);
            }


            if (CuttingProgress.Value >= CuttingRecipeSO.cuttingProgressMax)
            {
                GetKitchenObject().DestroySelf();
                KitchenObject.SpawnKitchenObject(CuttingRecipeSO.output, this);
                CuttingProgress.Value = 0;
                SetCuttingRecipeSOServerRpc(-1);
            }
        }
    }
    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return GetCuttingRecipeSOWithInput(inputKitchenObjectSO) != null;
    }

    private CuttingRecipeSO GetCuttingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return KitchenGameManager.Instance?.RecipeDatabase?.GetCuttingRecipe(inputKitchenObjectSO);
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
        return CuttingProgress.Value == 0 &&
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
        SetCuttingProgressServerRpc(-1f);
        SetCuttingRecipeSOServerRpc(-1);
    }
}