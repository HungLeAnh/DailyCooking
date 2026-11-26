// The three graphics tiers used on phones; each maps to a quality level of the same name.
public enum MobileTier
{
    Low = 0,
    Medium = 1,
    High = 2,
}

public static class MobileTiers
{
    public static string QualityLevelName(MobileTier tier)
    {
        switch (tier)
        {
            case MobileTier.Low: return "Mobile Low";
            case MobileTier.High: return "Mobile High";
            default: return "Mobile Medium";
        }
    }

    // Small props (food, plates, cups) only cast shadows on the highest tier.
    public static bool PropShadowsOn(MobileTier tier) => tier == MobileTier.High;
}
