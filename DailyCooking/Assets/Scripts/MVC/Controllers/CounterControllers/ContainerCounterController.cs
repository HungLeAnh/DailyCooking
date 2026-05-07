using System.Collections.Generic;
using UnityEngine;

public class ContainerCounterController : BaseCounterController, IContainerCounter
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    private float fillAmount = 0f;
    private string kitchenObjectSOGuid;

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
                        this.kitchenObjectSOGuid = containerData.KitchenObjectSOGuid;
                        this.fillAmount = containerData.FillAmount;
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
        if (kitchenObjectSO != null)
        {
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, playerStateMachine);
            return;
        }

        if (!playerStateMachine.HasKitchenObject() && fillAmount > 0f )
        {
            KitchenObjectSO kitchenObjectSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(kitchenObjectSOGuid);
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, playerStateMachine);
            fillAmount--;
            if(fillAmount <= 0f)
            {
                kitchenObjectSOGuid = null;
            }
        }
        else
        {
            if (playerStateMachine.GetKitchenObject().IsRefiller())
            {
                playerStateMachine.GetKitchenObject().RefillContainerServerRpc(this);
            }
        }
    }

    public List<KitchenObjectSO> GetContainerKitchenObjectType()
    {
        return new List<KitchenObjectSO> { KitchenGameManager.Instance.GetKitchenObjectSOByGuid(kitchenObjectSOGuid)};
    }

    public void Refill(float fillAmount, string kitchenObjectSOGuid)
    {
        Debug.Log("Trying to refill container counter with guid: " + kitchenObjectSOGuid);
        if(kitchenObjectSOGuid == this.kitchenObjectSOGuid || string.IsNullOrEmpty(this.kitchenObjectSOGuid))
        {
            PlacedObjectView placedObjectView = GetComponent<PlacedObjectView>();
            string guid = placedObjectView.GetPlacedObjectTypeSOGuid();
            placedObjectView.GetGridPositionList().ForEach(gridPosition =>
            {
                 GameManager.Instance.GameData.GridData.ChangeGridObjectData(gridPosition.x, gridPosition.y,
                     new ContainerData(kitchenObjectSOGuid, fillAmount, guid, gridPosition, placedObjectView.Dir, placedObjectView.InventoryTabType),
                     placedObjectView.GetPlacedObjectTypeSOGuid());
            });

        }
        else
        {
            Debug.LogError("Trying to refill container counter with different kitchen object type! Current guid: " + this.kitchenObjectSOGuid + " trying to refill with guid: " + kitchenObjectSOGuid);
        }

    }

}
