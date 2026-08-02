using System;
using UnityEngine;

[Serializable]
public struct ShopReward
{
    public string Guid;
    public int Amount;

    public ShopReward(string guid, int amount)
    {
        Guid = guid;
        Amount = amount;
    }
}