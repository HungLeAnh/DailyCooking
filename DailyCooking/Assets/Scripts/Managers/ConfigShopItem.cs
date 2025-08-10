using UnityEngine;

[System.Serializable]
public class ConfigShopItem
{
    [SerializeField] private int id;
    [SerializeField] private string name;
    [SerializeField] private ShopItemType type;
    [SerializeField] private int price;
    [SerializeField] private RewardData[] reward;
    [SerializeField] private ShopItemCategory category;
    [SerializeField] private int unlockLevel;

    public ShopItemCategory Category => category;
    public RewardData[] Reward => reward;
    public int Price => price;
    public ShopItemType Type => type;
    public string Name => name;
    public int Id => id;
    public int UnlockLevel => unlockLevel;

}

