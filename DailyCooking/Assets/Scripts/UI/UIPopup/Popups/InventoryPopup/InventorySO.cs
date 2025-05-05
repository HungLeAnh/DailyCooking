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

    public void Add(InventoryItemSO item, int count = 1)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < _items.Count; i++)
        {
            ItemStack currentItemStack = _items[i];
            if (item == currentItemStack.Item)
            {
                currentItemStack.Amount += count;
                return;
            }
        }

        _items.Add(new ItemStack(item, count));
    }

    public void Remove(InventoryItemSO item, int count = 1)
    {
        if (count <= 0)
            return;

        for (int i = 0; i < _items.Count; i++)
        {
            ItemStack currentItemStack = _items[i];

            if (currentItemStack.Item == item)
            {
                currentItemStack.Amount -= count;

                if (currentItemStack.Amount <= 0)
                    _items.Remove(currentItemStack);

                return;
            }
        }
    }

    public bool Contains(InventoryItemSO item)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (item == _items[i].Item)
            {
                return true;
            }
        }

        return false;
    }

    public int Count(InventoryItemSO item)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            ItemStack currentItemStack = _items[i];
            if (item == currentItemStack.Item)
            {
                return currentItemStack.Amount;
            }
        }

        return 0;
    }

    public bool[] IngredientsAvailability(List<ItemStack> ingredients)
    {
        if (ingredients == null)
            return null;
        bool[] availabilityArray = new bool[ingredients.Count];

        for (int i = 0; i < ingredients.Count; i++)
        {
            availabilityArray[i] = _items.Exists(o => o.Item == ingredients[i].Item && o.Amount >= ingredients[i].Amount);

        }
        return availabilityArray;


    }
    public bool hasIngredients(List<ItemStack> ingredients)
    {

        bool hasIngredients = !ingredients.Exists(j => !_items.Exists(o => o.Item == j.Item && o.Amount >= j.Amount));

        return hasIngredients;


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
