using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

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
    [Test] public void Add_UnknownGuid_DoesNotThrowOrAdd()
    {
        var inv = new InventoryData();
        inv.Init();
        LogAssert.Expect(LogType.Error,
            "CreateInventoryItem: invalid inventory id or GridBuildingSystem is not ready: missing-guid");
        Assert.DoesNotThrow(() => inv.Add("missing-guid"));
        Assert.AreEqual(0, inv.Items.Count);
    }
}
