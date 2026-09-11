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

    public bool HasToolInstalled() => TryResolveTool();

    public bool HasFood() => TryResolveTool() && _cookingTool.HasKitchenObject();

    // Child-first (scene-parented setups), then same grid cell (stacked placed tools).
    private bool TryResolveTool()
    {
        if (_cookingTool == null)
            _cookingTool = GetComponentInChildren<CookingTool>();
        if (_cookingTool == null)
            _cookingTool = FindStackedToolOnCell();
        return _cookingTool != null;
    }

    private CookingTool FindStackedToolOnCell()
    {
        foreach (PlacedObjectView view in ViewsOnCellsOf(GetComponent<PlacedObjectView>()))
        {
            if (view == null || view.gameObject == gameObject)
                continue;
            CookingTool tool = view.GetComponentInChildren<CookingTool>();
            if (tool != null)
                return tool;
        }
        return null;
    }

    // Pan clicked directly (not IInteractable): route to this counter's controller.
    public static CookingToolCounterController FindControllerForTool(CookingTool tool)
    {
        if (tool == null)
            return null;
        CookingToolCounterController ancestor = tool.GetComponentInParent<CookingToolCounterController>();
        if (ancestor != null)
            return ancestor;
        PlacedObjectView toolView = tool.GetComponent<PlacedObjectView>();
        foreach (PlacedObjectView view in ViewsOnCellsOf(toolView))
        {
            if (view == null || (toolView != null && view.gameObject == toolView.gameObject))
                continue;
            CookingToolCounterController controller = view.GetComponent<CookingToolCounterController>();
            if (controller != null)
                return controller;
        }
        return null;
    }

    private static IEnumerable<PlacedObjectView> ViewsOnCellsOf(PlacedObjectView view)
    {
        GridBuildingSystem gbs = GridBuildingSystem.Instance;
        if (view == null || gbs == null || gbs.GridManager?.Grid == null)
            yield break;
        List<Vector2Int> cells;
        try { cells = view.GetGridPositionList(); }
        catch { yield break; }
        if (cells == null)
            yield break;
        foreach (Vector2Int cell in cells)
        {
            List<GridObject> cellObjects = null;
            try { cellObjects = gbs.GridManager.Grid.GetGridObject(cell.x, cell.y); }
            catch { continue; }
            if (cellObjects == null)
                continue;
            foreach (GridObject gridObject in cellObjects)
            {
                PlacedObjectView other = gridObject?.GetPlacedObject();
                if (other != null)
                    yield return other;
            }
        }
    }

    public override bool CanRemove()
    {
        // Don't yank the counter out from under a tool that holds food.
        TryResolveTool();
        if (_cookingTool != null && _cookingTool.HasKitchenObject())
            return false;
        return base.CanRemove();
    }

    protected override void OnRestartGame(object sender)
    {
        base.OnRestartGame(sender);

        TryResolveTool();
        if (_cookingTool != null && _cookingTool.HasKitchenObject())
            _cookingTool.GetKitchenObject().DestroySelf();

        if (_cookingTool != null)
            _cookingTool.ClearKitchenObject();
    }

    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        // Tools are grid-placed (stacked on the counter cell) or scene-parented; resolve either way.
        if (!TryResolveTool())
        {
            base.InteractEvent(playerStateMachine);
            return;
        }

        HandleCookingInteraction(playerStateMachine);
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
                    if (_cookingTool.RequiresOptionChoice(inputKitchenObjectSO))
                    {
                        // Pending choice: stay Idle so server guard doesn't soft-lock in Cooking with 0 time.
                        // Popup cancel leaves food in Idle, player can take it back.
                        _cookingTool.UpdateCookingState(CookingTool.State.Idle);
                        _cookingTool.ShowLocalOptionMenu(inputKitchenObjectSO);
                    }
                    else
                    {
                        _cookingTool.SetCookingRecipeSO();
                        if (_cookingTool.HasValidRecipe())
                            _cookingTool.UpdateCookingState(CookingTool.State.Cooking);
                    }
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
        return TryResolveTool() ? _cookingTool.GetProgress() : 0f;
    }

    public bool IsDone()
    {
        return TryResolveTool() && _cookingTool.IsDone();
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        if (TryResolveTool())
        {
            _cookingTool.SetOptionKitchenObjectSO(index);
            if (_cookingTool.CookingTimeMax > 0f)
                _cookingTool.UpdateCookingState(CookingTool.State.Cooking);
        }
    }

    public List<KitchenObjectSO> GetListKitchenObjectList(KitchenObjectSO kitchenObjectSO)
    {
        return TryResolveTool() ? _cookingTool.GetListKitchenObjectList(kitchenObjectSO) : new List<KitchenObjectSO>();
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
    }
}
