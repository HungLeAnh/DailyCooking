using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class OptionalContainerCounterController : BaseCounterController, IHasOptionalSO, IContainerCounter
{
    private NetworkList<ContainerDataSerializable> kitchenObjectSONetworkList = new NetworkList<ContainerDataSerializable>();

    private PlayerStateMachine playerStateMachine;
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
    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            if (kitchenObjectSONetworkList == null || kitchenObjectSONetworkList.Count == 0)
            {
                UIManager.Instance.ShowAlertMessage($"This is empty!");
                return;
            }
            //Debug.Log("Player does not have kitchen object, showing option menu");
            this.playerStateMachine = playerStateMachine;
            var kitchenObjectSOList = GetContainerKitchenObjectType();
            OnShowOptionMenu(kitchenObjectSOList);

        }
        else if (playerStateMachine.HasKitchenObject())
        {
            //Debug.Log("Player has kitchen object:" + playerStateMachine.GetKitchenObject());
            if (playerStateMachine.GetKitchenObject() is RefillerKitchenObject refillerKitchenObject)
            {
                refillerKitchenObject.RefillContainerServerRpc(this);

                playerStateMachine.GetKitchenObject().DestroySelf();

            }
        }
    }

    public void SetOptionKitchenObjectSO(int index)
    {
        if (!this.playerStateMachine.HasKitchenObject())
        {
            var containerData = kitchenObjectSONetworkList[index];
            // ContainerDataSerializable stores the SO guid in KitchenObjectSOGuid
            string soGuid = containerData.KitchenObjectSOGuid.ToString();
            var kitchenObjectSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(soGuid);

            if (kitchenObjectSO != null)
            {

                UpdateContainerData(containerData,index);
                KitchenObject.SpawnKitchenObject(kitchenObjectSO, this.playerStateMachine);
            }
            else
            {
                Debug.LogError($"SetOptionKitchenObjectSO: KitchenObjectSO not found for guid {soGuid}");
            }
        }
        this.playerStateMachine = null;
    }

    public void OnShowOptionMenu(List<KitchenObjectSO> kitchenObjectSOList)
    {
        if(GridBuildingSystem.Instance.BuildingPlacementManager.IsBuilding)
            return;
        //Debug.Log("Showing option menu for container counter with " + kitchenObjectSOList.Count + " options");
        UIPopupManager.Instance.ShowPopup(
        UIPopupType.UIOptionMenuPopup,
        new UIOptionMenuPopup.Param
        {
            sender = this,
            optionalList = kitchenObjectSOList,
            Title = "Select ingredient to make: "
        });
    }

    public List<KitchenObjectSO> GetContainerKitchenObjectType()
    {
        return kitchenObjectSONetworkList.AsNativeArray().ToList()
            .Select(container => KitchenGameManager.Instance.GetKitchenObjectSOByGuid(container.KitchenObjectSOGuid.ToString()))
            .Where(so => so != null)
            .ToList();
    }

    public void Refill(float fillAmount, string kitchenObjectSOGuid)
    {
        PlacedObjectView placedObjectView = GetComponent<PlacedObjectView>();
        string guid = placedObjectView.GetPlacedObjectTypeSOGuid();
        kitchenObjectSONetworkList.Add(new ContainerDataSerializable(kitchenObjectSOGuid, fillAmount));
        placedObjectView.GetGridPositionList().ForEach(gridPosition =>
        {
            GameManager.Instance.GameData.GridData.ChangeGridObjectData(gridPosition.x, gridPosition.y,
                new ContainerData(kitchenObjectSONetworkList.AsNativeArray().ToList(), guid, gridPosition, placedObjectView.Dir, placedObjectView.InventoryTabType),
                placedObjectView.GetPlacedObjectTypeSOGuid());
        });
        
    }
    public void UpdateContainerData(ContainerDataSerializable containerData,int index)
    {
        UpdateContainerDataServerRpc(containerData,index);
    }
    [Rpc(SendTo.Server)]
    private void UpdateContainerDataServerRpc(ContainerDataSerializable containerData,int index)
    {
        if (containerData.FillAmount - 1f <= 0f)
        {
            kitchenObjectSONetworkList.RemoveAt(index);
        }
        else
        {
            containerData.FillAmount--;
            kitchenObjectSONetworkList[index] = containerData;
        }
        UpdateContainerDataClientRpc();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateContainerDataClientRpc()
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
