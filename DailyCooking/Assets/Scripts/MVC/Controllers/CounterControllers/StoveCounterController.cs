using System;
using UnityEngine;

public class StoveCounterController : BaseCounterController
{
    [SerializeField] private CookingTool _cookingTool;

    protected override void OnRestartGame(object sender, PlayerStateMachine e)
    {
        base.OnRestartGame(sender, e);

        if (_cookingTool.HasKitchenObject())
            _cookingTool.GetKitchenObject().DestroySelf();

        _cookingTool.ClearKitchenObject();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!_cookingTool.HasKitchenObject())
        {
            if (playerStateMachine.HasKitchenObject())
            {
                if (_cookingTool.HasRecipeWithInput(playerStateMachine.GetKitchenObject().GetKitchenObjectSO()))
                {
                    playerStateMachine.GetKitchenObject().SetKitchenObjectParent(_cookingTool);
                    _cookingTool.SetCookingRecipeSO();
                    _cookingTool.UpdateCookingState(CookingTool.State.Cooking);
                }
            }
        }
        else
        {
            if (playerStateMachine.HasKitchenObject())
            {
                if (playerStateMachine.GetKitchenObject().TryGetTableware(out TablewareKitchenObject plateKitchenObject))
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
                _cookingTool.GetKitchenObject().SetKitchenObjectParent(playerStateMachine);
                _cookingTool.UpdateCookingState(CookingTool.State.Idle);
            }
        }
    }

    public float GetProgress()
    {
        return _cookingTool.GetProgress();
    }

    public bool IsDone()
    {
        return _cookingTool.IsDone();
    }
}
