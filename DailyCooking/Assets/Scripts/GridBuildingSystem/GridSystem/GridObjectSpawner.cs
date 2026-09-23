
using System.Collections.Generic;

// Server only: respawns saved (or default) grid objects.
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

    // Multi-cell objects are saved once per covered cell; after the first copy spawns, the
    // placement check rejects the others because their cells are already taken.
    private static void SpawnObject(GridXZ<GridObject> grid, GridObjectData objectData)
    {
        PlacedObjectTypeSO placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(objectData.PlacedObjectTypeSOGuid);
        if (!PlacementRules.CanPlace(grid, placedObjectTypeSO, objectData.Origin, objectData.Dir, requireUnlockedCells: false))
            return;
        GridBuildingSystem.Instance.SpawnPlacedObject(placedObjectTypeSO, objectData.Origin, objectData.Dir);
    }
}
