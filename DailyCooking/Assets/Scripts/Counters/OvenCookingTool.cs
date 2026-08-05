using UnityEngine;

public class OvenCookingTool : CookingTool
{
    private BakingRecipeSO[] BakingRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetBakingRecipes() ?? System.Array.Empty<BakingRecipeSO>();
    private BurningRecipeSO[] BurningRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetBurningRecipes() ?? System.Array.Empty<BurningRecipeSO>();

    private BakingRecipeSO _bakingRecipeSO;
    private BurningRecipeSO _burningRecipeSO;

    public override bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        BakingRecipeSO bakingRecipeSO = GetBakingRecipeSOWithInput(inputKitchenObjectSO);
        return bakingRecipeSO != null;
    }

    private BakingRecipeSO GetBakingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (BakingRecipeSO bakingRecipeSO in BakingRecipes)
        {
            if (bakingRecipeSO.input == inputKitchenObjectSO)
            {
                return bakingRecipeSO;
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
        _bakingRecipeSO = GetBakingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
        CookingTimeMax = _bakingRecipeSO.bakingTimerMax;
    }

    public override void SetBurningRecipeSO(KitchenObjectSO kitchenObjectSO)
    {
        _burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
        BurningTimeMax = _burningRecipeSO.burningTimerMax;
    }

    public override KitchenObjectSO GetCookingOutput()
    {
        return _bakingRecipeSO.output;
    }

    public override KitchenObjectSO GetBurningOutput()
    {
        return _burningRecipeSO.output;
    }
}
