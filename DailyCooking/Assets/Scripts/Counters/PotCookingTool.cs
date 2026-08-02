using System;
using System.Collections.Generic;
using UnityEngine;

public class PotCookingTool : CookingTool,IHasOptionalSO
{
    private CombineRecipeSO[] CombineRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetCombineRecipes() ?? System.Array.Empty<CombineRecipeSO>();
    private BurningRecipeSO[] BurningRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetBurningRecipes() ?? System.Array.Empty<BurningRecipeSO>();
    [SerializeField] private CombineDetailUI _combineDetailUI;

    private CombineRecipeSO _combineRecipeSO;
    private BurningRecipeSO _burningRecipeSO;
    private List<KitchenObjectSO> kitchenObjectSOList = new List<KitchenObjectSO>();

    public override bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        CombineRecipeSO combineRecipeSO = GetCombineRecipesSOWithInput(inputKitchenObjectSO);
        return combineRecipeSO != null;
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
    private CombineRecipeSO GetCombineRecipesSOWithInput(KitchenObjectSO kitchenObjectSO)
    {
        foreach (var combineRecipe in CombineRecipes)
        {
            if(combineRecipe.input.Contains(kitchenObjectSO))
                return combineRecipe;
        }
        return null;
    }
    private CombineRecipeSO GetCombineRecipeSOFromOutput(KitchenObjectSO kitchenObjectSO)
    {
        foreach (var combineRecipe in CombineRecipes)
        {
            if(combineRecipe.output == kitchenObjectSO)
                return combineRecipe;
        }
        return null;
    }

    public override void SetCookingRecipeSO()
    {

    }

    public override void SetBurningRecipeSO(KitchenObjectSO kitchenObjectSO)
    {
        _burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
        BurningTimeMax = _burningRecipeSO.burningTimerMax;
    }

    public override KitchenObjectSO GetCookingOutput()
    {
        return _combineRecipeSO.output;
    }

    public override KitchenObjectSO GetBurningOutput()
    {
        return _burningRecipeSO.output;
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        _combineRecipeSO = CombineRecipes[index];
        CookingTimeMax = _combineRecipeSO.combineTimerMax;

        _combineDetailUI.InitUI(_combineRecipeSO);
    }

    public List<KitchenObjectSO> GetListKitchenObjectList(KitchenObjectSO kitchenObjectSO)
    {
        List<KitchenObjectSO> kitchenObjectSOs = new List<KitchenObjectSO>();
        foreach (var combineRecipe in CombineRecipes)
        {
            if (combineRecipe.input.Contains(kitchenObjectSO))
                kitchenObjectSOs.Add(combineRecipe.output);
        }
        return kitchenObjectSOs;
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        throw new NotImplementedException();
    }
}