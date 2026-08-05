using UnityEngine;

public class DeepFryCookingTool : CookingTool
{
    private DeepFryRecipeSO[] DeepFryRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetDeepFryRecipes() ?? System.Array.Empty<DeepFryRecipeSO>();
    private BurningRecipeSO[] BurningRecipes => KitchenGameManager.Instance.RecipeDatabase?.GetBurningRecipes() ?? System.Array.Empty<BurningRecipeSO>();

    private DeepFryRecipeSO _deepFryRecipeSO;
    private BurningRecipeSO _burningRecipeSO;

    public override bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        DeepFryRecipeSO deepFryRecipeSO = GetDeepFryRecipeSOWithInput(inputKitchenObjectSO);
        return deepFryRecipeSO != null;
    }

    private DeepFryRecipeSO GetDeepFryRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (DeepFryRecipeSO deepFryRecipeSO in DeepFryRecipes)
        {
            if (deepFryRecipeSO.input == inputKitchenObjectSO)
            {
                return deepFryRecipeSO;
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
        _deepFryRecipeSO = GetDeepFryRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
        CookingTimeMax = _deepFryRecipeSO.deepFryTimerMax;
    }

    public override void SetBurningRecipeSO(KitchenObjectSO kitchenObjectSO)
    {
        _burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
        BurningTimeMax = _burningRecipeSO.burningTimerMax;
    }

    public override KitchenObjectSO GetCookingOutput()
    {
        return _deepFryRecipeSO.output;
    }

    public override KitchenObjectSO GetBurningOutput()
    {
        return _burningRecipeSO.output;
    }
}
