using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : PersistentSingleton<ShopManager>
{
    [Header("Free Currency")]
    [SerializeField] private List<DailyFreeCurrency> dailyFreeCurrency = new List<DailyFreeCurrency>();

    public List<DailyFreeCurrency> DailyFreeCurrency { get => dailyFreeCurrency; set => dailyFreeCurrency = value; }

    public void OnPurchase(ConfigShopItem item, Dictionary<string, int> parsedData)
    {
        if(item.Price > GameManager.Instance.GameData.RestaurantData.Coins)
        {
            Debug.Log("Not enough coins to buy this item.");
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameNotiPopup, 
                new UIGameNotiPopup.Param {Title = "warning", 
                                            Message = "Not enough money to buy this item."
                });
            return;
        }
        GameManager.Instance.GameData.RestaurantData.UpdatePlayerCoins(-item.Price);
        if(item.Type == ShopItemType.Item)
        {
            foreach (var pair in parsedData)
            {
                string itemId = pair.Key;
                int amount = pair.Value;
                for (int i = 0; i < amount; i++)
                {
                    GameManager.Instance.GameData.
                        AddInventoryData(InventoryItemData.CreateInventoryItem(itemId,true));
                }
            }

        }
    }

    public void BuyCurrency(ShopItemType type, int currencyAmount)
    {
        switch (type)
        {
            case ShopItemType.Coin:
                GameManager.Instance.GameData.RestaurantData.UpdatePlayerCoins(currencyAmount);
                break;
            case ShopItemType.Gem:
                GameManager.Instance.GameData.RestaurantData.UpdatePlayerGems(currencyAmount);
                break;
            default:
                break;
        }
    }
}