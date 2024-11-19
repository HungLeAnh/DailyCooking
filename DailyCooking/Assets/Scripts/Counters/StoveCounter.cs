using System;
using UnityEngine;

public class StoveCounter : BaseCounter
{
    [SerializeField] private CookingTool _cookingTool;

    private void Start()
    {
        _cookingTool.CurrentState = CookingTool.State.Idle;
    }

    public override void Interact(PlayerStateMachine playerStateMachine)
    {
        if (!_cookingTool.HasKitchenObject())
        {
            //There is no kitchen object
            if (playerStateMachine.HasKitchenObject())
            {
                KitchenObjectSO kitchenObjectSO = playerStateMachine.GetKitchenObject().GetKitchenObjectSO();
                //Player is carrying something
                if (_cookingTool.HasRecipeWithInput(kitchenObjectSO))
                {
                    //Player is carrying something that can be fried
                    //IHasOptionalSO option = (IHasOptionalSO)_cookingTool;
                    //if (option != null)
                    //{
                    //    FireOnShowCombineRecipe(option.GetListKitchenObjectList(kitchenObjectSO));
                    //}

                    playerStateMachine.GetKitchenObject().SetKitchenObjectParent(_cookingTool);
                    
                    _cookingTool.SetCookingRecipeSO();

                    _cookingTool.UpdateCookingState(CookingTool.State.Cooking,0f);
                    _cookingTool.FireOnProgressChanged(_cookingTool.CookingTimer / _cookingTool.CookingTimeMax);
                }
            }
            else
            {
                //Player is not carrying anything
            }
        }
        else
        {
            //There is kitchen object here
            if (playerStateMachine.HasKitchenObject())
            {
                //Player is carrying something
                if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject plateKitchenObject))
                {
                    //Player is holding a plate
                    if (plateKitchenObject.TryAddIngredient(_cookingTool.GetKitchenObject().GetKitchenObjectSO()))
                    {
                        _cookingTool.GetKitchenObject().DestroySelf();
                        
                        _cookingTool.UpdateCookingState(CookingTool.State.Idle, 0f);
                        _cookingTool.FireOnProgressChanged(0f);
                    }
                }
            }
            else
            {
                //Player is not carrying anything
                _cookingTool.GetKitchenObject().SetKitchenObjectParent(playerStateMachine);

                _cookingTool.UpdateCookingState(CookingTool.State.Idle, 0f);

                _cookingTool.FireOnProgressChanged(0f);

            }
        }
    }
}
