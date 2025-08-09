using UnityEngine;

[System.Serializable]
public class ConfigShopItem
{
    [SerializeField] private int id;
    [SerializeField] private string name;
    [SerializeField] private ShopItemType type;
    [SerializeField] private int price;
    [SerializeField] private string reward;
    [SerializeField] private ShopItemCategory category;
    [SerializeField] private int unlockLevel;

    public ShopItemCategory Category { get => category; set => category = value; }
    public string Reward { get => reward; set => reward = value; }
    public int Price { get => price; set => price = value; }
    public ShopItemType Type { get => type; set => type = value; }
    public string Name { get => name; set => name = value; }
    public int Id { get => id; set => id = value; }
    public int UnlockLevel { get => unlockLevel; set => unlockLevel = value; }

}
