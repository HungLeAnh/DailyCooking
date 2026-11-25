using NUnit.Framework;
using UnityEngine;

public class LookTouchTrackerTests
{
    [Test] public void TouchOverUI_IsNotAdopted()
    {
        var tracker = new LookTouchTracker();
        tracker.Began(0, new Vector2(100f, 100f), true);
        Assert.IsFalse(tracker.HasLookFinger);
        Assert.AreEqual(Vector2.zero, tracker.Moved(0, new Vector2(150f, 100f)));
    }

    [Test] public void JoystickFingerThenLookFinger_OnlyLookFingerTurns()
    {
        var tracker = new LookTouchTracker();
        tracker.Began(0, new Vector2(100f, 100f), true);
        tracker.Began(1, new Vector2(800f, 300f), false);
        Assert.AreEqual(Vector2.zero, tracker.Moved(0, new Vector2(150f, 100f)));
        Assert.AreEqual(new Vector2(20f, 10f), tracker.Moved(1, new Vector2(820f, 310f)));
    }

    [Test] public void SecondFreeTouch_IsIgnoredWhileLookFingerHeld()
    {
        var tracker = new LookTouchTracker();
        tracker.Began(1, new Vector2(800f, 300f), false);
        tracker.Began(2, new Vector2(600f, 300f), false);
        Assert.AreEqual(Vector2.zero, tracker.Moved(2, new Vector2(650f, 300f)));
    }

    [Test] public void Ended_ReleasesAndAllowsNewFinger()
    {
        var tracker = new LookTouchTracker();
        tracker.Began(1, new Vector2(800f, 300f), false);
        tracker.Ended(1);
        Assert.IsFalse(tracker.HasLookFinger);
        tracker.Began(2, new Vector2(600f, 300f), false);
        Assert.IsTrue(tracker.HasLookFinger);
        Assert.AreEqual(new Vector2(5f, 0f), tracker.Moved(2, new Vector2(605f, 300f)));
    }

    [Test] public void FirstMove_DeltaFromBeganPosition()
    {
        var tracker = new LookTouchTracker();
        tracker.Began(3, new Vector2(10f, 10f), false);
        Assert.AreEqual(new Vector2(4f, -2f), tracker.Moved(3, new Vector2(14f, 8f)));
        Assert.AreEqual(new Vector2(1f, 1f), tracker.Moved(3, new Vector2(15f, 9f)));
    }
}
