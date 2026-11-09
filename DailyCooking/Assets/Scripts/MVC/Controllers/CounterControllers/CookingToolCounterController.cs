using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CookingToolCounterController : ClearCounterController, IHasOptionalSO, IHasProgress
{
    private CookingTool _cookingTool;

    public bool HasToolInstalled() => TryResolveTool();

    public bool HasFood() => TryResolveTool() && _cookingTool.HasAnyFood;

    private bool TryResolveTool()
    {
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
        if (_cookingTool != null && _cookingTool.HasAnyFood)
            return false;
        return base.CanRemove();
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
        // Manual-start tools (the Blender) commit their ingredients: once something is inside,
        // nothing comes back out until the finished dish is ready. Players may still add more
        // ingredients while the tool is idle, which is what keeps multi-ingredient drinks possible.
        if (_cookingTool.RequiresManualStart && _cookingTool.CurrentState == CookingTool.State.Idle)
        {
            if (playerStateMachine.HasKitchenObject())
                TryAddInput(playerStateMachine, playerStateMachine.GetKitchenObject());
            return;
        }

        if (!_cookingTool.HasAnyFood)
        {
            if (playerStateMachine.HasKitchenObject())
                TryAddInput(playerStateMachine, playerStateMachine.GetKitchenObject());
            return;
        }

        // Beverage tools consume their input: the raw ingredient is not available
        // again until the drink is ready.
        if (_cookingTool.CurrentState == CookingTool.State.Cooking && _cookingTool.IsBeverageTool)
            return;

        if (playerStateMachine.HasKitchenObject())
        {
            KitchenObject heldObject = playerStateMachine.GetKitchenObject();
            if (heldObject.TryGetTableware(out TablewareKitchenObject plateKitchenObject))
            {
                if (_cookingTool.TryGetFirstFood(out KitchenObject food, out _) &&
                    plateKitchenObject.TryAddIngredient(food.GetKitchenObjectSO()))
                {
                    food.DestroySelf();
                    _cookingTool.UpdateCookingState(CookingTool.State.Idle);
                }
                return;
            }

            // Multi-ingredient tools (the Blender) take more ingredients while they still fit.
            TryAddInput(playerStateMachine, heldObject);
            return;
        }

        if (_cookingTool.TryGetFirstFood(out KitchenObject firstFood, out _))
        {
            firstFood.SetKitchenObjectParent(playerStateMachine);
            _cookingTool.UpdateCookingState(CookingTool.State.Idle);
        }
    }

    // Server: moves the held ingredient into the tool when the recipe still accepts it.
    private void TryAddInput(PlayerStateMachine playerStateMachine, KitchenObject heldObject)
    {
        if (heldObject == null)
            return;
        KitchenObjectSO inputKitchenObjectSO = heldObject.GetKitchenObjectSO();
        if (!_cookingTool.CanAcceptInput(inputKitchenObjectSO))
        {
            // Refuse the ingredient and say so, whatever the tool: a silent rejection leaves
            // the player pressing interact with no idea why nothing happens.
            if (UIManager.Instance != null)
                UIManager.Instance.ShowAlertMessage("This ingredient doesn't fit this tool.");
            return;
        }

        int slot = _cookingTool.GetFirstFreeSlotIndex();
        if (slot < 0)
        {
            if (_cookingTool.RequiresManualStart && UIManager.Instance != null)
                UIManager.Instance.ShowAlertMessage("This tool is full.");
            return;
        }
        if (!heldObject.SetKitchenObjectParent(_cookingTool, slot))
            return;

        if (_cookingTool.RequiresOptionChoice(inputKitchenObjectSO))
        {
            // Pending choice: stay Idle so server guard doesn't soft-lock in Cooking with 0 time.
            // Popup cancel leaves food in Idle, player can take it back.
            _cookingTool.UpdateCookingState(CookingTool.State.Idle);
            _cookingTool.ShowOptionMenu(playerStateMachine, this, inputKitchenObjectSO);
            return;
        }

        _cookingTool.SetCookingRecipeSO();
        if (_cookingTool.IsCuttingActive)
        {
            // Cutting progresses one click at a time; the timed state would
            // auto-advance the bar and play the cooking sizzle.
            _cookingTool.UpdateCookingState(CookingTool.State.Idle);
        }
        else if (_cookingTool.RequiresManualStart)
        {
            // The Blender waits for the player to press the start button, so a matching set
            // stays idle until then (more ingredients can still be added).
            _cookingTool.UpdateCookingState(CookingTool.State.Idle);
        }
        else if (_cookingTool.HasValidRecipe())
        {
            _cookingTool.UpdateCookingState(CookingTool.State.Cooking);
        }
        else
        {
            // Multi-input recipe still missing ingredients: keep the tool idle and visible.
            _cookingTool.UpdateCookingState(CookingTool.State.Idle);
        }
    }

    public override void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
        if (!TryResolveTool())
            return;
        if (!_cookingTool.HasAnyFood)
            return;
        _cookingTool.Cut(playerStateMachine);
    }

    // True when the HUD start button should be offered for this tool: a manual-start tool
    // (the Blender) holding a complete recipe and waiting for the press.
    public bool CanPressStart()
    {
        return TryResolveTool() && _cookingTool.RequiresManualStart && _cookingTool.IsReadyToStart;
    }

    // Server: the player pressed the HUD start button.
    public bool StartCooking()
    {
        return TryResolveTool() && _cookingTool.TryStartCooking();
    }

    // PlayerStateMachine treats -1 as "no alternate action on this interactable".
    // Only a cutting tool with food offers the alternate action (one cut per click).
    // Starting a manual-start tool goes through the HUD button, and everything else
    // (place / take back / plate transfer / frying) stays on the normal interact path.
    public float GetProgress()
    {
        if (!TryResolveTool() || !_cookingTool.HasAnyFood || !_cookingTool.IsCuttingActive)
            return -1f;
        return _cookingTool.GetProgress();
    }

    public bool IsDone()
    {
        return TryResolveTool() && _cookingTool.IsDone();
    }

    // Server: the actor picked a cutting or combine option for the food waiting in the tool.
    public void ApplyOption(PlayerStateMachine actor, int index)
    {
        if (!TryResolveTool() || _cookingTool.CurrentState != CookingTool.State.Idle)
            return;
        if (!_cookingTool.ApplyOption(index))
            return;
        if (_cookingTool.IsCuttingActive)
            _cookingTool.UpdateCookingState(CookingTool.State.Idle);
        else if (!_cookingTool.RequiresManualStart && _cookingTool.CookingTimeMax > 0f)
            _cookingTool.UpdateCookingState(CookingTool.State.Cooking);
    }

    public List<KitchenObjectSO> GetListKitchenObjectList(KitchenObjectSO kitchenObjectSO)
    {
        return TryResolveTool() ? _cookingTool.GetListKitchenObjectList(kitchenObjectSO) : new List<KitchenObjectSO>();
    }

}
