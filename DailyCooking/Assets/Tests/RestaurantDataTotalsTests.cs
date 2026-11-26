using System.Collections.Generic;
using NUnit.Framework;

public class RestaurantDataTotalsTests
{
    [Test] public void SetCoins_SetsAndNotifies()
    {
        var d = new RestaurantData();
        int notified = 0;
        d.OnResourceChange += () => notified++;
        d.SetCoins(1234);
        Assert.AreEqual(1234, d.Coins);
        Assert.AreEqual(1, notified);
        d.SetCoins(1234);
        Assert.AreEqual(1234, d.Coins);
    }

    [Test] public void SetGems_SetsAndNotifies()
    {
        var d = new RestaurantData();
        int notified = 0;
        d.OnResourceChange += () => notified++;
        d.SetGems(7);
        Assert.AreEqual(7, d.Gems);
        Assert.AreEqual(1, notified);
    }

    [Test] public void SetProgress_SameLevel_NoLevelUp()
    {
        var d = new RestaurantData();
        var ups = new List<int>();
        d.OnLevelUp += l => ups.Add(l);
        int expChanged = 0;
        d.OnExpChange += () => expChanged++;
        d.SetProgress(1, 40);
        Assert.AreEqual(1, d.Level);
        Assert.AreEqual(40, d.Exp);
        CollectionAssert.IsEmpty(ups);
        Assert.AreEqual(1, expChanged);
    }

    [Test] public void SetProgress_OneLevel_FiresLevelUpOnce()
    {
        var d = new RestaurantData();
        var ups = new List<int>();
        d.OnLevelUp += l => ups.Add(l);
        int levelChanged = 0;
        d.OnLevelChange += () => levelChanged++;
        d.SetProgress(2, 10);
        CollectionAssert.AreEqual(new[] { 2 }, ups);
        Assert.AreEqual(1, levelChanged);
        Assert.AreEqual(2, d.Level);
        Assert.AreEqual(10, d.Exp);
    }

    [Test] public void SetProgress_TwoLevels_FiresLevelUpTwice()
    {
        var d = new RestaurantData();
        var ups = new List<int>();
        d.OnLevelUp += l => ups.Add(l);
        d.SetProgress(3, 0);
        CollectionAssert.AreEqual(new[] { 2, 3 }, ups);
        Assert.AreEqual(3, d.Level);
    }

    [Test] public void SetProgress_Lower_NoLevelUp()
    {
        var d = new RestaurantData { Level = 5 };
        var ups = new List<int>();
        d.OnLevelUp += l => ups.Add(l);
        d.SetProgress(4, 0);
        CollectionAssert.IsEmpty(ups);
        Assert.AreEqual(4, d.Level);
    }
}
