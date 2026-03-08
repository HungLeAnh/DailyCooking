
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GridObjectSpawner
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
        GridBuildingSystem.Instance.SpawnObjectServerRpc(objectData.PlacedObjectTypeSOGuid,objectData.Origin,objectData.Dir);
    }

    public static bool IsObjectPlaced(GridXZ<GridObject> grid, PlacedObjectTypeSO placedObjectTypeSO, Vector2Int origin, Dir dir)
    {
        bool canBuild = true;

        var gridObject = grid.GetGridObject((int)origin.x, (int)origin.y);
        if (gridObject == null)
        {
            canBuild = false;
            return canBuild;
        }
        foreach (var placedObject in gridObject)
        {
            if (placedObject == null ||
                !placedObject.CanBuild(placedObjectTypeSO.itemType.TabType, dir))
            {
                canBuild = false;
                break;
            }
        }
        return canBuild;
    }

}
