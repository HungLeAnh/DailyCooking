using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Multi-slot container: interacts open a pick menu over every stocked ingredient.
public class OptionalContainerCounterController : BaseContainerCounterController, IHasOptionalSO
{
    private NetworkList<ContainerDataSerializable> kitchenObjectSONetworkList = new NetworkList<ContainerDataSerializable>();

    protected override void LoadContainerData(ContainerData containerData)
    {
        var savedList = containerData.ContainerDataSerializableList;
        if (savedList == null)
        {
            Debug.LogError("OptionalContainerCounterController: ContainerData list is null!");
            return;
        }
        savedList.ForEach(kitchenObjectSONetworkList.Add);
    }

    protected override List<ContainerDataSerializable> CollectContainerData()
    {
        return kitchenObjectSONetworkList.AsNativeArray().ToList();
    }

    // Runs on the server (see PlayerStateMachine.InteractServerRpc).
    public override void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        if (!playerStateMachine.HasKitchenObject())
        {
            if (kitchenObjectSONetworkList == null || kitchenObjectSONetworkList.Count == 0)
            {
                UIManager.Instance.ShowAlert(playerStateMachine, "This is empty!");
                return;
            }
            // One entry per list slot so the picked index matches kitchenObjectSONetworkList.
            var options = new List<KitchenObjectSO>();
            foreach (var containerData in kitchenObjectSONetworkList)
                options.Add(KitchenGameManager.Instance.GetKitchenObjectSOByGuid(containerData.KitchenObjectSOGuid.ToString()));
            UIManager.Instance.ShowOptionMenu(playerStateMachine, this, options, "Select ingredient to make: ");
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

    public override List<KitchenObjectSO> GetContainerKitchenObjectType()
    {
        return kitchenObjectSONetworkList.AsNativeArray().ToList()
            .Select(container => KitchenGameManager.Instance.GetKitchenObjectSOByGuid(container.KitchenObjectSOGuid.ToString()))
            .Where(so => so != null)
            .ToList();
    }

    // Server only.
    public override bool Refill(float fillAmount, string kitchenObjectSOGuid)
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

    public override bool CanRemove()
    {
        var canRemove = base.CanRemove();
        if (kitchenObjectSONetworkList != null)
            return canRemove && kitchenObjectSONetworkList.Count == 0;
        return canRemove;
    }
}
