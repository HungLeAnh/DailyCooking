using System.Collections.Generic;
using UnityEngine;

// Single source for "can this object go here" and "where does it sit", shared by the
// client preview, the server's placement check and loading saved grids.
public static class PlacementRules
{
    // requireUnlockedCells: false when respawning saved/default objects, true for players.
    public static bool CanPlace(GridXZ<GridObject> grid, PlacedObjectTypeSO placedObjectTypeSO, Vector2Int origin, Dir dir, bool requireUnlockedCells)
    {
        if (grid == null || placedObjectTypeSO == null)
            return false;

        foreach (Vector2Int cell in placedObjectTypeSO.GetGridPositionList(origin, dir))
        {
            List<GridObject> cellObjects = grid.GetGridObject(cell.x, cell.y);
            if (cellObjects == null)
                return false;
            if (requireUnlockedCells && !grid.IsCellUnlocked(cell.x, cell.y))
                return false;

            if (placedObjectTypeSO.isTool)
            {
                // Tools sit in a free top slot of an allowed counter (Pan/Pot on StoveCounter, etc.).
                if (!ToolSlotResolver.TryFindUnderlyingCounter(grid, placedObjectTypeSO, cell, out PlacedObjectView counterView) ||
                    ToolSlotResolver.IsToolSlotOccupied(grid, counterView, cell))
                    return false;
                continue;
            }

            foreach (GridObject gridObject in cellObjects)
            {
                if (gridObject == null || !gridObject.CanBuild(placedObjectTypeSO.itemType.TabType, dir))
                    return false;
            }
        }
        return true;
    }

    public static Vector3 GetWorldPosition(GridXZ<GridObject> grid, PlacedObjectTypeSO placedObjectTypeSO, Vector2Int origin, Dir dir)
    {
        Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
        Vector3 worldPosition = grid.GetWorldPosition(origin) +
            new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();
        if (placedObjectTypeSO.isTool &&
            ToolSlotResolver.TryFindUnderlyingCounter(grid, placedObjectTypeSO, origin, out PlacedObjectView counterView) &&
            ToolSlotResolver.TryGetToolSlotPosition(counterView, origin, out Vector3 slotPosition))
        {
            worldPosition = slotPosition;
        }
        return worldPosition;
    }
}
