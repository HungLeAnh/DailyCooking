using Unity.Netcode;
using UnityEngine;

// Single food slot for a cooking tool. Plain C#: no Unity lifecycle, no networking.
public sealed class FoodSlot
{
    private KitchenObject _food;

    public KitchenObject Current => _food;
    public KitchenObjectSO CurrentSO => _food != null ? _food.GetKitchenObjectSO() : null;
    public bool Has => _food != null;

    public void Set(KitchenObject food)
    {
        _food = food;
    }

    public void Clear()
    {
        _food = null;
    }

    public bool TryResolveFrom(NetworkObjectReference netFood)
    {
        if (_food != null)
            return true;
        if (netFood.TryGet(out NetworkObject networkObject) && networkObject != null)
        {
            KitchenObject food = networkObject.GetComponent<KitchenObject>();
            if (food != null)
            {
                _food = food;
                return true;
            }
        }
        return false;
    }
}
