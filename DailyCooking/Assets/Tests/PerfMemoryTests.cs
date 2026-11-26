using NUnit.Framework;

public class PerfMemoryTests
{
    private static PerfMemoryState None => new PerfMemoryState { RememberedTier = -1, RememberedFps = 0, CleanSessions = 0 };

    private static PerfMemoryState Mem(MobileTier tier, int fps, int clean = 0) =>
        new PerfMemoryState { RememberedTier = (int)tier, RememberedFps = fps, CleanSessions = clean };

    private static PerfMemoryState CleanTimes(PerfMemoryState s, MobileTier pick, int n)
    {
        for (int i = 0; i < n; i++)
            s = PerfMemory.AfterSession(s, false, pick);
        return s;
    }

    [Test] public void NoMemory_StartsAtDevicePick60()
    {
        var c = PerfMemory.StartupChoice(MobileTier.High, None);
        Assert.AreEqual(MobileTier.High, c.tier);
        Assert.AreEqual(60, c.fps);
    }

    [Test] public void Start_NeverAboveDevicePick()
    {
        var c = PerfMemory.StartupChoice(MobileTier.Low, Mem(MobileTier.High, 60));
        Assert.AreEqual(MobileTier.Low, c.tier);
    }

    [Test] public void RememberedLower_StartsThere()
    {
        var c = PerfMemory.StartupChoice(MobileTier.High, Mem(MobileTier.Low, 30));
        Assert.AreEqual(MobileTier.Low, c.tier);
        Assert.AreEqual(30, c.fps);
    }

    [Test] public void AfterDowngrade_RemembersAndResets()
    {
        var s = PerfMemory.AfterDowngrade(Mem(MobileTier.High, 60, 2), MobileTier.Medium, 60);
        Assert.AreEqual((int)MobileTier.Medium, s.RememberedTier);
        Assert.AreEqual(60, s.RememberedFps);
        Assert.AreEqual(0, s.CleanSessions);
    }

    [Test] public void Downgrade_ResetsCleanCount()
    {
        var s = PerfMemory.AfterSession(Mem(MobileTier.Low, 60, 2), true, MobileTier.High);
        Assert.AreEqual(0, s.CleanSessions);
        Assert.AreEqual((int)MobileTier.Low, s.RememberedTier);
    }

    [Test] public void ThreeCleanSessions_Raise30To60First()
    {
        var s = CleanTimes(Mem(MobileTier.Low, 30), MobileTier.High, PerfMemory.CleanSessionsToRetry);
        Assert.AreEqual((int)MobileTier.Low, s.RememberedTier);
        Assert.AreEqual(60, s.RememberedFps);
        Assert.AreEqual(0, s.CleanSessions);
    }

    [Test] public void Raise_ThenTierUp()
    {
        var s = CleanTimes(Mem(MobileTier.Low, 60), MobileTier.High, 3);
        Assert.AreEqual((int)MobileTier.Medium, s.RememberedTier);
        Assert.AreEqual(60, s.RememberedFps);
    }

    [Test] public void Raise_StopsAtDevicePickAnd60()
    {
        var s = CleanTimes(Mem(MobileTier.Medium, 60), MobileTier.Medium, 3);
        Assert.AreEqual(-1, s.RememberedTier);
        Assert.AreEqual(0, s.RememberedFps);
        Assert.AreEqual(0, s.CleanSessions);
    }

    [Test] public void TwoClean_NoRaise()
    {
        var s = CleanTimes(Mem(MobileTier.Low, 30), MobileTier.High, 2);
        Assert.AreEqual((int)MobileTier.Low, s.RememberedTier);
        Assert.AreEqual(30, s.RememberedFps);
        Assert.AreEqual(2, s.CleanSessions);
    }

    [Test] public void NoMemory_CleanSessionsStayNone()
    {
        var s = CleanTimes(None, MobileTier.High, 5);
        Assert.AreEqual(-1, s.RememberedTier);
        Assert.AreEqual(0, s.CleanSessions);
    }
}
