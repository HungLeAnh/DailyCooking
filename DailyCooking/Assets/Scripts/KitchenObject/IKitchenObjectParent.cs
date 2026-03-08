using Unity.Netcode;
using UnityEngine;

public interface IKitchenObjectParent
{
    public Transform GetKitchenObjectFollowTransform(int index = 0);


    public void SetKitchenObject(KitchenObject kitchenObject,int index = 0);

    public KitchenObject GetKitchenObject(int index = 0);

    public void ClearKitchenObject(int index = 0);

    public bool HasKitchenObject(int index = 0);
    public NetworkObject GetNetworkObject();

}
