// The player's Settings choice. Auto = device pick plus automatic lowering; a manual level is fixed.
public enum GraphicsMode
{
    Auto = 0,
    Low = 1,
    Medium = 2,
    High = 3,
}

public struct GraphicsPreferences
{
    public GraphicsMode Mode;
    public int ChosenFps; // 0 = not chosen (managed); 30 or 60 = fixed

    public static GraphicsPreferences Default => new GraphicsPreferences { Mode = GraphicsMode.Auto, ChosenFps = 0 };
}

public static class GraphicsPreferencesRules
{
    private const int ModeCount = 4;

    public static GraphicsPreferences Sanitize(int storedMode, int storedFps)
    {
        var p = GraphicsPreferences.Default;
        if (storedMode >= 0 && storedMode < ModeCount)
            p.Mode = (GraphicsMode)storedMode;
        if (storedFps == 30 || storedFps == 60)
            p.ChosenFps = storedFps;
        return p;
    }

    public static MobileTier? ManualTier(GraphicsPreferences p)
    {
        switch (p.Mode)
        {
            case GraphicsMode.Low: return MobileTier.Low;
            case GraphicsMode.Medium: return MobileTier.Medium;
            case GraphicsMode.High: return MobileTier.High;
            default: return null;
        }
    }

    public static bool GovernorRuns(GraphicsPreferences p, bool killSwitchOn, bool autoAllowed) =>
        p.Mode == GraphicsMode.Auto && killSwitchOn && autoAllowed;

    public static bool FpsLocked(GraphicsPreferences p) => p.ChosenFps != 0;

    // Editor/PC in Auto keep their own frame rate and vSync; any choice, or a phone, sets them.
    public static bool AppliesFrameRate(GraphicsPreferences p, bool autoAllowed) =>
        p.Mode != GraphicsMode.Auto || autoAllowed || FpsLocked(p);

    public static int StartFps(GraphicsPreferences p, int managedFps) => FpsLocked(p) ? p.ChosenFps : managedFps;

    public static GraphicsMode Next(GraphicsMode m) => (GraphicsMode)(((int)m + 1) % ModeCount);

    public static GraphicsMode Previous(GraphicsMode m) => (GraphicsMode)(((int)m + ModeCount - 1) % ModeCount);

    // The FPS row shows the current rate; an arrow picks the other one. 0 counts as 60.
    public static int ToggleFps(int fps) => fps == 30 ? 60 : 30;
}
