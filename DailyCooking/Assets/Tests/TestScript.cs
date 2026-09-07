using NUnit.Framework;
using UnityEngine;

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

public class InventoryDataTests
{
    private InventoryItemData MakeItem(string guid) => new InventoryItemData(guid, InventoryTabType.Counter);

    [Test] public void Add_ByItem_IncreasesCount()
    {
        var inv = new InventoryData();
        inv.Init();
        var item = MakeItem("test-guid-123");
        inv.Add(item, 1);
        Assert.AreEqual(1, inv.Items.Count);
        Assert.AreEqual(1, inv.GetNumberOfItems());
        inv.Add(item, 2);
        Assert.AreEqual(1, inv.Items.Count);
        Assert.AreEqual(3, inv.Items[0].Amount);
    }
    [Test] public void Remove_ByItem_DecreasesAndRemoves()
    {
        var inv = new InventoryData();
        inv.Init();
        var item = MakeItem("test-guid-456");
        inv.Add(item, 2);
        inv.Remove(item, 1);
        Assert.AreEqual(1, inv.Items[0].Amount);
        inv.Remove(item, 1);
        Assert.AreEqual(0, inv.Items.Count);
    }
    [Test] public void Add_ZeroCount_Ignored()
    {
        var inv = new InventoryData();
        inv.Init();
        var item = MakeItem("any");
        inv.Add(item, 0);
        Assert.AreEqual(0, inv.Items.Count);
    }
    [Test] public void Contains_And_Count_Work()
    {
        var inv = new InventoryData();
        inv.Init();
        var item = MakeItem("guid-abc");
        Assert.IsFalse(inv.Contains(item));
        inv.Add(item, 5);
        Assert.IsTrue(inv.Contains(item));
        Assert.AreEqual(5, inv.Count(item));
    }
    [Test] public void GetNumberOfItems_SumsAmounts()
    {
        var inv = new InventoryData();
        inv.Init();
        inv.Add(MakeItem("a"), 2);
        inv.Add(MakeItem("b"), 3);
        Assert.AreEqual(5, inv.GetNumberOfItems());
    }
}

public class TutorialDataTests
{
    [Test] public void SetHasPlayed_FiresEvent()
    {
        var data = new TutorialData();
        bool fired = false;
        data.OnTutorialDataChanged += () => fired = true;
        data.SetHasPlayedFirstTime(true);
        Assert.IsTrue(fired);
        Assert.IsTrue(data.HasPlayedFirstTime);
    }
}

public class GameDefineTests
{
    [Test] public void GetDefaultGridJson_ReturnsValidJson()
    {
        string json = GameDefine.GetDefaultGridJson();
        Assert.IsFalse(string.IsNullOrEmpty(json));
        Assert.IsTrue(json.Contains("PlacedObjectTypeSOGuid"), "JSON should contain PlacedObjectTypeSOGuid");
        Assert.IsTrue(json.Length > 1000, "JSON should be substantial (>1000 chars)");
        // Basic structure check
        Assert.IsTrue(json.TrimStart().StartsWith("[[["), "Should start with [[[");
    }
    [Test] public void GetDefaultGridJson_FallbackNotEmpty()
    {
#pragma warning disable CS0618
        string legacy = GameDefine.GridArrayDataInit;
#pragma warning restore CS0618
        Assert.IsFalse(string.IsNullOrEmpty(legacy));
        string resolved = GameDefine.GetDefaultGridJson();
        Assert.IsFalse(string.IsNullOrEmpty(resolved));
    }
    [Test] public void DefaultGridConfigSO_Exists_InResources()
    {
        var so = Resources.Load<DefaultGridConfigSO>("DefaultGridConfig");
        Assert.IsNotNull(so, "DefaultGridConfigSO asset should exist at Resources/DefaultGridConfig");
        Assert.AreEqual(5, so.gridSize);
        Assert.IsFalse(string.IsNullOrEmpty(so.gridArrayJson));
    }
    [Test] public void GridSize_IsFive()
    {
        Assert.AreEqual(5, GameDefine.GridSize);
    }
    [Test] public void EmotionAndBotConstants_AreCorrect()
    {
        Assert.AreEqual(15f, GameDefine.EMOTION_DURATION);
        Assert.AreEqual(0.7f, GameDefine.TIP_PERCENTAGE);
    }
}
