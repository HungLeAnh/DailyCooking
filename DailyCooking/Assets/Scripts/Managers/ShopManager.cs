using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : PersistentSingleton<ShopManager>
{
    public void OnPurchase(ConfigShopItem item)
    {
        if(item.Price > GameManager.Instance.GameData.PlayerStats.playerData.Coins)
        {
            Debug.Log("Not enough coins to buy this item.");
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameNotiPopup, 
                new UIGameNotiPopup.Param {Title = "warning", 
                                            Message = "Not enough money to buy this item."
                });
            return;
        }
        GameManager.Instance.GameData.PlayerStats.UpdatePlayerCoins(-item.Price);
        GameManager.Instance.GameData.AddInventoryData(InventoryItemData.CreateInventoryItem(item.Id.ToString()));
    }
}