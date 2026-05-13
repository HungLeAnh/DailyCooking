using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class ContainerCounterController : BaseCounterController, IContainerCounter
{
    private NetworkVariable<float> fillAmount = new NetworkVariable<float>(0f);
    private NetworkVariable<FixedString64Bytes> kitchenObjectSOGuid = new NetworkVariable<FixedString64Bytes>();
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
                    Debug.Log("ContainerCounterController: Found matching grid object data! ContainerData: " + containerData.KitchenObjectSOGuid);
                    if (containerData != null)
                    {
                        this.kitchenObjectSOGuid.Value = containerData.KitchenObjectSOGuid;
                        this.fillAmount.Value = containerData.FillAmount;
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
        Debug.Log("ContainerCounterController: InteractEvent called! Player has kitchen object: " + playerStateMachine.HasKitchenObject() + ", fill amount: " + fillAmount.Value);
        if (!playerStateMachine.HasKitchenObject() && fillAmount.Value > 0f )
        {
            KitchenObjectSO kitchenObjectSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(kitchenObjectSOGuid.Value.ToString());
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, playerStateMachine);
            fillAmount.Value--;
            if(fillAmount.Value <= 0f)
            {
                kitchenObjectSOGuid.Value = "";
            }
            UpdateContainerData();
        }
        else
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
        return new List<KitchenObjectSO> { KitchenGameManager.Instance.GetKitchenObjectSOByGuid(kitchenObjectSOGuid.Value.ToString()) };
    }

    public void Refill(float fillAmount, string kitchenObjectSOGuid)
    {
        Debug.Log("Trying to refill container counter with guid: " + kitchenObjectSOGuid);
        if(kitchenObjectSOGuid == this.kitchenObjectSOGuid.Value.ToString() || string.IsNullOrEmpty(this.kitchenObjectSOGuid.Value.ToString()))
        {
            PlacedObjectView placedObjectView = GetComponent<PlacedObjectView>();
            string guid = placedObjectView.GetPlacedObjectTypeSOGuid();
            placedObjectView.GetGridPositionList().ForEach(gridPosition =>
            {
                 GameManager.Instance.GameData.GridData.ChangeGridObjectData(gridPosition.x, gridPosition.y,
                     new ContainerData(kitchenObjectSOGuid, fillAmount, guid, gridPosition, placedObjectView.Dir, placedObjectView.InventoryTabType),
                     placedObjectView.GetPlacedObjectTypeSOGuid());
            });
            this.fillAmount.Value = fillAmount;
            this.kitchenObjectSOGuid.Value = kitchenObjectSOGuid;

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
                new ContainerData(kitchenObjectSOGuid.Value.ToString(), fillAmount.Value, guid, gridPosition, placedObjectView.Dir, placedObjectView.InventoryTabType),
                placedObjectView.GetPlacedObjectTypeSOGuid());
        });
    }
}
