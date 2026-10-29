using NUnit.Framework;

public class CookingClockTests
{
    [Test] public void Tick_ServerCooking_AdvancesCookClock()
    {
        var clock = new CookingClock();
        clock.Reset(0f, 0f);

        clock.Tick(CookingTool.State.Cooking, 0.5f, true, 0f, 0f);

        Assert.AreEqual(0.5f, clock.LocalCook, 0.001f);
        Assert.AreEqual(0f, clock.LocalBurn, 0.001f);
    }

    [Test] public void Tick_ClientCooking_SnapsToServerWhenDriftIsLarge()
    {
        var clock = new CookingClock();
        clock.Reset(0f, 0f);

        clock.Tick(CookingTool.State.Cooking, 0.2f, false, 5f, 0f);

        Assert.AreEqual(5f, clock.LocalCook, 0.001f);
    }

    [Test] public void Tick_ClientCooking_KeepsSmallDrift()
    {
        var clock = new CookingClock();
        clock.Reset(2f, 0f);

        clock.Tick(CookingTool.State.Cooking, 0.1f, false, 2.2f, 0f);

        Assert.AreEqual(2.1f, clock.LocalCook, 0.001f);
    }

    [Test] public void Tick_Cooked_AdvancesBurnClock()
    {
        var clock = new CookingClock();
        clock.Reset(3f, 0.25f);

        clock.Tick(CookingTool.State.Cooked, 0.5f, true, 3f, 0.25f);

        Assert.AreEqual(3f, clock.LocalCook, 0.001f);
        Assert.AreEqual(0.75f, clock.LocalBurn, 0.001f);
    }

    [Test] public void Tick_Idle_DoesNotAdvanceEitherClock()
    {
        var clock = new CookingClock();
        clock.Reset(1f, 2f);

        clock.Tick(CookingTool.State.Idle, 1f, true, 1f, 2f);

        Assert.AreEqual(1f, clock.LocalCook, 0.001f);
        Assert.AreEqual(2f, clock.LocalBurn, 0.001f);
    }

    [Test] public void SyncAccumulator_ResetsWhenMarked()
    {
        var clock = new CookingClock();

        clock.AddSync(CookingClock.NetSyncInterval);
        Assert.IsTrue(clock.ShouldSync());

        clock.MarkSynced();
        Assert.IsFalse(clock.ShouldSync());
    }
}
