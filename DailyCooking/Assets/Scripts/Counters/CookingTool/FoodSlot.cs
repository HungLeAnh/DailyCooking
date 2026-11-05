using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Food slots for a cooking tool. Plain C#: no Unity lifecycle, no networking.
// Slots keep their index for the lifetime of the item: a KitchenObject stores the index it
// was parented with, so compacting the list would leave the object's parent pointer stale.
public sealed class FoodSlot
{
    private readonly List<KitchenObject> _foods = new List<KitchenObject>();

    public int SlotCount => _foods.Count;

    public int FoodCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < _foods.Count; i++)
                if (_foods[i] != null) count++;
            return count;
        }
    }

    public bool HasFood => FoodCount > 0;

    // First item in slot order; single-slot tools only ever have this one.
    public KitchenObject Current
    {
        get
        {
            for (int i = 0; i < _foods.Count; i++)
                if (_foods[i] != null) return _foods[i];
            return null;
        }
    }

    public KitchenObject Get(int index)
    {
        return index >= 0 && index < _foods.Count ? _foods[index] : null;
    }

    public bool Contains(KitchenObject food)
    {
        return food != null && _foods.Contains(food);
    }

    public int FirstFreeIndex(int capacity)
    {
        for (int i = 0; i < capacity; i++)
            if (Get(i) == null) return i;
        return -1;
    }

    public void Set(KitchenObject food, int index = 0)
    {
        if (index < 0) index = 0;
        while (_foods.Count <= index)
            _foods.Add(null);
        _foods[index] = food;
    }

    public void ClearAt(int index)
    {
        if (index < 0 || index >= _foods.Count) return;
        _foods[index] = null;
        TrimTrailingEmptySlots();
    }

    public void Clear()
    {
        _foods.Clear();
    }

    // Non-null items in slot order. The caller owns the list.
    public List<KitchenObject> CopyFoods()
    {
        List<KitchenObject> foods = new List<KitchenObject>(FoodCount);
        for (int i = 0; i < _foods.Count; i++)
            if (_foods[i] != null) foods.Add(_foods[i]);
        return foods;
    }

    // Late joiners: the parent reference may arrive before the object itself spawns.
    public bool TryResolveFrom(NetworkObjectReference netFood, int index)
    {
        if (Get(index) != null) return true;
        if (netFood.TryGet(out NetworkObject networkObject) && networkObject != null)
        {
            KitchenObject food = networkObject.GetComponent<KitchenObject>();
            if (food != null)
            {
                Set(food, index);
                return true;
            }
        }
        return false;
    }

    private void TrimTrailingEmptySlots()
    {
        while (_foods.Count > 0 && _foods[_foods.Count - 1] == null)
            _foods.RemoveAt(_foods.Count - 1);
    }
}
