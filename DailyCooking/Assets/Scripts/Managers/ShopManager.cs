using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : PersistentSingleton<ShopManager>
{
    [Header("Free Currency")]
    [SerializeField] private List<DailyFreeCurrency> dailyFreeCurrency = new List<DailyFreeCurrency>();

    public List<DailyFreeCurrency> DailyFreeCurrency { get => dailyFreeCurrency; set => dailyFreeCurrency = value; }

    public void OnPurchase(ConfigShopItem item, List<ShopReward> rewards)
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
        GameManager.Instance.UpdateRestaurantCoinServerRpc(-item.Price);
        if(item.Type == ShopItemType.Item)
        {
            foreach (var reward in rewards)
            {
                for (int i = 0; i < reward.Amount; i++)
                {
                    GameManager.Instance.AddInventoryDataServerRpc(reward.Guid);
                }
            }

        }
        if (item.Type == ShopItemType.Ingredient)
        {
            foreach (var reward in rewards)
            {
                var kitchenObjectSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(reward.Guid);
                GameManager.Instance.UpdatePostBoxDataServerRpc(kitchenObjectSO.Guid);
            }
        }
    }

    public void BuyCurrency(ShopItemType type, int currencyAmount)
    {
        switch (type)
        {
            case ShopItemType.Coin:
                GameManager.Instance.UpdateRestaurantCoinServerRpc(currencyAmount);
                break;
            case ShopItemType.Gem:
                GameManager.Instance.UpdateRestaurantGemsServerRpc(currencyAmount);
                break;
            default:
                break;
        }
    }
}