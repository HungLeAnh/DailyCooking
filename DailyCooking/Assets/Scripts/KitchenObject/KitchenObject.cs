using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Netcode;

public class KitchenObject : NetworkBehaviour
{
    [SerializeField] protected KitchenObjectSO kitchenObjectSO;
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
        //Debug.Log("SetKitchenObjectParentClientRpc called with networkObjectReference: " + networkObjectReference.NetworkObjectId);
        networkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponentInChildren<CookingTool>();
        if (kitchenObjectParent == null)
        {
            kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();
        }

        if (this.kitchenObjectParent != null)
        {
            this.kitchenObjectParent.ClearKitchenObject(index);
        }

        this.kitchenObjectParent = kitchenObjectParent;
        if (kitchenObjectParent.HasKitchenObject(index))
        {
            Debug.LogError(this.gameObject.name + " already has a parent that has a kitchen object!! Parent: " 
                + kitchenObjectParent.GetNetworkObject().name);
            //Debug.LogError("IKitchenObjectParent already has a KitchenObject!!");
        }
        kitchenObjectParent.SetKitchenObject(this, index);

        kitchenObjectFollowTransform.setTargetTransform(kitchenObjectParent.GetKitchenObjectFollowTransform(index));

    }
    public IKitchenObjectParent GetKitchenObjectParent()
    {
        return kitchenObjectParent;
    }

    public void ResetState()
    {
        kitchenObjectParent = null;
    }
    public void DestroySelf(int index = 0)
    {
        DestroySelfServerRpc(index);
    }
    [Rpc(SendTo.Server)]
    private void DestroySelfServerRpc(int index = 0)
    {
        ClearKitchenObjectOnParentClientRpc(index);
        var netObj = gameObject.GetComponent<NetworkObject>();
        netObj.Despawn();
        if (KitchenObjectPool.Instance != null && kitchenObjectSO != null)
        {
            KitchenObjectPool.Instance.ReturnKitchenObject(gameObject, kitchenObjectSO.Guid);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    public void ClearKitchenObjectOnParentClientRpc(int index = 0)
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
}