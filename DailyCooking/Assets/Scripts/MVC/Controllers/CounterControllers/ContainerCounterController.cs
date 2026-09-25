using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ContainerCounterController : BaseCounterController, IContainerCounter
{
    [SerializeField] private Image kitchenObjectImage;

    private NetworkVariable<ContainerDataSerializable> networkContainerData = new NetworkVariable<ContainerDataSerializable>(new ContainerDataSerializable());
    public override void OnNetworkSpawn()
    {
        kitchenObjectImage.gameObject.SetActive(false);
        GridBuildingSystem.Instance.OnObjectSpawned += GridBuildingSystem_OnObjectSpawned;
        networkContainerData.OnValueChanged += NetworkContainerData_OnValueChanged;
        NetworkContainerData_OnValueChanged(default(ContainerDataSerializable), networkContainerData.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (GridBuildingSystem.Instance != null)
        {
            GridBuildingSystem.Instance.OnObjectSpawned -= GridBuildingSystem_OnObjectSpawned;
        }
        networkContainerData.OnValueChanged -= NetworkContainerData_OnValueChanged;
    }

    private void NetworkContainerData_OnValueChanged(ContainerDataSerializable previousValue, ContainerDataSerializable newValue)
    {
        if (newValue.FillAmount > 0f && !string.IsNullOrEmpty(newValue.KitchenObjectSOGuid.ToString()))
        {
            kitchenObjectImage.gameObject.SetActive(true);
            kitchenObjectImage.sprite = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(newValue.KitchenObjectSOGuid.ToString()).Sprite;
        }
        else
        {
            kitchenObjectImage.gameObject.SetActive(false);
            kitchenObjectImage.sprite = null;
        }
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
        foreach ( var gridObjectData in gridObjectDatas )
        {
            //Debug.Log("ContainerCounterController: Checking grid object data with placed object type SO guid: " + gridObjectData.ToString());
            if (gridObjectData.PlacedObjectTypeSOGuid == placedObjectTypeSOGuid)
            {
                if (gridObjectData is ContainerData containerData)
                {
                    if (containerData != null && containerData.ContainerDataSerializableList != null && containerData.ContainerDataSerializableList.Count>0)
                    {
                        //Debug.Log("ContainerCounterController: Found matching grid object data! ContainerData: " + containerData.ContainerDataSerializableList[0].KitchenObjectSOGuid);
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

    // Runs on the server (see PlayerStateMachine.InteractServerRpc).
    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            if(string.IsNullOrEmpty(networkContainerData.Value.KitchenObjectSOGuid.ToString()) || networkContainerData.Value.FillAmount <= 0f)
            {
                UIManager.Instance.ShowAlert(playerStateMachine, "This is empty!");
                return;
            }
            KitchenObjectSO kitchenObjectSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(networkContainerData.Value.KitchenObjectSOGuid.ToString());
            if (KitchenObject.SpawnKitchenObject(kitchenObjectSO, playerStateMachine) != null)
                TakeOneFromContainer();
        }
        else if (playerStateMachine.GetKitchenObject() is RefillerKitchenObject refillerKitchenObject)
        {
            if (refillerKitchenObject.RefillContainer(this))
                refillerKitchenObject.DestroySelf();
            else
                UIManager.Instance.ShowAlert(playerStateMachine, "Cannot refill container with different ingredient type ");
        }
    }

    public List<KitchenObjectSO> GetContainerKitchenObjectType()
    {
        return new List<KitchenObjectSO> { KitchenGameManager.Instance.GetKitchenObjectSOByGuid(networkContainerData.Value.KitchenObjectSOGuid.ToString()) };
    }

    // Server only. Returns false when the container holds a different ingredient.
    public bool Refill(float fillAmount, string kitchenObjectSOGuid)
    {
        if (!IsServer) return false;
        string currentGuid = this.networkContainerData.Value.KitchenObjectSOGuid.ToString();
        bool isEmpty = string.IsNullOrEmpty(currentGuid) || this.networkContainerData.Value.FillAmount <= 0f;
        if (!isEmpty && kitchenObjectSOGuid != currentGuid)
            return false;

        this.networkContainerData.Value = new ContainerDataSerializable(kitchenObjectSOGuid, fillAmount);
        SaveContainerData();
        return true;
    }

    // Server only: one ingredient was taken out.
    private void TakeOneFromContainer()
    {
        var data = networkContainerData.Value;
        if (networkContainerData.Value.FillAmount - 1f <= 0f)
        {
            networkContainerData.Value = new ContainerDataSerializable();
        }
        else
        {
            data.FillAmount--;
            networkContainerData.Value = data;
        }
        SaveContainerData();
    }
    // Server only: the host's GridData is the save; clients read the NetworkVariable.
    private void SaveContainerData()
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
    public override bool CanRemove()
    {
        var canRemove = base.CanRemove();

        return canRemove && (string.IsNullOrEmpty(networkContainerData.Value.KitchenObjectSOGuid.ToString()) || networkContainerData.Value.FillAmount == 0f);
    }
}
