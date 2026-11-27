using NUnit.Framework;

public class AdCandidatesTests
{
    [Test] public void CanWatch()
    {
        Assert.IsTrue(AdCandidates.CanWatch(hasAd: true, isAway: false));
        Assert.IsFalse(AdCandidates.CanWatch(hasAd: true, isAway: true));
        Assert.IsFalse(AdCandidates.CanWatch(hasAd: false, isAway: false));
        Assert.IsFalse(AdCandidates.CanWatch(hasAd: false, isAway: true));
    }
}
