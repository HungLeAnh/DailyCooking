using System.Collections.Generic;

// Server-side record of the gesture reports for one cook on one tool. Rejects duplicates and reports
// that drift too far from the server's own timer, then turns what it kept into this step's stars.
public sealed class CookStepRecorder
{
    public const float MaxReportDrift = 0.2f;
    private const float NotReported = -1f;

    private readonly List<int> swipeStars = new List<int>();
    private readonly HashSet<int> stirPrompts = new HashSet<int>();
    private float flipFraction = NotReported;
    private float pourLevel = NotReported;

    public void Reset()
    {
        swipeStars.Clear();
        stirPrompts.Clear();
        flipFraction = NotReported;
        pourLevel = NotReported;
    }

    public bool RecordSwipe(float centredness)
    {
        if (centredness < 0f)
            return false;
        swipeStars.Add(GestureScoring.CutStars(centredness));
        return true;
    }

    public bool RecordFlip(float clientFraction, float serverFraction)
    {
        if (flipFraction >= 0f || clientFraction < 0f || System.Math.Abs(clientFraction - serverFraction) > MaxReportDrift)
            return false;
        flipFraction = clientFraction;
        return true;
    }

    public bool RecordStir(int promptIndex, int currentPromptIndex)
    {
        if (promptIndex < 0 || promptIndex > currentPromptIndex)
            return false;
        return stirPrompts.Add(promptIndex);
    }

    public bool RecordPour(float level, float serverLevel)
    {
        if (pourLevel >= 0f || level < 0f || System.Math.Abs(level - serverLevel) > MaxReportDrift)
            return false;
        pourLevel = level;
        return true;
    }

    // Watch (scored at take-out) and None count as a plain 1-star step here.
    public int StepStars(GestureFamily family, int expectedStirPrompts)
    {
        switch (family)
        {
            case GestureFamily.Cut:
                if (swipeStars.Count == 0)
                    return 1;
                int sum = 0;
                foreach (int stars in swipeStars)
                    sum += stars;
                return QualityRules.RoundHalfUp((float)sum / swipeStars.Count);
            case GestureFamily.Flip:
                return GestureScoring.FlipStars(flipFraction);
            case GestureFamily.Stir:
                return GestureScoring.StirStars(stirPrompts.Count, expectedStirPrompts);
            case GestureFamily.Pour:
                return GestureScoring.PourStars(pourLevel);
            default:
                return 1;
        }
    }
}
