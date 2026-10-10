using NUnit.Framework;

public class RestaurantDataTests
{
    [Test] public void UpdateCoins_AddsCorrectly()
    {
        var data = new RestaurantData();
        int start = data.Coins;
        data.UpdateRestaurantCoins(100);
        Assert.AreEqual(start + 100, data.Coins);
    }
    [Test] public void UpdateGems_AddsCorrectly()
    {
        var data = new RestaurantData();
        data.UpdateRestaurantGems(5);
        Assert.AreEqual(5, data.Gems);
    }
    [Test] public void UpdateExp_LevelsUp_WhenThresholdReached()
    {
        var data = new RestaurantData();
        data.Level = 1;
        data.Exp = 0;
        bool levelChanged = false;
        int newLevel = -1;
        data.OnLevelUp += lvl => { levelChanged = true; newLevel = lvl; };
        data.UpdateRestaurantExp(100);
        Assert.IsTrue(levelChanged);
        Assert.AreEqual(2, newLevel);
        Assert.AreEqual(2, data.Level);
        Assert.AreEqual(0, data.Exp);
    }
    [Test] public void UpdateExp_NotLevelUp_BelowThreshold()
    {
        var data = new RestaurantData();
        data.Level = 2;
        data.UpdateRestaurantExp(50);
        Assert.AreEqual(2, data.Level);
        Assert.AreEqual(50, data.Exp);
    }
    [Test] public void UpdateRestaurantName_SetsName()
    {
        var data = new RestaurantData();
        data.UpdateRestaurantName("MyTest");
        Assert.AreEqual("MyTest", data.RestaurantName);
    }
}
