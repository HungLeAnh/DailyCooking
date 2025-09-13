
using System.Collections.Generic;
using UnityEngine;

public static class GridObjectSpawner
{
    public static void SpawnObjectsFromData(GridXZ<GridObject> grid, List<GridObjectData> gridObjectDataList)
    {
        if (gridObjectDataList == null) return;

        foreach (var objectData in gridObjectDataList)
        {
            SpawnObject(grid, objectData);
        }
    }

    private static void SpawnObject(GridXZ<GridObject> grid, GridObjectData objectData)
    {
        PlacedObjectTypeSO placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(objectData.PlacedObjectTypeSOGuid);
        if (placedObjectTypeSO == null) return;

        List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(objectData.Origin, objectData.Dir);
        Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(objectData.Dir);
        Vector3 placedObjectWorldPosition = grid.GetWorldPosition(objectData.Origin) +
            new Vector3(rotationOffset.x, 0, rotationOffset.y) * grid.GetCellSize();

        PlacedObjectView placedObject = PlacedObjectFactory.Create(placedObjectWorldPosition, objectData.Origin, objectData.Dir, placedObjectTypeSO);

        foreach (var gridPosition in gridPositionList)
        {
            var gridObject = grid.GetGridObject(gridPosition.x, gridPosition.y);
            gridObject?.SetPlacedObject(placedObject);
        }
    }
}
