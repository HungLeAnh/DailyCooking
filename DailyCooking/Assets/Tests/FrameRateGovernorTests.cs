using System.Collections.Generic;
using NUnit.Framework;

public class FrameRateGovernorTests
{
    private const float Good = 1f / 60f;
    private const float Bad = 1f / 30f;

    // Feeds `seconds` of constant frame time and returns every non-None decision.
    private static List<GovernorDecision> Run(FrameRateGovernor governor, float seconds, float dt)
    {
        var decisions = new List<GovernorDecision>();
        int frames = (int)System.Math.Round(seconds / dt);
        for (int i = 0; i < frames; i++)
        {
            GovernorDecision d = governor.Feed(dt);
            if (d != GovernorDecision.None)
                decisions.Add(d);
        }
        return decisions;
    }

    [Test] public void Smooth60_NeverDrops()
    {
        var g = new FrameRateGovernor(MobileTier.High, 60, false);
        CollectionAssert.IsEmpty(Run(g, 20f, Good));
        Assert.AreEqual(MobileTier.High, g.Tier);
    }

    [Test] public void SingleBadWindow_NoDrop()
    {
        var g = new FrameRateGovernor(MobileTier.High, 60, false);
        var d = Run(g, 5f, Bad);
        d.AddRange(Run(g, 5f, Good));
        CollectionAssert.IsEmpty(d);
        Assert.AreEqual(MobileTier.High, g.Tier);
    }

    [Test] public void TwoBadWindows_DropOneTier()
    {
        var g = new FrameRateGovernor(MobileTier.High, 60, false);
        CollectionAssert.AreEqual(new[] { GovernorDecision.DropTier }, Run(g, 10f, Bad));
        Assert.AreEqual(MobileTier.Medium, g.Tier);
    }

    [Test] public void SkipsWindowAfterDrop()
    {
        var g = new FrameRateGovernor(MobileTier.High, 60, false);
        Run(g, 10f, Bad);
        CollectionAssert.IsEmpty(Run(g, 5f, Bad));
        CollectionAssert.AreEqual(new[] { GovernorDecision.DropTier }, Run(g, 10f, Bad));
        Assert.AreEqual(MobileTier.Low, g.Tier);
    }

    [Test] public void AtLow_DropsTo30_ThenFinished()
    {
        var g = new FrameRateGovernor(MobileTier.Low, 60, false);
        CollectionAssert.AreEqual(new[] { GovernorDecision.DropTo30 }, Run(g, 10f, Bad));
        Assert.AreEqual(30, g.TargetFps);
        Assert.IsTrue(g.Finished);
        CollectionAssert.IsEmpty(Run(g, 20f, 1f / 15f));
    }

    [Test] public void FpsLocked_NeverDropsTo30()
    {
        var g = new FrameRateGovernor(MobileTier.Low, 60, true);
        CollectionAssert.IsEmpty(Run(g, 20f, Bad));
        Assert.AreEqual(60, g.TargetFps);
        Assert.IsTrue(g.Finished);
    }

    [Test] public void ResetWindow_DiscardsPartialWindow()
    {
        var g = new FrameRateGovernor(MobileTier.High, 60, false);
        var d = new List<GovernorDecision>();
        for (int i = 0; i < 5; i++)
        {
            d.AddRange(Run(g, 4f, Bad));
            g.ResetWindow();
        }
        CollectionAssert.IsEmpty(d);
        Assert.AreEqual(MobileTier.High, g.Tier);
    }

    [Test] public void Tolerance_JustAboveTarget_CountsOnTarget()
    {
        var g = new FrameRateGovernor(MobileTier.High, 60, false);
        CollectionAssert.IsEmpty(Run(g, 10f, Good * 1.1f));
    }

    [Test] public void LongFrame_AfterResume_Ignored()
    {
        // Coming back from background / an ad: one frame carries the paused time.
        var g = new FrameRateGovernor(MobileTier.High, 60, false);
        Assert.AreEqual(GovernorDecision.None, g.Feed(30f));
        CollectionAssert.IsEmpty(Run(g, 5f, Bad));
        Assert.AreEqual(MobileTier.High, g.Tier);
    }

    [Test] public void At30Target_JudgesAgainst30()
    {
        // A session restored at 30 FPS on Low is judged against 30, not 60.
        var g = new FrameRateGovernor(MobileTier.Low, 30, false);
        CollectionAssert.IsEmpty(Run(g, 20f, Bad));
        Assert.IsTrue(g.Finished);
    }
}
