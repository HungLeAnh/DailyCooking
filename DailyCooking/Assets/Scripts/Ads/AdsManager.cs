using System;
using Unity.Services.LevelPlay;
using UnityEngine;
public enum AdsType
{
    // rewarded Ads
    Free_Gem,
    Free_Cash,

    // intertitial ads
    Break_Time,
    AFK,

    Unknown = 999
}
public class AdsManager : PersistentSingleton<AdsManager>
{
    private const string APPKEY = "24b0ee825";
#if UNITY_ANDROID
    private const string InterstitialAdUnitId = "5rzvxdtegit9amc8";
    private const string RewardedAdUnitId = "4hamsbcysnxm8xlp";
    private const string BannerAdUnitId = "";
#else
    private const string InterstitialAdUnitId = "";
    private const string RewardedAdUnitId = "";
    private const string BannerAdUnitId = "";
#endif

    private int interstitialRetryAttempt;
    private int rewardedRetryAttempt;
    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayInterstitialAd interstitialAd;
    private Action callBackAction;

    public void Start()
    {
        if (string.IsNullOrEmpty(RewardedAdUnitId) || string.IsNullOrEmpty(InterstitialAdUnitId))
        {
            Debug.Log("AdsManager: no ad units for this platform, ads disabled.");
            return;
        }
        // Privacy settings only apply if set before Init.
        // TODO: GDPR consent is still granted for everyone; ask the player (a consent prompt or a
        // certified CMP) before serving ads in regions that require it.
        LevelPlayPrivacySettings.SetGDPRConsent(true);
        LevelPlayPrivacySettings.SetCCPA(true);   // do not sell personal information
        LevelPlayPrivacySettings.SetCOPPA(false); // general audience, not child-directed
        //LevelPlay.SetMetaData("is_test_suite", "enable");

        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
        LevelPlay.Init(APPKEY);
    }

    private void OnDestroy()
    {
        LevelPlay.OnInitSuccess -= SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed -= SdkInitializationFailedEvent;
        rewardedAd?.DestroyAd();
        interstitialAd?.DestroyAd();
    }
    public bool IsRewardedAdsLoaded()
    {
        return rewardedAd != null && rewardedAd.IsAdReady();
    }
    public bool IsInterstitialAdsLoaded()
    {
        return interstitialAd != null && interstitialAd.IsAdReady();
    }
    private void LoadIntertitialAds()
    {
        interstitialAd?.LoadAd();
    }
    private void LoadRewardAds()
    {
        rewardedAd?.LoadAd();
    }
    private void CreateRewardedAd()
    {
        // Register to Rewarded events
        var configBuilder = new LevelPlayRewardedAd.Config.Builder().SetBidFloor(0.2).Build();
        rewardedAd = new LevelPlayRewardedAd(RewardedAdUnitId, configBuilder);
        rewardedAd.OnAdLoaded += RewardedOnAdLoadedEvent;
        rewardedAd.OnAdLoadFailed += RewardedOnAdLoadFailedEvent;
        rewardedAd.OnAdDisplayed += RewardedOnAdDisplayedEvent;
        rewardedAd.OnAdDisplayFailed += RewardedOnAdDisplayFailedEvent;
        rewardedAd.OnAdClicked += RewardedOnAdClickedEvent;
        rewardedAd.OnAdClosed += RewardedOnAdClosedEvent;
        rewardedAd.OnAdInfoChanged += RewardedOnAdInfoChangedEvent;
        
        rewardedAd.OnAdRewarded += RewardedOnAdRewardedEvent;
        rewardedAd.LoadAd();

    }
    private void CreateInterstitialAd()
    {
        // Register to Interstitial events
        var configBuilder = new LevelPlayInterstitialAd.Config.Builder().SetBidFloor(0.2).Build();
        interstitialAd = new LevelPlayInterstitialAd(InterstitialAdUnitId, configBuilder);
        interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
        interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
        interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
        interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
        interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
        interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
        interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;
        interstitialAd.LoadAd();

    }
    private void SdkInitializationFailedEvent(LevelPlayInitError error)
    {
        Debug.LogError($"{error.ToString()}");
    }

    private void SdkInitializationCompletedEvent(LevelPlayConfiguration configuration)
    {
        Debug.Log("LevelPlay Initialized! Now it is safe to load ads.");
        //LevelPlay.LaunchTestSuite();
        CreateRewardedAd();
        CreateInterstitialAd();
    }

    public void ShowInterstitialAds(string placementName = "")
    {
        if (IsInterstitialAdsLoaded())
        {
            interstitialAd.ShowAd(placementName);
        }
    }
    public void ShowRewardedAds(string placementName = "", Action callback = null)
    {
        if (IsRewardedAdsLoaded())
        {
            callBackAction = callback;
            rewardedAd.ShowAd(placementName);
        }
    }
    // Implement the RewardAds events
    private void RewardedOnAdLoadedEvent(LevelPlayAdInfo adInfo) 
    {
        rewardedRetryAttempt = 0;
    }
    private void RewardedOnAdLoadFailedEvent(LevelPlayAdError error) 
    {
        rewardedRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, rewardedRetryAttempt));

        Invoke(nameof(LoadRewardAds), (float)retryDelay);
    }
    private void RewardedOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
    private void RewardedOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) { }
    private void RewardedOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward adReward)
    {
        var callback = callBackAction;
        callBackAction = null;
        callback?.Invoke();
    }
    private void RewardedOnAdClosedEvent(LevelPlayAdInfo adInfo) 
    {
        rewardedAd.LoadAd();
    }
    private void RewardedOnAdClickedEvent(LevelPlayAdInfo adInfo) { }
    private void RewardedOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) { }
    // Implement the InterstitialAds events
    private void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo) 
    {
        interstitialRetryAttempt = 0;
    }
    private void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error) 
    {
        interstitialRetryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, interstitialRetryAttempt));

        Invoke(nameof(LoadIntertitialAds), (float)retryDelay);
    }
    private void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
    private void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) { }
    private void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo) { }
    private void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo) 
    {
        interstitialAd.LoadAd();
    }
    private void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) { }
}