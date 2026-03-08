using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Netcode;

public class KitchenObject : NetworkBehaviour
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    
    private IKitchenObjectParent kitchenObjectParent;
    private FollowTransform kitchenObjectFollowTransform;
    protected virtual void Awake()
    {
        kitchenObjectFollowTransform = GetComponent<FollowTransform>();
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
    public void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent, int index = 0)
    {
        SetKitchenObjectParentClientRpc(kitchenObjectParent.GetNetworkObject(),index);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetKitchenObjectParentClientRpc(NetworkObjectReference networkObjectReference, int index = 0)
    {
        networkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        if (this.kitchenObjectParent != null)
        {
            this.kitchenObjectParent.ClearKitchenObject(index);
        }

        this.kitchenObjectParent = kitchenObjectParent;
        if (kitchenObjectParent.HasKitchenObject(index))
        {
            Debug.LogError("IKitchenObjectParent already has a KitchenObject!!");
        }
        kitchenObjectParent.SetKitchenObject(this, index);

        kitchenObjectFollowTransform.setTargetTransform(kitchenObjectParent.GetKitchenObjectFollowTransform(index));

    }
    public IKitchenObjectParent GetKitchenObjectParent()
    {
        return kitchenObjectParent;
    }
    public void DestroySelf(int index = 0)
    {
        DestroySelfServerRpc(index);
    }
    [Rpc(SendTo.Server)]
    private void DestroySelfServerRpc(int index = 0)
    {
        ClearKitchenObjectOnParentClientRpc(index);
        gameObject.GetComponent<NetworkObject>().Despawn();
        Destroy(gameObject);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void ClearKitchenObjectOnParentClientRpc(int index = 0)
    {
        kitchenObjectParent.ClearKitchenObject(index);
    }
    public void ClearKitchenObjectOnParent(int index = 0)
    {
        kitchenObjectParent.ClearKitchenObject(index);

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

    public static void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent)
    {
        KitchenGameManager.Instance.SpawnKitchenObject(kitchenObjectSO, kitchenObjectParent);
    }    
    public static void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent, int index)
    {
        KitchenGameManager.Instance.SpawnKitchenObject(kitchenObjectSO, kitchenObjectParent,index);
    }
    public static void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        KitchenGameManager.Instance.DestroyKitchenObject(kitchenObject);
    }
}
