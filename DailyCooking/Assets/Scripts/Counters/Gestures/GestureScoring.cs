// Turns one measured gesture into 1-3 stars. The server calls these with the number the client
// reported; the close-up prompts use the same constants to draw their windows and bands.
public static class GestureScoring
{
    public const float CutGood = 0.7f;
    public const float CutOk = 0.35f;

    public const float FlipWindowStart = 0.45f;
    public const float FlipWindowEnd = 0.65f;
    public const float FlipMargin = 0.15f;

    public const float StirIntervalSeconds = 2f;

    public const float PourBandMin = 0.8f;
    public const float PourBandMax = 0.95f;

    public const float WatchZoneStart = 0.1f;
    public const float WatchZoneEnd = 0.5f;

    // centredness: 1 = straight through the middle of the food, 0 = at its edge.
    public static int CutStars(float centredness)
    {
        if (centredness >= CutGood) return 3;
        if (centredness >= CutOk) return 2;
        return 1;
    }

    // flipFraction: cook progress (0-1) when flipped; negative = never flipped.
    public static int FlipStars(float flipFraction)
    {
        if (flipFraction < 0f) return 1;
        if (flipFraction >= FlipWindowStart && flipFraction <= FlipWindowEnd) return 3;
        if (flipFraction >= FlipWindowStart - FlipMargin && flipFraction <= FlipWindowEnd + FlipMargin) return 2;
        return 1;
    }

    public static int StirStars(int answered, int expected)
    {
        if (expected <= 0 || answered >= expected) return 3;
        if (answered * 2 >= expected) return 2;
        return 1;
    }

    // level: fill level when released (1 = full); negative = never released.
    public static int PourStars(float level)
    {
        if (level < 0f || level > PourBandMax) return 1;
        if (level >= PourBandMin) return 3;
        return 2;
    }

    // burnFraction: how far into the burn time the food was taken out. Burned food is scored elsewhere.
    public static int WatchStars(float burnFraction)
    {
        return burnFraction >= WatchZoneStart && burnFraction <= WatchZoneEnd ? 3 : 2;
    }
}
