using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CookingToolCounterController : ClearCounterController, IHasOptionalSO
{
    // Legacy: dedicated counter no longer hosts carryable CookingToolItem.
    // Cooking tools are now grid-placed static appliances (PanToolPlaced/PotToolPlaced).
    // This controller is kept for StoveCounter highlight/selection but does not manage tool install.
    [SerializeField] private List<CookingToolConfigSO.CookingToolType> acceptedToolTypes = new List<CookingToolConfigSO.CookingToolType>();

    private CookingTool _cookingTool;

    public bool HasToolInstalled() => _cookingTool != null && _cookingTool.HasKitchenObject() == false ? false : _cookingTool != null;

    protected override void OnRestartGame(object sender)
    {
        base.OnRestartGame(sender);

        if (_cookingTool != null && _cookingTool.HasKitchenObject())
            _cookingTool.GetKitchenObject().DestroySelf();

        if (_cookingTool != null)
            _cookingTool.ClearKitchenObject();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        // Tools are no longer carryable; delegate to base counter behavior.
        // If a static CookingTool is attached as child, forward cooking interaction directly.
        if (_cookingTool == null)
            _cookingTool = GetComponentInChildren<CookingTool>();

        if (_cookingTool != null)
        {
            HandleCookingInteraction(playerStateMachine);
            return;
        }

        base.InteractEvent(playerStateMachine);
    }

    private void HandleCookingInteraction(PlayerStateMachine playerStateMachine)
    {
        if (!_cookingTool.HasKitchenObject())
        {
            if (playerStateMachine.HasKitchenObject())
            {
                KitchenObjectSO inputKitchenObjectSO = playerStateMachine.GetKitchenObject().GetKitchenObjectSO();
                if (_cookingTool.HasRecipeWithInput(inputKitchenObjectSO))
                {
                    playerStateMachine.GetKitchenObject().SetKitchenObjectParent(_cookingTool);
                    _cookingTool.SetCookingRecipeSO();
                    _cookingTool.UpdateCookingState(CookingTool.State.Cooking);
                    _cookingTool.ShowLocalOptionMenu(inputKitchenObjectSO);
                }
            }
            else
            {
                // Empty-handed on empty static tool -> no carry removal (tools are grid-placed)
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
        return _cookingTool != null ? _cookingTool.GetProgress() : 0f;
    }

    public bool IsDone()
    {
        return _cookingTool != null && _cookingTool.IsDone();
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        if (_cookingTool != null)
        {
            _cookingTool.SetOptionKitchenObjectSO(index);
            if (_cookingTool.CookingTimeMax > 0f)
                _cookingTool.UpdateCookingState(CookingTool.State.Cooking);
        }
    }

    public List<KitchenObjectSO> GetListKitchenObjectList(KitchenObjectSO kitchenObjectSO)
    {
        return _cookingTool != null ? _cookingTool.GetListKitchenObjectList(kitchenObjectSO) : new List<KitchenObjectSO>();
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
    }
}
