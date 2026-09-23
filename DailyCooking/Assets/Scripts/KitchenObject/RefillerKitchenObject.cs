using Unity.Collections;
using Unity.Netcode;

public class RefillerKitchenObject : KitchenObject
{
    // The ingredient this box refills, replicated so clients can check it too.
    private readonly NetworkVariable<FixedString64Bytes> refillKitchenObjectSOGuid = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public KitchenObjectSO RefillKitchenObjectSO
    {
        get
        {
            string guid = refillKitchenObjectSOGuid.Value.ToString();
            if (string.IsNullOrEmpty(guid) || KitchenGameManager.Instance == null)
                return null;
            return KitchenGameManager.Instance.GetKitchenObjectSOByGuid(guid);
        }
    }

    // Server only.
    public void SetRefillKitchenObject(KitchenObjectSO value)
    {
        if (!IsServer) return;
        refillKitchenObjectSOGuid.Value = value != null ? value.Guid : string.Empty;
    }

    // Server only. Returns true when the container accepted the refill.
    public bool RefillContainer(IContainerCounter containerCounter)
    {
        if (!IsServer || containerCounter == null) return false;
        KitchenObjectSO refillSO = RefillKitchenObjectSO;
        if (refillSO == null) return false;
        if (!(GetKitchenObjectSO() is RefillerKitchenObjectSO refillerSO)) return false;
        return containerCounter.Refill(refillerSO.refillingAmount, refillSO.Guid);
    }
}
