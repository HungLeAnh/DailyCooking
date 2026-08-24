using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CookingToolCounterController : ClearCounterController, IHasOptionalSO
{
    [SerializeField] private List<CookingToolConfigSO.CookingToolType> acceptedToolTypes = new List<CookingToolConfigSO.CookingToolType>();

    private CookingTool _cookingTool;
    private CookingToolItem _installedToolItem;

    public bool HasToolInstalled() => _installedToolItem != null;

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
        // 1) Player is carrying a cooking tool -> try to install it on this counter.
        if (playerStateMachine.HasKitchenObject())
        {
            CookingToolItem toolItem = playerStateMachine.GetKitchenObject().GetComponent<CookingToolItem>();
            if (toolItem != null)
            {
                if (!HasToolInstalled() && IsCompatible(toolItem))
                {
                    InstallTool(toolItem, playerStateMachine);
                }
                return;
            }
        }

        // 2) A tool is installed -> normal cooking interaction (or remove when empty).
        if (HasToolInstalled())
        {
            HandleCookingInteraction(playerStateMachine);
            return;
        }

        // 3) No tool installed -> default counter behavior.
        base.InteractEvent(playerStateMachine);
    }

    private bool IsCompatible(CookingToolItem toolItem)
    {
        CookingToolConfigSO config = toolItem.CookingTool != null ? toolItem.CookingTool.GetConfig() : null;
        if (config == null)
            return false;

        foreach (CookingToolConfigSO.CookingToolType type in config.EffectiveToolTypes)
        {
            if (acceptedToolTypes.Contains(type))
                return true;
        }
        return false;
    }

    private void InstallTool(CookingToolItem toolItem, PlayerStateMachine player)
    {
        NetworkObject counterNetworkObject = GetNetworkObject();
        toolItem.NetworkObject.TrySetParent(counterNetworkObject.transform, false);
        toolItem.DisableFollow();
        toolItem.transform.localPosition = Vector3.zero;
        toolItem.transform.localRotation = Quaternion.identity;

        _installedToolItem = toolItem;
        _cookingTool = toolItem.CookingTool;

        player.ClearKitchenObject();
    }

    private void RemoveTool(PlayerStateMachine player)
    {
        if (_installedToolItem == null)
            return;

        CookingToolItem toolItem = _installedToolItem;
        toolItem.NetworkObject.TrySetParent((Transform)null);
        toolItem.EnableFollow(player.GetKitchenObjectFollowTransform());
        player.SetKitchenObject(toolItem);

        _installedToolItem = null;
        _cookingTool = null;
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
                // Empty-handed on an empty tool -> detach the tool back to the player.
                RemoveTool(playerStateMachine);
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
