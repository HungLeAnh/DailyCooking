using System;
using NUnit.Framework;

public class EconomyTests
{
    [Test] public void DailyOffer_ResetsOnFirstRefresh()
    {
        var shopData = new ShopData();
        shopData.UpdateDailyFreeItemCoinCount();

        Assert.IsTrue(shopData.RefreshDailyShopOffer(new DateTime(2026, 9, 23, 8, 0, 0, DateTimeKind.Utc)));
        Assert.AreEqual(0, shopData.DailyFreeItemCoinCount);
    }

    [Test] public void DailyOffer_DoesNotResetAgainTheSameDay()
    {
        var shopData = new ShopData();
        shopData.RefreshDailyShopOffer(new DateTime(2026, 9, 23, 8, 0, 0, DateTimeKind.Utc));
        shopData.UpdateDailyFreeItemGemCount();
        shopData.UpdateDailyFreeItemGemCount();

        Assert.IsFalse(shopData.RefreshDailyShopOffer(new DateTime(2026, 9, 23, 23, 59, 0, DateTimeKind.Utc)));
        Assert.AreEqual(2, shopData.DailyFreeItemGemCount);
    }

    [Test] public void DailyOffer_ResetsOnTheNextUtcDay()
    {
        var shopData = new ShopData();
        shopData.RefreshDailyShopOffer(new DateTime(2026, 9, 23, 23, 0, 0, DateTimeKind.Utc));
        shopData.UpdateDailyFreeItemCoinCount();

        Assert.IsTrue(shopData.RefreshDailyShopOffer(new DateTime(2026, 9, 24, 0, 1, 0, DateTimeKind.Utc)));
        Assert.AreEqual(0, shopData.DailyFreeItemCoinCount);
    }

    [Test] public void LevelUpReward_ScalesWithLevel()
    {
        Assert.AreEqual(200, EconomyRules.GetLevelUpRewardCoins(2));
        Assert.AreEqual(1000, EconomyRules.GetLevelUpRewardCoins(10));
    }

    [Test] public void ShopItemType_KeepsSerializedValues()
    {
        // Scenes, prefabs and config assets store these numbers.
        Assert.AreEqual(2, (int)ShopItemType.Ingredient);
        Assert.AreEqual(3, (int)ShopItemType.Gem);
        Assert.AreEqual(4, (int)ShopItemType.Coin);
    }
}
