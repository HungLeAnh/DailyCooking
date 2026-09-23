using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class OptionalContainerCounterController : BaseCounterController, IHasOptionalSO, IContainerCounter
{
    private NetworkList<ContainerDataSerializable> kitchenObjectSONetworkList = new NetworkList<ContainerDataSerializable>();

    public override void OnNetworkSpawn()
    {
        GridBuildingSystem.Instance.OnObjectSpawned += GridBuildingSystem_OnObjectSpawned;
        kitchenObjectSONetworkList.OnListChanged += KitchenObjectSONetworkList_OnListChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (GridBuildingSystem.Instance != null)
        {
            GridBuildingSystem.Instance.OnObjectSpawned -= GridBuildingSystem_OnObjectSpawned;
        }
        kitchenObjectSONetworkList.OnListChanged -= KitchenObjectSONetworkList_OnListChanged;
    }

    private void KitchenObjectSONetworkList_OnListChanged(NetworkListEvent<ContainerDataSerializable> changeEvent)
    {

    }

    private void GridBuildingSystem_OnObjectSpawned()
    {
        if (!IsHost || !IsServer)
            return;
        PlacedObjectView placedObjectView = GetComponent<PlacedObjectView>();
        if (placedObjectView == null)
        {
            Debug.LogError("ContainerCounterController: PlacedObjectView is null!");
            return;
        }
        var position = placedObjectView.GetGridPositionList();
        string placedObjectTypeSOGuid = placedObjectView.GetPlacedObjectTypeSOGuid();
        var gridObjectDatas = GameManager.Instance.GameData.GridData.GetGridObjectDatas(position[0].x, position[0].y);
        if (gridObjectDatas == null)
        {
            Debug.LogError("ContainerCounterController: GridObjectDatas is null!");
            return;
        }
        foreach (var gridObjectData in gridObjectDatas)
        {
            //Debug.Log("ContainerCounterController: Checking grid object data with placed object type SO guid: " + gridObjectData.ToString());
            if (gridObjectData.PlacedObjectTypeSOGuid == placedObjectTypeSOGuid)
            {
                if (gridObjectData is ContainerData containerData)
                {
                    //Debug.Log("ContainerCounterController: Found matching grid object data! ContainerData: " + containerData.ContainerDataSerializableList[0].KitchenObjectSOGuid);
                    if (containerData != null)
                    {
                        containerData.ContainerDataSerializableList.ForEach(containerDataSerializable =>
                        {
                            kitchenObjectSONetworkList.Add(containerDataSerializable);
                        });
                        break;
                    }
                    else
                    {
                        Debug.LogError("ContainerCounterController: ContainerData is null!"); break;
                    }
                }
                else
                {
                    Debug.LogWarning("is not containerData");
                }

            }
        }
    }
    // Runs on the server (see PlayerStateMachine.InteractServerRpc).
    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            if (kitchenObjectSONetworkList == null || kitchenObjectSONetworkList.Count == 0)
            {
                playerStateMachine.ShowAlert("This is empty!");
                return;
            }
            // One entry per list slot so the picked index matches kitchenObjectSONetworkList.
            var options = new List<KitchenObjectSO>();
            foreach (var containerData in kitchenObjectSONetworkList)
                options.Add(KitchenGameManager.Instance.GetKitchenObjectSOByGuid(containerData.KitchenObjectSOGuid.ToString()));
            playerStateMachine.ShowOptionMenu(this, options, "Select ingredient to make: ");
        }
        else if (playerStateMachine.GetKitchenObject() is RefillerKitchenObject refillerKitchenObject)
        {
            if (refillerKitchenObject.RefillContainer(this))
                refillerKitchenObject.DestroySelf();
        }
    }

    // Server: the actor picked an ingredient; index is into kitchenObjectSONetworkList.
    public void ApplyOption(PlayerStateMachine actor, int index)
    {
        if (actor.HasKitchenObject() || index < 0 || index >= kitchenObjectSONetworkList.Count)
            return;
        var containerData = kitchenObjectSONetworkList[index];
        var kitchenObjectSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(containerData.KitchenObjectSOGuid.ToString());
        if (kitchenObjectSO == null)
        {
            Debug.LogError($"ApplyOption: KitchenObjectSO not found for guid {containerData.KitchenObjectSOGuid}");
            return;
        }
        if (KitchenObject.SpawnKitchenObject(kitchenObjectSO, actor) != null)
            TakeOneFromContainer(index);
    }

    public List<KitchenObjectSO> GetContainerKitchenObjectType()
    {
        return kitchenObjectSONetworkList.AsNativeArray().ToList()
            .Select(container => KitchenGameManager.Instance.GetKitchenObjectSOByGuid(container.KitchenObjectSOGuid.ToString()))
            .Where(so => so != null)
            .ToList();
    }

    // Server only.
    public bool Refill(float fillAmount, string kitchenObjectSOGuid)
    {
        if (!IsServer) return false;
        kitchenObjectSONetworkList.Add(new ContainerDataSerializable(kitchenObjectSOGuid, fillAmount));
        SaveContainerData();
        return true;
    }

    // Server only: one ingredient of entry index was taken out.
    private void TakeOneFromContainer(int index)
    {
        var containerData = kitchenObjectSONetworkList[index];
        if (containerData.FillAmount - 1f <= 0f)
        {
            kitchenObjectSONetworkList.RemoveAt(index);
        }
        else
        {
            containerData.FillAmount--;
            kitchenObjectSONetworkList[index] = containerData;
        }
        SaveContainerData();
    }

    // Server only: the host's GridData is the save; clients read the NetworkList.
    private void SaveContainerData()
    {
        PlacedObjectView placedObjectView = GetComponent<PlacedObjectView>();
        string guid = placedObjectView.GetPlacedObjectTypeSOGuid();
        placedObjectView.GetGridPositionList().ForEach(gridPosition =>
        {
            GameManager.Instance.GameData.GridData.ChangeGridObjectData(gridPosition.x, gridPosition.y,
                new ContainerData( kitchenObjectSONetworkList.AsNativeArray().ToList(), guid, gridPosition, placedObjectView.Dir, placedObjectView.InventoryTabType),
                placedObjectView.GetPlacedObjectTypeSOGuid());
        });
    }
    public override bool CanRemove()
    {
        var canRemove = base.CanRemove();
        if(kitchenObjectSONetworkList != null)
            return canRemove && kitchenObjectSONetworkList.Count == 0;
        return canRemove;
    }
}
