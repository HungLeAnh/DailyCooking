using System;
using UnityEngine;

[Serializable]
public class ContainerData : GridObjectData
{
    public KitchenObjectSO KitchenObjectSO { get; private set; }
    public float FillAmount { get; private set; } = 0f;

    public ContainerData(KitchenObjectSO kitchenObjectSO, float fillAmount, string placedObjectTypeSOGuid, Vector2Int origin, Dir dir, InventoryTabType type)
        : base(placedObjectTypeSOGuid, origin, dir, type)
    {
        KitchenObjectSO = kitchenObjectSO;
        FillAmount = fillAmount;
    }

    public void Fill(float amount)
    {
        FillAmount += amount;
    }
    public void Empty(float amount)
    {
        FillAmount -= amount;
        if (FillAmount < 0f)
        {
            FillAmount = 0f;
        }
    }
}