using System.Collections.Generic;
using NUnit.Framework;

public class QualityRulesTests
{
    private static QualityRecord Stars(int stars) => QualityRecord.None.WithStep(stars);

    [Test] public void None_HasZeroStars()
    {
        Assert.AreEqual(0, QualityRecord.None.Stars);
    }

    [Test] public void WithStep_ClampsToOneToThree()
    {
        Assert.AreEqual(3, QualityRecord.None.WithStep(5).Stars);
        Assert.AreEqual(1, QualityRecord.None.WithStep(0).Stars);
    }

    [Test] public void ChainedSteps_Average()
    {
        Assert.AreEqual(2, QualityRecord.None.WithStep(3).WithStep(1).Stars);
    }

    [Test] public void HalfRoundsUp()
    {
        Assert.AreEqual(3, QualityRecord.None.WithStep(3).WithStep(2).Stars);
    }

    [Test] public void Plus_AddsSumsAndCounts()
    {
        QualityRecord sum = Stars(3).Plus(Stars(1));
        Assert.AreEqual(4, sum.Sum);
        Assert.AreEqual(2, sum.Count);
    }

    [Test] public void BurnedChain_AddsOneStar()
    {
        // A 3-star cooked item that then burns gets one extra 1-star step.
        Assert.AreEqual(2, Stars(3).WithStep(1).Stars);
    }

    [Test] public void DishStars_IgnoresRaw()
    {
        Assert.AreEqual(3, QualityRules.DishStars(new List<QualityRecord> { QualityRecord.None, Stars(3), Stars(3) }));
    }

    [Test] public void DishStars_AllRaw_IsZero()
    {
        Assert.AreEqual(0, QualityRules.DishStars(new List<QualityRecord> { QualityRecord.None, QualityRecord.None }));
    }

    [Test] public void DishStars_Mixed()
    {
        Assert.AreEqual(2, QualityRules.DishStars(new List<QualityRecord> { Stars(1), Stars(3), Stars(2) }));
    }

    [Test] public void TipPercent_Values()
    {
        Assert.AreEqual(0f, QualityRules.TipPercent(0), 1e-6f);
        Assert.AreEqual(0f, QualityRules.TipPercent(1), 1e-6f);
        Assert.AreEqual(0.10f, QualityRules.TipPercent(2), 1e-6f);
        Assert.AreEqual(0.25f, QualityRules.TipPercent(3), 1e-6f);
    }

    [Test] public void Payment_AddsWaitAndQualityTip()
    {
        Assert.AreEqual(145, QualityRules.Payment(100, 0.2f, 3));
        Assert.AreEqual(120, QualityRules.Payment(100, 0.2f, 0));
    }
}
