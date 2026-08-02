using System;
using System.Collections.Generic;
using UnityEngine;

public class PanCookingTool : CookingTool
{
    private FryingRecipeSO[] FryingRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetFryingRecipes() ?? System.Array.Empty<FryingRecipeSO>();
    private BurningRecipeSO[] BurningRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetBurningRecipes() ?? System.Array.Empty<BurningRecipeSO>();

    private FryingRecipeSO _fryingRecipeSO;
    private BurningRecipeSO _burningRecipeSO;

    public override bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSOWithInput(inputKitchenObjectSO);
        return fryingRecipeSO != null;
    }

    private FryingRecipeSO GetFryingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (FryingRecipeSO fryingRecipeSO in FryingRecipes)
        {
            if (fryingRecipeSO.input == inputKitchenObjectSO)
            {
                return fryingRecipeSO;
            }
        }
        return null;
    }
    private BurningRecipeSO GetBurningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (BurningRecipeSO burningRecipeSO in BurningRecipes)
        {
            if (burningRecipeSO.input == inputKitchenObjectSO)
            {
                return burningRecipeSO;
            }
        }
        return null;
    }

    public override void SetCookingRecipeSO()
    {
        _fryingRecipeSO = GetFryingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
        CookingTimeMax = _fryingRecipeSO.fryingTimerMax;
    }

    public override void SetBurningRecipeSO(KitchenObjectSO kitchenObjectSO)
    {
        _burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
        BurningTimeMax = _burningRecipeSO.burningTimerMax;
    }

    public override KitchenObjectSO GetCookingOutput()
    {
        return _fryingRecipeSO.output;
    }

    public override KitchenObjectSO GetBurningOutput()
    {
        return _burningRecipeSO.output;
    }
}