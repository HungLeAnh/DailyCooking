using System;

[Serializable]

public class RewardData
{
    public enum RewardType
    {
        Coin,
        Gem,
    }

    public string id;
    public int amount;

    public RewardData(string id, int amount)
    {
        this.id = id;
        this.amount = amount;
    }
}
