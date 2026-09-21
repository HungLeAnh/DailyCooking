using System.Collections.Generic;
using UnityEngine;

// Resolves where a grid-placed cooking tool must sit: the explicit top slot
// of its underlying counter (slot index = tool cell position within the
// counter's GetGridPositionList() order). Single source for live placement,
// saved-grid respawn and ghost preview.
public static class ToolSlotResolver
{
    public static bool TryFindUnderlyingCounter(GridXZ<GridObject> grid, PlacedObjectTypeSO toolSO, Vector2Int toolOrigin, out PlacedObjectView counterView)
    {
        counterView = null;
        if (grid == null || toolSO == null) return false;
        List<GridObject> cell = grid.GetGridObject(toolOrigin.x, toolOrigin.y);
        if (cell == null) return false;
        foreach (var placedObject in cell)
        {
            if (placedObject == null) continue;
            PlacedObjectView view = placedObject.GetPlacedObject();
            if (view == null) continue;
            if (toolSO.allowedUnderlyingCounters != null && toolSO.allowedUnderlyingCounters.Count > 0)
            {
                foreach (var allowed in toolSO.allowedUnderlyingCounters)
                {
                    if (allowed != null && view.GetPlacedObjectTypeSOGuid() == allowed.Guid)
                    {
                        counterView = view;
                        return true;
                    }
                }
            }
            else if (view.InventoryTabType == InventoryTabType.Counter)
            {
                counterView = view;
                return true;
            }
        }
        return false;
    }

    public static bool TryGetToolSlotPosition(PlacedObjectView counterView, Vector2Int toolOrigin, out Vector3 slotPosition)
    {
        slotPosition = default;
        if (counterView == null) return false;
        int slotIndex = GetToolSlotIndex(counterView, toolOrigin);

        BaseCounterController controller = counterView.GetComponent<BaseCounterController>();
        Transform top = null;
        if (controller != null)
        {
            int count = controller.GetTopSlotCount();
            if (count > 0)
                top = controller.GetTopSlotTransform(Mathf.Min(slotIndex, count - 1));
        }
        if (top != null)
        {
            slotPosition = top.position;
            return true;
        }
        Collider col = counterView.GetComponentInChildren<Collider>();
        if (col != null)
        {
            slotPosition = new Vector3(counterView.transform.position.x, col.bounds.max.y, counterView.transform.position.z);
            return true;
        }
        Renderer rend = counterView.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            slotPosition = new Vector3(counterView.transform.position.x, rend.bounds.max.y, counterView.transform.position.z);
            return true;
        }
        return false;
    }

    public static int GetToolSlotIndex(PlacedObjectView counterView, Vector2Int toolOrigin)
    {
        if (counterView == null) return 0;
        List<Vector2Int> cells = null;
        try { cells = counterView.GetGridPositionList(); }
        catch { cells = null; }
        int slotIndex = cells != null ? cells.IndexOf(toolOrigin) : -1;
        return slotIndex < 0 ? 0 : slotIndex;
    }

    public static bool IsToolSlotOccupied(GridXZ<GridObject> grid, PlacedObjectView counterView, Vector2Int toolOrigin)
    {
        if (grid == null || counterView == null) return false;
        int slotIndex = GetToolSlotIndex(counterView, toolOrigin);
        List<GridObject> cell = grid.GetGridObject(toolOrigin.x, toolOrigin.y);
        if (cell == null) return false;
        foreach (var placedObject in cell)
        {
            if (placedObject == null) continue;
            PlacedObjectView view = placedObject.GetPlacedObject();
            if (view == null || view == counterView) continue;
            PlacedObjectTypeSO viewSO = view.PlacedObjectTypeSO;
            if (viewSO == null && GridBuildingSystem.Instance != null)
                viewSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(view.GetPlacedObjectTypeSOGuid());
            if (viewSO == null || !viewSO.isTool) continue;
            if (GetToolSlotIndex(counterView, view.Origin) == slotIndex)
                return true;
        }
        return false;
    }
}
