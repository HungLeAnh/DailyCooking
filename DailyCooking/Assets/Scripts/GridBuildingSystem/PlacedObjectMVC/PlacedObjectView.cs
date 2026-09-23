using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// A grid-placed object. Spawned only by the server (GridBuildingSystem.SpawnPlacedObject);
// every peer, including late joiners, registers it in its own grid when it spawns.
[Serializable]
public class PlacedObjectView : NetworkBehaviour
{
    private NetworkVariable<FixedString64Bytes> placedObjectTypeSOGuid = new NetworkVariable<FixedString64Bytes>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2Int> origin = new NetworkVariable<Vector2Int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Dir> dir = new NetworkVariable<Dir>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private PlacedObjectTypeSO placedObjectTypeSO;
    private bool isRegistered;
    public Vector2Int Origin => origin.Value;
    public Dir Dir => dir.Value;
    public PlacedObjectTypeSO PlacedObjectTypeSO => placedObjectTypeSO;
    public InventoryTabType InventoryTabType => placedObjectTypeSO.itemType.TabType;

    public override void OnNetworkSpawn()
    {
        placedObjectTypeSOGuid.OnValueChanged += PlacedObjectTypeSOGuid_OnValueChanged;
        placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(placedObjectTypeSOGuid.Value.ToString());

        // The grid exists once GridBuildingSystem has initialized (a joining client waits for
        // the host's game data first).
        if (GridBuildingSystem.Instance.GridManager != null)
            RegisterInGrid();
        else
            GridBuildingSystem.Instance.OnObjectSpawned += GridBuildingSystem_OnObjectSpawned;
    }

    public override void OnNetworkDespawn()
    {
        if (GridBuildingSystem.Instance != null)
        {
            GridBuildingSystem.Instance.OnObjectSpawned -= GridBuildingSystem_OnObjectSpawned;
            UnregisterFromGrid();
        }
        placedObjectTypeSOGuid.OnValueChanged -= PlacedObjectTypeSOGuid_OnValueChanged;
    }

    private void GridBuildingSystem_OnObjectSpawned()
    {
        GridBuildingSystem.Instance.OnObjectSpawned -= GridBuildingSystem_OnObjectSpawned;
        RegisterInGrid();
    }

    private void PlacedObjectTypeSOGuid_OnValueChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(newValue.ToString());
    }

    private void RegisterInGrid()
    {
        if (isRegistered || placedObjectTypeSO == null)
            return;
        var grid = GridBuildingSystem.Instance.GridManager?.Grid;
        if (grid == null)
        {
            Debug.LogError("GridManager is null");
            return;
        }
        isRegistered = true;
        foreach (var gridPosition in GetGridPositionList())
        {
            grid.AddGridObjectData(gridPosition.x, gridPosition.y, new GridObject(grid, this, gridPosition.x, gridPosition.y));
        }
        this.GetComponent<IModuleItem>()?.RegisterItem();
    }

    // Only the in-memory grid: the saved GridData entry is removed explicitly by the server when
    // a player picks the object up, so despawning on shutdown never erases the save.
    private void UnregisterFromGrid()
    {
        if (!isRegistered)
            return;
        isRegistered = false;
        var grid = GridBuildingSystem.Instance.GridManager?.Grid;
        if (grid == null)
            return;
        foreach (var gridPosition in GetGridPositionList())
        {
            List<GridObject> cellObjects = grid.GetGridObject(gridPosition.x, gridPosition.y);
            if (cellObjects == null)
                continue;
            if (cellObjects.RemoveAll(gridObject => gridObject != null && gridObject.GetPlacedObject() == this) > 0)
                grid.TriggerGridObjectChanged(gridPosition.x, gridPosition.y);
        }
    }

    // Server only, before Spawn().
    public void Intialize(string placedObjectTypeSOGuid, Vector2Int origin, Dir dir)
    {
        this.placedObjectTypeSOGuid.Value = placedObjectTypeSOGuid;
        this.origin.Value = origin;
        this.dir.Value = dir;
    }

    public List<Vector2Int> GetGridPositionList()
    {
        this.placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(placedObjectTypeSOGuid.Value.ToString());
        return placedObjectTypeSO.GetGridPositionList(origin.Value, dir.Value);
    }

    public override string ToString()
    {
        return placedObjectTypeSO != null ? placedObjectTypeSO.nameString : base.ToString();
    }

    public string GetPlacedObjectTypeSOGuid()
    {
        return placedObjectTypeSOGuid.Value.ToString();
    }
}
