using NUnit.Framework;

public class GestureScoringTests
{
    [Test] public void Cut_Thresholds()
    {
        Assert.AreEqual(3, GestureScoring.CutStars(0.9f));
        Assert.AreEqual(2, GestureScoring.CutStars(0.5f));
        Assert.AreEqual(1, GestureScoring.CutStars(0.1f));
    }

    [Test] public void Flip_WindowMarginAndMissed()
    {
        Assert.AreEqual(3, GestureScoring.FlipStars(0.55f));
        Assert.AreEqual(2, GestureScoring.FlipStars(0.35f));
        Assert.AreEqual(2, GestureScoring.FlipStars(0.75f));
        Assert.AreEqual(1, GestureScoring.FlipStars(0.1f));
        Assert.AreEqual(1, GestureScoring.FlipStars(-1f));
    }

    [Test] public void Stir_AnsweredShare()
    {
        Assert.AreEqual(3, GestureScoring.StirStars(4, 4));
        Assert.AreEqual(2, GestureScoring.StirStars(2, 4));
        Assert.AreEqual(1, GestureScoring.StirStars(1, 4));
        Assert.AreEqual(3, GestureScoring.StirStars(0, 0));
    }

    [Test] public void Pour_BandUnderOverAndMissed()
    {
        Assert.AreEqual(3, GestureScoring.PourStars(0.9f));
        Assert.AreEqual(2, GestureScoring.PourStars(0.5f));
        Assert.AreEqual(1, GestureScoring.PourStars(1.2f));
        Assert.AreEqual(1, GestureScoring.PourStars(-1f));
    }

    [Test] public void Watch_ZoneElseTwo()
    {
        Assert.AreEqual(3, GestureScoring.WatchStars(0.3f));
        Assert.AreEqual(2, GestureScoring.WatchStars(0.05f));
        Assert.AreEqual(2, GestureScoring.WatchStars(0.7f));
    }
}
