using UnityEngine;

public class BeverageCookingTool : CookingTool
{
    private DrinkRecipeSO[] DrinkRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetDrinkRecipes() ?? System.Array.Empty<DrinkRecipeSO>();
    private BurningRecipeSO[] BurningRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetBurningRecipes() ?? System.Array.Empty<BurningRecipeSO>();

    private DrinkRecipeSO _drinkRecipeSO;
    private BurningRecipeSO _burningRecipeSO;

    public override bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        DrinkRecipeSO drinkRecipeSO = GetDrinkRecipesSOWithInput(inputKitchenObjectSO);
        return drinkRecipeSO != null;
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

    private DrinkRecipeSO GetDrinkRecipesSOWithInput(KitchenObjectSO kitchenObjectSO)
    {
        foreach (var drinkRecipe in DrinkRecipes)
        {
            if (drinkRecipe.input.Contains(kitchenObjectSO))
                return drinkRecipe;
        }
        return null;
    }

    public override void SetCookingRecipeSO()
    {
        _drinkRecipeSO = GetDrinkRecipesSOWithInput(GetKitchenObject().GetKitchenObjectSO());
        CookingTimeMax = _drinkRecipeSO.drinkTimerMax;
    }

    public override void SetBurningRecipeSO(KitchenObjectSO kitchenObjectSO)
    {
        _burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
        BurningTimeMax = _burningRecipeSO.burningTimerMax;
    }

    public override KitchenObjectSO GetCookingOutput()
    {
        return _drinkRecipeSO.output;
    }

    public override KitchenObjectSO GetBurningOutput()
    {
        return _burningRecipeSO.output;
    }
}
