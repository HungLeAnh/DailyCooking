using NUnit.Framework;

public class TutorialDataTests
{
    [Test] public void SetHasPlayed_FiresEvent()
    {
        var data = new TutorialData();
        bool fired = false;
        data.OnTutorialDataChanged += () => fired = true;
        data.SetHasPlayedFirstTime(true);
        Assert.IsTrue(fired);
        Assert.IsTrue(data.HasPlayedFirstTime);
    }
}
