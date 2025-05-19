using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryData
{
    private List<ItemStack> _items = new List<ItemStack>();
    public List<ItemStack> Items => _items;

    public void Init()
    {
        if (_items == null)
        {
            _items = new List<ItemStack>();
        }
        _items.Clear();
    }

    public void Add(InventoryItemData item, int count = 1)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < _items.Count; i++)
        {
            ItemStack currentItemStack = _items[i];
            if (item.PlacedObjectTypeSOGuid == currentItemStack.Item.PlacedObjectTypeSOGuid)
            {
                currentItemStack.Amount += count;
                return;
            }
        }

        _items.Add(new ItemStack(item, count));
    }
    public void Add(string id, int count = 1)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < _items.Count; i++)
        {
            ItemStack currentItemStack = _items[i];
            if (id == currentItemStack.Item.PlacedObjectTypeSOGuid)
            {
                currentItemStack.Amount += count;
                return;
            }
        }

        _items.Add(new ItemStack(InventoryItemData.CreateInventoryItem(id,true), count));
    }

    public void Remove(InventoryItemData item, int count = 1)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < _items.Count; i++)
        {
            ItemStack currentItemStack = _items[i];

            if (currentItemStack.Item.PlacedObjectTypeSOGuid == item.PlacedObjectTypeSOGuid)
            {
                currentItemStack.Amount -= count;

                if (currentItemStack.Amount <= 0)
                    _items.Remove(currentItemStack);

                return;
            }
        }
    }    
    public void Remove(string id, int count = 1)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < _items.Count; i++)
        {
            ItemStack currentItemStack = _items[i];

            if (currentItemStack.Item.PlacedObjectTypeSOGuid == id)
            {
                currentItemStack.Amount -= count;

                if (currentItemStack.Amount <= 0)
                    _items.Remove(currentItemStack);

                return;
            }
        }
    }

    public bool Contains(InventoryItemData item)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (item.PlacedObjectTypeSOGuid == _items[i].Item.PlacedObjectTypeSOGuid)
            {
                return true;
            }
        }

        return false;
    }

    public int Count(InventoryItemData item)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            ItemStack currentItemStack = _items[i];
            if (item.PlacedObjectTypeSOGuid == currentItemStack.Item.PlacedObjectTypeSOGuid)
            {
                return currentItemStack.Amount;
            }
        }

        return 0;
    }

    public int GetNumberOfItems()
    {
        int numberOfItems = 0;

        if (Items != null)
            for (int i = 0; i < Items.Count; i++)
            {
                numberOfItems += Items[i].Amount;
            }
        return numberOfItems;

    }
    public void ResetInventory()
    {
        Init();

    }
}
