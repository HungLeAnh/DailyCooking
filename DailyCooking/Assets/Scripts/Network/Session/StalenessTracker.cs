// "Haven't heard from the other side for 5 s": shows "Reconnecting…" / "Away" long before the
// 60 s transport timeout decides the connection is gone.
public sealed class StalenessTracker
{
    public const float StaleSeconds = 5f;

    private float lastHeard;
    private bool hasBaseline;

    public void Reset(float now)
    {
        lastHeard = now;
        hasBaseline = true;
    }

    public void Heard(float now) => Reset(now);

    public bool IsStale(float now) => hasBaseline && now - lastHeard > StaleSeconds;
}
