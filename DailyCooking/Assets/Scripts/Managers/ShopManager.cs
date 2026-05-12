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
        GameManager.Instance.UpdateRestaurantCoinServerRpc(-item.Price);
        if(item.Type == ShopItemType.Item)
        {
            foreach (var pair in parsedData)
            {
                string itemId = pair.Key;
                int amount = pair.Value;
                for (int i = 0; i < amount; i++)
                {
                    GameManager.Instance.AddInventoryDataServerRpc(itemId);
                }
            }

        }
        if (item.Type == ShopItemType.Ingredient)
        {
            //Debug.Log(parsedData.Count);
            foreach (var pair in parsedData)
            {
                string recipeId = pair.Key;
                int amount = pair.Value;
                //Debug.Log($"Adding {amount} of {recipeId} to the post box.");
                var kitchenObjectSO = KitchenGameManager.Instance.GetKitchenObjectSOByGuid(recipeId);
                GridBuildingSystem.Instance.PostBox.AddPackage(kitchenObjectSO);
                //Debug.Log($"Added {amount} of {kitchenObjectSO.objectName} to the post box.");
                //for (int i = 0; i < amount; i++)
                //{

                //}
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