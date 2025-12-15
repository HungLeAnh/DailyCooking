
using System.Collections.Generic;
using UnityEngine;

public static class GridObjectSpawner
{
    public static void SpawnObjectsFromData(GridXZ<GridObject> grid, List<GridObjectData>[,] gridObjectDataList)
    {
        if (gridObjectDataList == null) return;
        for(int x = 0; x < gridObjectDataList.GetLength(0); x++)
        {
            for(int z = 0; z < gridObjectDataList.GetLength(1); z++)
            {
                if(gridObjectDataList[x, z] == null) continue;
                foreach (var objectData in gridObjectDataList[x,z])
                {
                    SpawnObject(grid, objectData);
                }
            }
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
        if (!IsObjectPlaced(grid,placedObjectTypeSO,objectData))
        {
            return;
        }
        PlacedObjectView placedObject = PlacedObjectFactory.Create(placedObjectWorldPosition, objectData.Origin, objectData.Dir, placedObjectTypeSO);

        foreach (var gridPosition in gridPositionList)
        {
            grid.AddGridObjectData(gridPosition.x, gridPosition.y,
                new GridObject(grid, placedObject, gridPosition.x, gridPosition.y));
        }
        placedObject.GetComponent<IPlaceable>().IsPlaced = true;
    }
    public static bool IsObjectPlaced(GridXZ<GridObject> grid, PlacedObjectTypeSO placedObjectTypeSO, GridObjectData objectData)
    {
        bool canBuild = true;

        var gridObject = grid.GetGridObject((int)objectData.Origin.x, (int)objectData.Origin.y);
        if (gridObject == null)
        {
            canBuild = false;
            return canBuild;
        }
        foreach (var placedObject in gridObject)
        {
            if (placedObject == null ||
                !placedObject.CanBuild(placedObjectTypeSO.itemType.TabType, objectData.Dir))
            {
                canBuild = false;
                break;
            }
        }
        return canBuild;
    }
}
