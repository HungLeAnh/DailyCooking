using System;
using UnityEngine;
using Unity.Netcode;

// Which parent holds a kitchen object, replicated so every peer (and late joiners) agree.
public struct KitchenObjectParentSlot : INetworkSerializable, IEquatable<KitchenObjectParentSlot>
{
    public bool HasParent;
    public NetworkBehaviourReference Parent;
    public int Index;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref HasParent);
        serializer.SerializeValue(ref Parent);
        serializer.SerializeValue(ref Index);
    }

    public bool Equals(KitchenObjectParentSlot other)
    {
        return HasParent == other.HasParent && Parent.Equals(other.Parent) && Index == other.Index;
    }
}

public class KitchenObject : NetworkBehaviour
{
    [SerializeField] protected KitchenObjectSO kitchenObjectSO;
    private readonly NetworkVariable<KitchenObjectParentSlot> parentSlot = new NetworkVariable<KitchenObjectParentSlot>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private IKitchenObjectParent kitchenObjectParent;
    private int kitchenObjectParentIndex;
    private bool isParentPending;
    private FollowTransform kitchenObjectFollowTransform;
    protected virtual void Awake()
    {
        kitchenObjectFollowTransform = GetComponent<FollowTransform>();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        parentSlot.OnValueChanged += ParentSlot_OnValueChanged;
        ApplyParentSlot(parentSlot.Value);
    }
    public override void OnNetworkDespawn()
    {
        parentSlot.OnValueChanged -= ParentSlot_OnValueChanged;
        DetachFromLocalParent();
        base.OnNetworkDespawn();
    }
    protected virtual void Update()
    {
        // A late joiner can spawn this object before its parent; retry until the parent exists.
        if (isParentPending)
            ApplyParentSlot(parentSlot.Value);
    }
    public KitchenObjectSO GetKitchenObjectSO()
    {
        return kitchenObjectSO;
    }
    public KitchenObjectOptionalProcessSO GetKitchenObjectOptionalProcessSO()
    {
        if (kitchenObjectSO == null)
            return null;

        if (kitchenObjectSO.processSO != null)
            return kitchenObjectSO.processSO;
        else
            return null;

    }

    // Server only. Returns false when the target slot is already taken.
    public bool SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent, int index = 0)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"{name}: SetKitchenObjectParent must run on the server.", this);
            return false;
        }
        if (!IsSpawned || !TryCreateParentSlot(kitchenObjectParent, index, out KitchenObjectParentSlot slot))
            return false;
        parentSlot.Value = slot;
        return true;
    }

    // Server only, before Spawn(): the spawn message then already carries the parent.
    public bool InitializeParent(IKitchenObjectParent kitchenObjectParent, int index = 0)
    {
        if (!TryCreateParentSlot(kitchenObjectParent, index, out KitchenObjectParentSlot slot))
            return false;
        parentSlot.Value = slot;
        return true;
    }

    private bool TryCreateParentSlot(IKitchenObjectParent kitchenObjectParent, int index, out KitchenObjectParentSlot slot)
    {
        slot = default;
        if (!(kitchenObjectParent is NetworkBehaviour parentBehaviour) || !parentBehaviour.IsSpawned)
            return false;
        KitchenObject current = kitchenObjectParent.GetKitchenObject(index);
        if (current != null && current != this)
            return false;
        slot = new KitchenObjectParentSlot { HasParent = true, Parent = parentBehaviour, Index = index };
        return true;
    }

    private void ParentSlot_OnValueChanged(KitchenObjectParentSlot previousValue, KitchenObjectParentSlot newValue)
    {
        ApplyParentSlot(newValue);
    }

    private void ApplyParentSlot(KitchenObjectParentSlot slot)
    {
        isParentPending = false;
        if (!slot.HasParent)
        {
            DetachFromLocalParent();
            return;
        }
        if (!slot.Parent.TryGet(out NetworkBehaviour parentBehaviour) || !(parentBehaviour is IKitchenObjectParent newParent))
        {
            isParentPending = true;
            return;
        }
        if (newParent == kitchenObjectParent && slot.Index == kitchenObjectParentIndex)
            return;

        DetachFromLocalParent();
        kitchenObjectParent = newParent;
        kitchenObjectParentIndex = slot.Index;
        newParent.SetKitchenObject(this, slot.Index);
        kitchenObjectFollowTransform.setTargetTransform(newParent.GetKitchenObjectFollowTransform(slot.Index));
    }

    // Clears the slot this object occupied, using the index it was stored under.
    private void DetachFromLocalParent()
    {
        if (kitchenObjectParent == null)
            return;
        if (kitchenObjectParent as UnityEngine.Object != null &&
            kitchenObjectParent.GetKitchenObject(kitchenObjectParentIndex) == this)
        {
            kitchenObjectParent.ClearKitchenObject(kitchenObjectParentIndex);
        }
        kitchenObjectParent = null;
    }

    public IKitchenObjectParent GetKitchenObjectParent()
    {
        return kitchenObjectParent;
    }

    // Server only. Despawning clears the parent slot on every peer (OnNetworkDespawn).
    public void DestroySelf(int index = 0)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"{name}: DestroySelf must run on the server.", this);
            return;
        }
        if (!IsSpawned) return;
        NetworkObject.Despawn(true);
    }

    public bool TryGetTableware(out TablewareKitchenObject tablewareKitchenObject)
    {
        if (this is TablewareKitchenObject)
        {
            tablewareKitchenObject = this as TablewareKitchenObject;
            return true;
        }
        else
        {
            tablewareKitchenObject = null;
            return false;
        }
    }

    public static KitchenObject SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent)
    {
        return KitchenGameManager.Instance.SpawnKitchenObject(kitchenObjectSO, kitchenObjectParent);
    }
}
