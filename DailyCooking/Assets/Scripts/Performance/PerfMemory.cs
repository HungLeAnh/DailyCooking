// What Auto remembers across launches: the last tier/FPS it had to drop to, and how many clean
// sessions have run since. RememberedTier -1 means no memory; RememberedFps 0 means none.
public struct PerfMemoryState
{
    public int RememberedTier;
    public int RememberedFps;
    public int CleanSessions;

    public bool HasMemory => RememberedTier >= 0;

    public static PerfMemoryState Empty => new PerfMemoryState { RememberedTier = -1, RememberedFps = 0, CleanSessions = 0 };
}

// Start at the remembered downgrade (never above the device pick); after enough clean sessions,
// retry one step up: 30 -> 60 at the same tier first, then one tier up, until back at the pick.
public static class PerfMemory
{
    public const int CleanSessionsToRetry = 3;

    public static (MobileTier tier, int fps) StartupChoice(MobileTier devicePick, PerfMemoryState s)
    {
        if (!s.HasMemory)
            return (devicePick, 60);
        MobileTier remembered = ClampTier(s.RememberedTier);
        MobileTier tier = remembered < devicePick ? remembered : devicePick;
        int fps = s.RememberedFps == 30 ? 30 : 60;
        return (tier, fps);
    }

    public static PerfMemoryState AfterSession(PerfMemoryState s, bool downgraded, MobileTier devicePick)
    {
        if (downgraded)
        {
            s.CleanSessions = 0;
            return s;
        }
        if (!s.HasMemory)
            return PerfMemoryState.Empty;

        s.CleanSessions++;
        if (s.CleanSessions < CleanSessionsToRetry)
            return s;

        MobileTier tier = ClampTier(s.RememberedTier);
        int fps = s.RememberedFps == 30 ? 30 : 60;
        if (fps == 30)
            fps = 60;
        else if (tier < MobileTier.High)
            tier++;
        if (tier > devicePick)
            tier = devicePick;

        if (tier >= devicePick && fps == 60)
            return PerfMemoryState.Empty;
        return new PerfMemoryState { RememberedTier = (int)tier, RememberedFps = fps, CleanSessions = 0 };
    }

    public static PerfMemoryState AfterDowngrade(PerfMemoryState s, MobileTier tier, int fps)
    {
        return new PerfMemoryState { RememberedTier = (int)tier, RememberedFps = fps, CleanSessions = 0 };
    }

    private static MobileTier ClampTier(int stored)
    {
        if (stored <= (int)MobileTier.Low) return MobileTier.Low;
        if (stored >= (int)MobileTier.High) return MobileTier.High;
        return (MobileTier)stored;
    }
}
