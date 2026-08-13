using System;
using System.Collections.Generic;
using UnityEngine;

public class CookingToolCounterController : BaseCounterController, IHasOptionalSO
{
    [SerializeField] private CookingTool _cookingTool;

    protected override void OnRestartGame(object sender)
    {
        base.OnRestartGame(sender);

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

    public void SetOptionKitchenObjectSO(int index)
    {
        _cookingTool.SetOptionKitchenObjectSO(index);
        if (_cookingTool.CookingTimeMax > 0f)
            _cookingTool.UpdateCookingState(CookingTool.State.Cooking);
    }

    public List<KitchenObjectSO> GetListKitchenObjectList(KitchenObjectSO kitchenObjectSO)
    {
        return _cookingTool.GetListKitchenObjectList(kitchenObjectSO);
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        throw new NotImplementedException();
    }
}
