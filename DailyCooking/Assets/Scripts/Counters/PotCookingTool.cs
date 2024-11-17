using System;
using System.Collections.Generic;
using UnityEngine;

public class PotCookingTool : CookingTool,IHasOptionalSO
{
    [SerializeField] private CombineRecipeSO[] combineRecipeSOArray;
    [SerializeField] private BurningRecipeSO[] burningRecipeSOArray;
    [SerializeField] private CombineDetailUI _combineDetailUI;

    private CombineRecipeSO _combineRecipeSO;
    private BurningRecipeSO _burningRecipeSO;
    private List<KitchenObjectSO> kitchenObjectSOList = new List<KitchenObjectSO>();

    public override void UpdateCookingState(State state, float cookingtime = 0)
    {
        base.UpdateCookingState(state, cookingtime);
    }
    public override bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        CombineRecipeSO combineRecipeSO = GetCombineRecipesSOWithInput(inputKitchenObjectSO);
        return combineRecipeSO != null;

    }

    private BurningRecipeSO GetBurningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (BurningRecipeSO burningRecipeSO in burningRecipeSOArray)
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
        foreach (var combineRecipe in combineRecipeSOArray)
        {
            if(combineRecipe.input.Contains(kitchenObjectSO))
                return combineRecipe;
        }
        return null;
    }    
    private CombineRecipeSO GetCombineRecipeSOFromOutput(KitchenObjectSO kitchenObjectSO)
    {
        foreach (var combineRecipe in combineRecipeSOArray)
        {
            if(combineRecipe.output == kitchenObjectSO)
                return combineRecipe;
        }
        return null;
    }

    public override void SetCookingRecipeSO(KitchenObjectSO kitchenObjectSO)
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
        _combineRecipeSO = combineRecipeSOArray[index];
        CookingTimeMax = _combineRecipeSO.combineTimerMax;

        _combineDetailUI.InitUI(_combineRecipeSO);
    }

    public List<KitchenObjectSO> GetListKitchenObjectList(KitchenObjectSO kitchenObjectSO)
    {
        List<KitchenObjectSO> kitchenObjectSOs = new List<KitchenObjectSO>();
        foreach (var combineRecipe in combineRecipeSOArray)
        {
            if (combineRecipe.input.Contains(kitchenObjectSO))
                kitchenObjectSOs.Add(combineRecipe.output);
        }
        return kitchenObjectSOs;
    }
}