using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ContainerCounterController : BaseCounterController, IContainerCounter
{
    private NetworkVariable<ContainerDataSerializable> networkContainerData = new NetworkVariable<ContainerDataSerializable>(new ContainerDataSerializable());
    public override void OnNetworkSpawn()
    {
        GridBuildingSystem.Instance.OnObjectSpawned += GridBuildingSystem_OnObjectSpawned;
    }

    private void GridBuildingSystem_OnObjectSpawned()
    {
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
        foreach ( var gridObjectData in gridObjectDatas )
        {
            Debug.Log("ContainerCounterController: Checking grid object data with placed object type SO guid: " + gridObjectData.ToString());
            if (gridObjectData.PlacedObjectTypeSOGuid == placedObjectTypeSOGuid)
            {
                if (gridObjectData is ContainerData containerData)
                {
                    if (containerData != null && containerData.ContainerDataSerializableList != null && containerData.ContainerDataSerializableList.Count>0)
                    {
                        Debug.Log("ContainerCounterController: Found matching grid object data! ContainerData: " + containerData.ContainerDataSerializableList[0].KitchenObjectSOGuid);
                        this.networkContainerData.Value = containerData.ContainerDataSerializableList[0];
                        break;
                    }
                    else
                    {
                        Debug.LogError("ContainerCounterController: ContainerData is null!"); 
                        break;
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
        Debug.Log("ContainerCounterController: InteractEvent called! Player has kitchen object: " + playerStateMachine.HasKitchenObject() + ", fill amount: " + networkContainerData.Value.FillAmount);

        if (!playerStateMachine.HasKitchenObject() && networkContainerData.Value.FillAmount > 0f )
        {
            if(string.IsNullOrEmpty(networkContainerData.Value.KitchenObjectSOGuid.ToString()))
            {
                Debug.Log("ContainerCounterController: Cannot interact with empty container!");
                return;
            }
            KitchenObjectSO kitchenObjectSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(networkContainerData.Value.KitchenObjectSOGuid.ToString());
            Debug.Log("Spawning kitchen object with SO guid: " + networkContainerData.Value.KitchenObjectSOGuid.ToString() + ", fill amount: " + networkContainerData.Value.FillAmount);
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, playerStateMachine);

            if(networkContainerData.Value.FillAmount - 1f <= 0f)
            {
                networkContainerData.Value = new ContainerDataSerializable("", networkContainerData.Value.FillAmount - 1f);
            }
            else
            {
                networkContainerData.Value = new ContainerDataSerializable(networkContainerData.Value.KitchenObjectSOGuid.ToString(), networkContainerData.Value.FillAmount - 1f);
            }
            UpdateContainerData();
        }
        else if(playerStateMachine.HasKitchenObject() && networkContainerData.Value.FillAmount <= 0f)
        {
            Debug.Log("Player has kitchen object:" + playerStateMachine.GetKitchenObject());
            if (playerStateMachine.GetKitchenObject() is RefillerKitchenObject refillerKitchenObject)
            {
                refillerKitchenObject.RefillContainerServerRpc(this);

                playerStateMachine.GetKitchenObject().DestroySelf();

            }
        }
    }

    public List<KitchenObjectSO> GetContainerKitchenObjectType()
    {
        return new List<KitchenObjectSO> { KitchenGameManager.Instance.GetKitchenObjectSOByGuid(networkContainerData.Value.KitchenObjectSOGuid.ToString()) };
    }

    public void Refill(float fillAmount, string kitchenObjectSOGuid)
    {
        Debug.Log("Trying to refill container counter with guid: " + kitchenObjectSOGuid+ " is empty: " + string.IsNullOrEmpty(this.networkContainerData.Value.KitchenObjectSOGuid.ToString())
            + " is whitespace: " + string.IsNullOrWhiteSpace(this.networkContainerData.Value.KitchenObjectSOGuid.ToString()));
        if(kitchenObjectSOGuid == this.networkContainerData.Value.KitchenObjectSOGuid.ToString() || string.IsNullOrEmpty(this.networkContainerData.Value.KitchenObjectSOGuid.ToString()))
        {
            PlacedObjectView placedObjectView = GetComponent<PlacedObjectView>();
            string guid = placedObjectView.GetPlacedObjectTypeSOGuid();
            placedObjectView.GetGridPositionList().ForEach(gridPosition =>
            {
                 GameManager.Instance.GameData.GridData.ChangeGridObjectData(gridPosition.x, gridPosition.y,
                     new ContainerData(new List<ContainerDataSerializable> { new ContainerDataSerializable(kitchenObjectSOGuid, fillAmount) }, guid, gridPosition, placedObjectView.Dir, placedObjectView.InventoryTabType),
                     placedObjectView.GetPlacedObjectTypeSOGuid());
            });
            this.networkContainerData.Value = new ContainerDataSerializable(kitchenObjectSOGuid, fillAmount);

        }
        else
        {
            UIManager.Instance.ShowAlertMessage("Cannot refill container counter with different ingredient type ");
        }

    }
    public void UpdateContainerData()
    {
        UpdateContainerDataServerRpc();
    }
    [Rpc(SendTo.Server)]
    private void UpdateContainerDataServerRpc()
    {
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
                new ContainerData(new List<ContainerDataSerializable>{networkContainerData.Value}, guid, gridPosition, placedObjectView.Dir, placedObjectView.InventoryTabType),
                placedObjectView.GetPlacedObjectTypeSOGuid());
        });
    }
}
