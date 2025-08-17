using System;
using UnityEngine;

public class StoveCounterService
{
    private readonly CookingTool _cookingTool;

    public StoveCounterService(CookingTool cookingTool)
    {
        _cookingTool = cookingTool;
    }

    public void Interact(IKitchenObjectParent counter, IKitchenObjectParent player)
    {
        if (!_cookingTool.HasKitchenObject())
        {
            if (player.HasKitchenObject())
            {
                if (_cookingTool.HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO()))
                {
                    player.GetKitchenObject().SetKitchenObjectParent(_cookingTool);
                    _cookingTool.SetCookingRecipeSO();
                    _cookingTool.UpdateCookingState(CookingTool.State.Cooking);
                }
            }
        }
        else
        {
            if (player.HasKitchenObject())
            {
                if (player.GetKitchenObject().TryGetTableware(out TablewareKitchenObject plateKitchenObject))
                {
                    if (plateKitchenObject.TryAddIngredient(_cookingTool.GetKitchenObject().GetKitchenObjectSO()))
                    {
                        _cookingTool.GetKitchenObject().DestroySelf();
                        _cookingTool.UpdateCookingState(CookingTool.State.Idle);
                    }
                }
            }
            else
            {
                _cookingTool.GetKitchenObject().SetKitchenObjectParent(player);
                _cookingTool.UpdateCookingState(CookingTool.State.Idle);
            }
        }
    }

}
