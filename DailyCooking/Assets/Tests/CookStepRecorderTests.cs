using NUnit.Framework;

public class CookStepRecorderTests
{
    private CookStepRecorder recorder;

    [SetUp] public void SetUp() => recorder = new CookStepRecorder();

    [Test] public void NoReports_ScoresOneStar_Flip() => Assert.AreEqual(1, recorder.StepStars(GestureFamily.Flip, 0));
    [Test] public void NoReports_ScoresOneStar_Pour() => Assert.AreEqual(1, recorder.StepStars(GestureFamily.Pour, 0));
    [Test] public void NoReports_ScoresOneStar_Stir() => Assert.AreEqual(1, recorder.StepStars(GestureFamily.Stir, 3));
    [Test] public void NoReports_Cut_IsOne() => Assert.AreEqual(1, recorder.StepStars(GestureFamily.Cut, 0));

    [Test] public void Flip_InWindow_Three()
    {
        Assert.IsTrue(recorder.RecordFlip(0.55f, 0.5f));
        Assert.AreEqual(3, recorder.StepStars(GestureFamily.Flip, 0));
    }

    [Test] public void SecondFlip_Ignored()
    {
        recorder.RecordFlip(0.55f, 0.5f);
        Assert.IsFalse(recorder.RecordFlip(0.1f, 0.1f));
        Assert.AreEqual(3, recorder.StepStars(GestureFamily.Flip, 0));
    }

    [Test] public void FlipFarFromServer_Ignored()
    {
        Assert.IsFalse(recorder.RecordFlip(0.55f, 0.9f));
        Assert.AreEqual(1, recorder.StepStars(GestureFamily.Flip, 0));
    }

    [Test] public void StirSamePromptTwice_CountsOnce()
    {
        recorder.RecordStir(0, 1);
        Assert.IsFalse(recorder.RecordStir(0, 1));
        recorder.RecordStir(1, 1);
        Assert.AreEqual(2, recorder.StepStars(GestureFamily.Stir, 4));
    }

    [Test] public void StirFuturePrompt_Ignored()
    {
        Assert.IsFalse(recorder.RecordStir(3, 1));
        Assert.IsFalse(recorder.RecordStir(-1, 1));
    }

    [Test] public void Swipes_Average()
    {
        Assert.IsTrue(recorder.RecordSwipe(0.9f));
        Assert.IsTrue(recorder.RecordSwipe(0.1f));
        Assert.IsFalse(recorder.RecordSwipe(-1f));
        Assert.AreEqual(2, recorder.StepStars(GestureFamily.Cut, 0));
    }

    [Test] public void UndoLastSwipe_DropsOnlyTheLastOne()
    {
        recorder.RecordSwipe(0.9f);
        recorder.RecordSwipe(0.1f);
        recorder.UndoLastSwipe();
        Assert.AreEqual(3, recorder.StepStars(GestureFamily.Cut, 0));
        recorder.UndoLastSwipe();
        recorder.UndoLastSwipe(); // nothing left: no error
        Assert.AreEqual(1, recorder.StepStars(GestureFamily.Cut, 0));
    }

    [Test] public void Pour_InBand_Three()
    {
        Assert.IsTrue(recorder.RecordPour(0.9f, 0.85f));
        Assert.AreEqual(3, recorder.StepStars(GestureFamily.Pour, 0));
    }

    [Test] public void SecondPour_Ignored()
    {
        recorder.RecordPour(0.9f, 0.85f);
        Assert.IsFalse(recorder.RecordPour(0.5f, 0.5f));
        Assert.AreEqual(3, recorder.StepStars(GestureFamily.Pour, 0));
    }

    [Test] public void Reset_ClearsEverything()
    {
        recorder.RecordFlip(0.55f, 0.5f);
        recorder.RecordStir(0, 0);
        recorder.RecordSwipe(0.9f);
        recorder.RecordPour(0.9f, 0.9f);
        recorder.Reset();
        Assert.AreEqual(1, recorder.StepStars(GestureFamily.Flip, 0));
        Assert.AreEqual(1, recorder.StepStars(GestureFamily.Stir, 2));
        Assert.AreEqual(1, recorder.StepStars(GestureFamily.Cut, 0));
        Assert.AreEqual(1, recorder.StepStars(GestureFamily.Pour, 0));
    }
}
