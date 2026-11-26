// First guess of the tier a phone can run, from what it reports about itself. The thresholds are
// estimates until measured on real devices.
public static class DeviceTierPicker
{
    public const int HighMinMemoryMB = 6144;
    public const int HighMinGraphicsMemoryMB = 2048;
    public const int HighMinCores = 8;
    public const int LowBelowMemoryMB = 3072;
    public const int LowBelowCores = 6;

    public static MobileTier Pick(int systemMemoryMB, int graphicsMemoryMB, int processorCount)
    {
        if (systemMemoryMB < LowBelowMemoryMB || processorCount < LowBelowCores)
            return MobileTier.Low;
        if (systemMemoryMB >= HighMinMemoryMB && graphicsMemoryMB >= HighMinGraphicsMemoryMB && processorCount >= HighMinCores)
            return MobileTier.High;
        return MobileTier.Medium;
    }
}
