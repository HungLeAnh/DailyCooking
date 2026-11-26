using NUnit.Framework;

public class FakeAdProviderTests
{
    [Test] public void Fake_CompletesAfterDelay()
    {
        var fake = new FakeAdProvider { DelaySeconds = 3f };
        AdOutcome? result = null;
        fake.ShowRewarded("p", o => result = o);
        fake.Tick(2.9f);
        Assert.IsNull(result);
        fake.Tick(0.2f);
        Assert.AreEqual(AdOutcome.Completed, result);
    }

    [Test] public void Fake_NoAd_ReportsNoAd()
    {
        var fake = new FakeAdProvider { HasAd = false };
        Assert.IsFalse(fake.HasRewarded);
        Assert.IsFalse(fake.HasInterstitial);
        AdOutcome? result = null;
        fake.ShowInterstitial("p", o => result = o);
        Assert.AreEqual(AdOutcome.NoAd, result);
    }

    [Test] public void Fake_ConfiguredFailure()
    {
        var fake = new FakeAdProvider { DelaySeconds = 1f, NextOutcome = AdOutcome.FailedOrClosedEarly };
        AdOutcome? result = null;
        fake.ShowRewarded("p", o => result = o);
        fake.Tick(1.5f);
        Assert.AreEqual(AdOutcome.FailedOrClosedEarly, result);
    }

    [Test] public void Fake_IsShowing_WhileAdRuns()
    {
        var fake = new FakeAdProvider { DelaySeconds = 2f };
        fake.ShowRewarded("p", o => { });
        Assert.IsTrue(fake.IsShowing);
        fake.Tick(3f);
        Assert.IsFalse(fake.IsShowing);
    }
}
