using System;
using UnityEngine;

[Serializable]
public class ItemStack
{
    private InventoryItemData item;
    private int amount;

    public InventoryItemData Item { get => item; set => item = value; }
    public int Amount { get => amount; set => amount = value;}
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
    public ItemStack(InventoryItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }

}
