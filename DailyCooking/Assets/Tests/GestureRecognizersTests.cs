using NUnit.Framework;
using UnityEngine;

public class GestureRecognizersTests
{
    [Test] public void Swipe_ThroughCentre_IsOne()
    {
        Assert.AreEqual(1f, GestureRecognizers.SwipeCentredness(new Vector2(-50, 0), new Vector2(50, 0), Vector2.zero, 40f), 1e-4f);
    }

    [Test] public void Swipe_Offset_IsHalf()
    {
        Assert.AreEqual(0.5f, GestureRecognizers.SwipeCentredness(new Vector2(-50, 20), new Vector2(50, 20), Vector2.zero, 40f), 1e-4f);
    }

    [Test] public void Swipe_Miss_IsMinusOne()
    {
        Assert.AreEqual(-1f, GestureRecognizers.SwipeCentredness(new Vector2(-50, 60), new Vector2(50, 60), Vector2.zero, 40f), 1e-4f);
    }

    [Test] public void Swipe_TooShort_IsMinusOne()
    {
        Assert.AreEqual(-1f, GestureRecognizers.SwipeCentredness(new Vector2(-10, 0), new Vector2(10, 0), Vector2.zero, 40f), 1e-4f);
    }

    [Test] public void Stir_FullCircle_TriggersOnceAfter300Degrees()
    {
        var tracker = new GestureRecognizers.StirTracker();
        tracker.Reset(Vector2.zero);
        int triggers = 0;
        int firstTriggerAngle = -1;
        for (int angle = 0; angle <= 360; angle += 30)
        {
            float rad = angle * Mathf.Deg2Rad;
            if (tracker.Feed(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 50f))
            {
                triggers++;
                if (firstTriggerAngle < 0) firstTriggerAngle = angle;
            }
        }
        Assert.AreEqual(1, triggers);
        Assert.GreaterOrEqual(firstTriggerAngle, 300);
    }

    [Test] public void Stir_BackAndForth_NeverTriggers()
    {
        var tracker = new GestureRecognizers.StirTracker();
        tracker.Reset(Vector2.zero);
        for (int i = 0; i < 10; i++)
        {
            float rad = (i % 2 == 0 ? 30f : -30f) * Mathf.Deg2Rad;
            Assert.IsFalse(tracker.Feed(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * 50f));
        }
    }

    [Test] public void PourLevel_Values()
    {
        Assert.AreEqual(0.5f, GestureRecognizers.PourLevel(1f, 2f), 1e-5f);
        Assert.AreEqual(1.5f, GestureRecognizers.PourLevel(3f, 2f), 1e-5f);
        Assert.AreEqual(1f, GestureRecognizers.PourLevel(1f, 0f), 1e-5f);
    }
}
