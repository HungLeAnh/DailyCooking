using System;

// A stand-in ad for the Editor and tests: "shows" for DelaySeconds, then reports NextOutcome.
public sealed class FakeAdProvider : IAdProvider
{
    public bool HasAd = true;
    public float DelaySeconds = 3f;
    public AdOutcome NextOutcome = AdOutcome.Completed;

    private Action<AdOutcome> pending;
    private float remaining;

    public bool HasRewarded => HasAd;
    public bool HasInterstitial => HasAd;
    public bool IsShowing => pending != null;

    public void ShowRewarded(string placement, Action<AdOutcome> done) => Show(done);
    public void ShowInterstitial(string placement, Action<AdOutcome> done) => Show(done);

    public void Tick(float dt)
    {
        if (pending == null) return;
        remaining -= dt;
        if (remaining > 0f) return;
        Action<AdOutcome> done = pending;
        pending = null;
        done(NextOutcome);
    }

    private void Show(Action<AdOutcome> done)
    {
        if (!HasAd || pending != null)
        {
            done?.Invoke(AdOutcome.NoAd);
            return;
        }
        pending = done ?? (o => { });
        remaining = DelaySeconds;
    }
}
