using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// One-slot container: interacts pull the stored ingredient directly and show the sprite.
public class ContainerCounterController : BaseContainerCounterController
{
    [SerializeField] private Image kitchenObjectImage;

    private NetworkVariable<ContainerDataSerializable> networkContainerData = new NetworkVariable<ContainerDataSerializable>(new ContainerDataSerializable());

    protected override void OnContainerNetworkSpawned()
    {
        kitchenObjectImage.gameObject.SetActive(false);
        networkContainerData.OnValueChanged += NetworkContainerData_OnValueChanged;
        NetworkContainerData_OnValueChanged(default(ContainerDataSerializable), networkContainerData.Value);
    }

    protected override void OnContainerNetworkDespawned()
    {
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

    protected override void LoadContainerData(ContainerData containerData)
    {
        var savedList = containerData.ContainerDataSerializableList;
        if (savedList == null || savedList.Count == 0)
        {
            Debug.LogError("ContainerCounterController: ContainerData is empty!");
            return;
        }
        networkContainerData.Value = savedList[0];
    }

    protected override List<ContainerDataSerializable> CollectContainerData()
    {
        return new List<ContainerDataSerializable> { networkContainerData.Value };
    }

    // Runs on the server (see PlayerStateMachine.InteractServerRpc).
    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            if (string.IsNullOrEmpty(networkContainerData.Value.KitchenObjectSOGuid.ToString()) || networkContainerData.Value.FillAmount <= 0f)
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

    public override List<KitchenObjectSO> GetContainerKitchenObjectType()
    {
        return new List<KitchenObjectSO> { KitchenGameManager.Instance.GetKitchenObjectSOByGuid(networkContainerData.Value.KitchenObjectSOGuid.ToString()) };
    }

    // Server only. Returns false when the container holds a different ingredient.
    public override bool Refill(float fillAmount, string kitchenObjectSOGuid)
    {
        if (!IsServer) return false;
        string currentGuid = networkContainerData.Value.KitchenObjectSOGuid.ToString();
        bool isEmpty = string.IsNullOrEmpty(currentGuid) || networkContainerData.Value.FillAmount <= 0f;
        if (!isEmpty && kitchenObjectSOGuid != currentGuid)
            return false;

        networkContainerData.Value = new ContainerDataSerializable(kitchenObjectSOGuid, fillAmount);
        SaveContainerData();
        return true;
    }

    // Server only: one ingredient was taken out.
    private void TakeOneFromContainer()
    {
        var data = networkContainerData.Value;
        if (data.FillAmount - 1f <= 0f)
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

    public override bool CanRemove()
    {
        return base.CanRemove() &&
            (string.IsNullOrEmpty(networkContainerData.Value.KitchenObjectSOGuid.ToString()) || networkContainerData.Value.FillAmount == 0f);
    }
}
