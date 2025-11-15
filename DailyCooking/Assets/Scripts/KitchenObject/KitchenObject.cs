using System.Collections.Generic;
using System;
using UnityEngine;

public class KitchenObject : MonoBehaviour
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    private IKitchenObjectParent kitchenObjectParent;

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

        transform.parent = kitchenObjectParent.GetKitchenObjectFollowTransform(index);
        transform.localPosition = Vector3.zero;
    }
    public IKitchenObjectParent GetKitchenObjectParent()
    {
        return kitchenObjectParent;
    }
    public void DestroySelf()
    {
        kitchenObjectParent.ClearKitchenObject();
        Destroy(gameObject);
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
        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);
        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();
        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
        return kitchenObject;
    }
    public static KitchenObject SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent, int index)
    {
        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);
        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();
        kitchenObject.SetKitchenObjectParent(kitchenObjectParent,index);
        return kitchenObject;
    }
}
