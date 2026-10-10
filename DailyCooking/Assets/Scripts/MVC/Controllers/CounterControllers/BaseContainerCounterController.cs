using System.Collections.Generic;
using UnityEngine;

// Shared plumbing for counters whose contents live in the host's GridData save and are
// replicated to clients: rehydrate when the grid spawns, and write back on every change.
public abstract class BaseContainerCounterController : BaseCounterController, IContainerCounter
{
    public override void OnNetworkSpawn()
    {
        GridBuildingSystem.Instance.OnObjectSpawned += GridBuildingSystem_OnObjectSpawned;
        OnContainerNetworkSpawned();
    }

    public override void OnNetworkDespawn()
    {
        if (GridBuildingSystem.Instance != null)
            GridBuildingSystem.Instance.OnObjectSpawned -= GridBuildingSystem_OnObjectSpawned;
        OnContainerNetworkDespawned();
    }

    // Subclasses wire their NetworkVariable/NetworkList listeners here.
    protected virtual void OnContainerNetworkSpawned() { }
    protected virtual void OnContainerNetworkDespawned() { }

    private void GridBuildingSystem_OnObjectSpawned()
    {
        if (!IsServer)
            return;
        PlacedObjectView placedObjectView = GetComponent<PlacedObjectView>();
        if (placedObjectView == null)
        {
            Debug.LogError($"{GetType().Name}: PlacedObjectView is null!");
            return;
        }
        var position = placedObjectView.GetGridPositionList();
        string placedObjectTypeSOGuid = placedObjectView.GetPlacedObjectTypeSOGuid();
        var gridObjectDatas = GameManager.Instance.GameData.GridData.GetGridObjectDatas(position[0].x, position[0].y);
        if (gridObjectDatas == null)
        {
            Debug.LogError($"{GetType().Name}: GridObjectDatas is null!");
            return;
        }
        foreach (var gridObjectData in gridObjectDatas)
        {
            if (gridObjectData.PlacedObjectTypeSOGuid != placedObjectTypeSOGuid)
                continue;
            if (!(gridObjectData is ContainerData containerData))
            {
                Debug.LogWarning("is not containerData");
                continue;
            }
            LoadContainerData(containerData);
            break;
        }
    }

    // Server: reads this counter's saved contents out of the grid cell that was spawned.
    protected abstract void LoadContainerData(ContainerData containerData);

    // Server: the host's GridData is the save; clients read the replicated state instead.
    protected void SaveContainerData()
    {
        PlacedObjectView placedObjectView = GetComponent<PlacedObjectView>();
        string guid = placedObjectView.GetPlacedObjectTypeSOGuid();
        placedObjectView.GetGridPositionList().ForEach(gridPosition =>
        {
            GameManager.Instance.GameData.GridData.ChangeGridObjectData(gridPosition.x, gridPosition.y,
                new ContainerData(CollectContainerData(), guid, gridPosition, placedObjectView.Dir, placedObjectView.InventoryTabType),
                placedObjectView.GetPlacedObjectTypeSOGuid());
        });
    }

    protected abstract List<ContainerDataSerializable> CollectContainerData();

    public abstract List<KitchenObjectSO> GetContainerKitchenObjectType();

    // Server only. Returns false when the container cannot take this ingredient.
    public abstract bool Refill(float fillAmount, string kitchenObjectSOGuid);
}
