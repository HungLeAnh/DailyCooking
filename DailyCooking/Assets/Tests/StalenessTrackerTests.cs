using NUnit.Framework;

public class StalenessTrackerTests
{
    [Test] public void Staleness_After5s_Stale()
    {
        var t = new StalenessTracker();
        t.Reset(100f);
        Assert.IsFalse(t.IsStale(104.9f));
        Assert.IsTrue(t.IsStale(105.1f));
    }

    [Test] public void Staleness_HeardResets()
    {
        var t = new StalenessTracker();
        t.Reset(0f);
        t.Heard(4f);
        Assert.IsFalse(t.IsStale(8f));
        Assert.IsTrue(t.IsStale(9.5f));
    }

    [Test] public void NoBaseline_NotStale()
    {
        Assert.IsFalse(new StalenessTracker().IsStale(1000f));
    }
}
