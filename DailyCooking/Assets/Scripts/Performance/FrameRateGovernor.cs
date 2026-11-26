public enum GovernorDecision
{
    None,
    DropTier,
    DropTo30,
}

// Watches frame times in 5 s windows and steps the graphics down when the phone can't keep up:
// one tier at a time, then 60 -> 30 FPS at Low (unless the player chose the FPS). Never raises.
// Pure logic: the caller feeds unscaled frame times and applies the decisions.
public sealed class FrameRateGovernor
{
    public const float WindowSeconds = 5f;
    public const float OnTargetRatio = 0.85f;
    public const float Tolerance = 1.15f;
    public const int FailedWindowsToAct = 2;
    // A frame this long is a resume from background/an ad or a load, not the phone's pace: skipped.
    public const float MaxFrameSeconds = 0.5f;

    // Absorbs float rounding so a window of exactly 5 s of frames closes on its last frame.
    private const double WindowEpsilon = 1e-4;

    private readonly bool fpsLocked;
    private double windowElapsed;
    private int windowFrames;
    private int windowOnTarget;
    private int failedStreak;
    private bool skipNextWindow;

    public MobileTier Tier { get; private set; }
    public int TargetFps { get; private set; }

    // True when there is nothing left to lower.
    public bool Finished => Tier == MobileTier.Low && (fpsLocked || TargetFps <= 30);

    public FrameRateGovernor(MobileTier startTier, int startFps, bool fpsLocked)
    {
        Tier = startTier;
        TargetFps = startFps > 0 ? startFps : 60;
        this.fpsLocked = fpsLocked;
    }

    public GovernorDecision Feed(float unscaledDt)
    {
        if (Finished || unscaledDt <= 0f || unscaledDt > MaxFrameSeconds)
            return GovernorDecision.None;

        windowElapsed += unscaledDt;
        windowFrames++;
        if (unscaledDt <= (1f / TargetFps) * Tolerance)
            windowOnTarget++;

        if (windowElapsed < WindowSeconds - WindowEpsilon)
            return GovernorDecision.None;

        bool failed = windowOnTarget < windowFrames * OnTargetRatio;
        ClearWindow();

        if (skipNextWindow)
        {
            skipNextWindow = false;
            return GovernorDecision.None;
        }

        failedStreak = failed ? failedStreak + 1 : 0;
        if (failedStreak < FailedWindowsToAct)
            return GovernorDecision.None;

        failedStreak = 0;
        skipNextWindow = true;
        if (Tier != MobileTier.Low)
        {
            Tier = Tier - 1;
            return GovernorDecision.DropTier;
        }
        TargetFps = 30;
        return GovernorDecision.DropTo30;
    }

    // Called after a scene load, pause or focus loss: those frames say nothing about the phone.
    public void ResetWindow()
    {
        ClearWindow();
        failedStreak = 0;
    }

    private void ClearWindow()
    {
        windowElapsed = 0;
        windowFrames = 0;
        windowOnTarget = 0;
    }
}
