using System;
using UnityEngine;

[Serializable]
public class ItemStack
{
    private InventoryItemSO item;
    private int amount;

    public InventoryItemSO Item => item;
    public int Amount
    {
        get => amount;
        set => amount = value;
    }
    public ItemStack()
    {
        item = null;
        amount = 0;
    }
    public ItemStack(ItemStack itemStack)
    {
        item = itemStack.Item;
        amount = itemStack.Amount;
    }
    public ItemStack(InventoryItemSO item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }
}
