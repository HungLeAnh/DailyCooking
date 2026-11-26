using NUnit.Framework;

public class GraphicsPreferencesTests
{
    private static GraphicsPreferences P(GraphicsMode m, int fps = 0) => new GraphicsPreferences { Mode = m, ChosenFps = fps };

    [Test] public void Default_IsAutoUnchosenFps()
    {
        Assert.AreEqual(GraphicsMode.Auto, GraphicsPreferences.Default.Mode);
        Assert.AreEqual(0, GraphicsPreferences.Default.ChosenFps);
    }

    [Test] public void Load_InvalidValues_FallBack()
    {
        var bad = GraphicsPreferencesRules.Sanitize(7, 45);
        Assert.AreEqual(GraphicsMode.Auto, bad.Mode);
        Assert.AreEqual(0, bad.ChosenFps);
        var ok = GraphicsPreferencesRules.Sanitize(2, 30);
        Assert.AreEqual(GraphicsMode.Medium, ok.Mode);
        Assert.AreEqual(30, ok.ChosenFps);
        Assert.AreEqual(GraphicsMode.Auto, GraphicsPreferencesRules.Sanitize(-1, 60).Mode);
    }

    [Test] public void Auto_GovernorRuns() => Assert.IsTrue(GraphicsPreferencesRules.GovernorRuns(P(GraphicsMode.Auto), true, true));

    [Test] public void Manual_GovernorOff()
    {
        Assert.IsFalse(GraphicsPreferencesRules.GovernorRuns(P(GraphicsMode.Low), true, true));
        Assert.IsFalse(GraphicsPreferencesRules.GovernorRuns(P(GraphicsMode.High), true, true));
    }

    [Test] public void KillSwitch_GovernorOff() => Assert.IsFalse(GraphicsPreferencesRules.GovernorRuns(P(GraphicsMode.Auto), false, true));

    [Test] public void NotAutoAllowed_GovernorOff() => Assert.IsFalse(GraphicsPreferencesRules.GovernorRuns(P(GraphicsMode.Auto), true, false));

    [Test] public void ChosenFps_IsLocked()
    {
        Assert.IsTrue(GraphicsPreferencesRules.FpsLocked(P(GraphicsMode.Auto, 60)));
        Assert.IsTrue(GraphicsPreferencesRules.FpsLocked(P(GraphicsMode.Auto, 30)));
        Assert.IsFalse(GraphicsPreferencesRules.FpsLocked(P(GraphicsMode.Auto, 0)));
    }

    [Test] public void ManualTier_Maps()
    {
        Assert.IsNull(GraphicsPreferencesRules.ManualTier(P(GraphicsMode.Auto)));
        Assert.AreEqual(MobileTier.Low, GraphicsPreferencesRules.ManualTier(P(GraphicsMode.Low)));
        Assert.AreEqual(MobileTier.Medium, GraphicsPreferencesRules.ManualTier(P(GraphicsMode.Medium)));
        Assert.AreEqual(MobileTier.High, GraphicsPreferencesRules.ManualTier(P(GraphicsMode.High)));
    }

    [Test] public void StartFps_ChosenWins()
    {
        Assert.AreEqual(30, GraphicsPreferencesRules.StartFps(P(GraphicsMode.Auto, 30), 60));
        Assert.AreEqual(60, GraphicsPreferencesRules.StartFps(P(GraphicsMode.Auto, 0), 60));
    }

    [Test] public void NextPrevious_Cycle()
    {
        Assert.AreEqual(GraphicsMode.Low, GraphicsPreferencesRules.Next(GraphicsMode.Auto));
        Assert.AreEqual(GraphicsMode.Auto, GraphicsPreferencesRules.Next(GraphicsMode.High));
        Assert.AreEqual(GraphicsMode.High, GraphicsPreferencesRules.Previous(GraphicsMode.Auto));
        Assert.AreEqual(GraphicsMode.Medium, GraphicsPreferencesRules.Previous(GraphicsMode.High));
    }

    [Test] public void ToggleFps()
    {
        Assert.AreEqual(30, GraphicsPreferencesRules.ToggleFps(60));
        Assert.AreEqual(60, GraphicsPreferencesRules.ToggleFps(30));
        Assert.AreEqual(30, GraphicsPreferencesRules.ToggleFps(0));
    }
}
