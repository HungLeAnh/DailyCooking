using System;
using UnityEngine;

public class CuttingCounterService
{
    public event EventHandler<float> OnProgressChanged;
    public event EventHandler OnCut;
    public event EventHandler<KitchenObjectSO> OnSpawnKitchenObject;

    private readonly CuttingRecipeSO[] _cuttingRecipeSOArray;

    public CuttingCounterService(CuttingRecipeSO[] cuttingRecipeSOArray)
    {
        _cuttingRecipeSOArray = cuttingRecipeSOArray;
    }

    public void Interact(CuttingCounterModel model, IKitchenObjectParent counter, IKitchenObjectParent player)
    {
        if (!counter.HasKitchenObject())
        {
            if (player.HasKitchenObject())
            {
                if (HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO()))
                {
                    player.GetKitchenObject().SetKitchenObjectParent(counter);
                    model.CuttingRecipeSO = GetCuttingRecipeSOWithInput(counter.GetKitchenObject().GetKitchenObjectSO());
                    model.CuttingProgress = 0;
                    OnProgressChanged?.Invoke(this, (float)model.CuttingProgress / model.CuttingRecipeSO.cuttingProgressMax);
                }
            }
        }
        else
        {
            if (model.CuttingProgress == 0)
            {
                if (player.HasKitchenObject())
                {
                    if (player.GetKitchenObject().TryGetTableware(out TablewareKitchenObject tablewareKitchenObject))
                    {
                        if (tablewareKitchenObject.TryAddIngredient(counter.GetKitchenObject().GetKitchenObjectSO()))
                        {
                            counter.GetKitchenObject().DestroySelf();
                            OnSpawnKitchenObject?.Invoke(this, null); // Indicate object destroyed
                        }
                    }
                }
                else
                {
                    counter.GetKitchenObject().SetKitchenObjectParent(player);
                    OnSpawnKitchenObject?.Invoke(this, null); // Indicate object taken
                }
            }
        }
    }

    public void Cut(CuttingCounterModel model, IKitchenObjectParent counter)
    {
        if (counter.HasKitchenObject() && HasRecipeWithInput(counter.GetKitchenObject().GetKitchenObjectSO()))
        {
            model.CuttingProgress++;
            OnCut?.Invoke(this, EventArgs.Empty);

            if (model.CuttingRecipeSO == null)
            {
                model.CuttingRecipeSO = GetCuttingRecipeSOWithInput(counter.GetKitchenObject().GetKitchenObjectSO());
            }

            OnProgressChanged?.Invoke(this, (float)model.CuttingProgress / model.CuttingRecipeSO.cuttingProgressMax);

            if (model.CuttingProgress >= model.CuttingRecipeSO.cuttingProgressMax)
            {
                counter.GetKitchenObject().DestroySelf();
                KitchenObject.SpawnKitchenObject(model.CuttingRecipeSO.output, counter);
                OnSpawnKitchenObject?.Invoke(this, model.CuttingRecipeSO.output);
                model.CuttingProgress = 0;
                model.CuttingRecipeSO = null;
                OnProgressChanged?.Invoke(this, 0f);
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
}