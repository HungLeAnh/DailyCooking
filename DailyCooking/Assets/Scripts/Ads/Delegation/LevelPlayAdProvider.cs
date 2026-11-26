using System;

// The real ads on a phone, through AdsManager (LevelPlay).
public sealed class LevelPlayAdProvider : IAdProvider
{
    public bool HasRewarded => AdsManager.Instance != null && AdsManager.Instance.IsRewardedAdsLoaded();
    public bool HasInterstitial => AdsManager.Instance != null && AdsManager.Instance.IsInterstitialAdsLoaded();
    public bool IsShowing => AdsManager.Instance != null && AdsManager.Instance.IsShowingAd;

    public void ShowRewarded(string placement, Action<AdOutcome> done)
    {
        if (!HasRewarded) { done?.Invoke(AdOutcome.NoAd); return; }
        AdsManager.Instance.ShowRewarded(placement, done);
    }

    public void ShowInterstitial(string placement, Action<AdOutcome> done)
    {
        if (!HasInterstitial) { done?.Invoke(AdOutcome.NoAd); return; }
        AdsManager.Instance.ShowInterstitial(placement, done);
    }
}
