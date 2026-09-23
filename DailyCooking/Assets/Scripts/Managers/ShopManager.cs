using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CurrencyExchangeOffer
{
    public ShopItemType RewardType = ShopItemType.Coin;
    public int GemCost;
    public int RewardAmount;
}

// Client-side front for shop actions: checks the balance for instant feedback, then sends an
// intent to the server, which looks the price up again and applies the purchase
// (GameManager.Economy).
public class ShopManager : PersistentSingleton<ShopManager>
{
    [Header("Free Currency")]
    [SerializeField] private List<DailyFreeCurrency> dailyFreeCurrency = new List<DailyFreeCurrency>();

    [Header("Currency Exchange")]
    [Tooltip("Gem-to-currency offers the server accepts. Must match the UICurrencyItem entries in the shop.")]
    [SerializeField] private List<CurrencyExchangeOffer> currencyExchangeOffers = new List<CurrencyExchangeOffer>
    {
        new CurrencyExchangeOffer { RewardType = ShopItemType.Coin, GemCost = 30, RewardAmount = 600 },
        new CurrencyExchangeOffer { RewardType = ShopItemType.Coin, GemCost = 100, RewardAmount = 2400 },
        new CurrencyExchangeOffer { RewardType = ShopItemType.Coin, GemCost = 200, RewardAmount = 5000 },
    };

    public List<DailyFreeCurrency> DailyFreeCurrency { get => dailyFreeCurrency; set => dailyFreeCurrency = value; }
    public List<CurrencyExchangeOffer> CurrencyExchangeOffers => currencyExchangeOffers;

    public void OnPurchase(ConfigShopItem item, List<ShopReward> rewards)
    {
        if(item.Price > GameManager.Instance.GameData.RestaurantData.Coins)
        {
            ShowNotEnough("Not enough money to buy this item.");
            return;
        }
        GameManager.Instance.BuyShopItemServerRpc(item.Id);
    }

    public void ExchangeCurrency(ShopItemType rewardType, int gemCost, int rewardAmount)
    {
        if (GameManager.Instance.GameData.RestaurantData.Gems < gemCost)
        {
            ShowNotEnough("Not enough gems to buy this currency.");
            return;
        }
        GameManager.Instance.ExchangeCurrencyServerRpc(rewardType, gemCost, rewardAmount);
    }

    public CurrencyExchangeOffer FindExchangeOffer(ShopItemType rewardType, int gemCost, int rewardAmount)
    {
        return currencyExchangeOffers.Find(offer =>
            offer.RewardType == rewardType && offer.GemCost == gemCost && offer.RewardAmount == rewardAmount);
    }

    private static void ShowNotEnough(string message)
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameNotiPopup,
            new UIGameNotiPopup.Param { Title = "warning", Message = message });
    }
}
