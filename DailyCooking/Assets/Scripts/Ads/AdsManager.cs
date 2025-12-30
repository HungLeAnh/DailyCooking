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
#endif

    private int interstitialRetryAttempt;
    private int rewardedRetryAttempt;
    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayInterstitialAd interstitialAd;
    private Action callBackAction;

    public void Start()
    {
        // Register OnInitFailed and OnInitSuccess listeners
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
        LevelPlay.OnImpressionDataReady += ImpressionDataReadyEvent;
        // SDK init        
        LevelPlay.Init(APPKEY);
        //LevelPlay.SetMetaData("is_test_suite", "enable");
        LevelPlay.SetMetaData("do_not_sell", "true");
        LevelPlay.SetConsent(true);
        LevelPlay.SetMetaData("is_child_directed", "true");
        

    }
    public bool IsRewardedAdsLoaded()
    {
        return rewardedAd.IsAdReady();
    }
    public bool IsInterstitialAdsLoaded()
    {
        return interstitialAd.IsAdReady();
    }
    private void LoadIntertitialAds()
    {
        interstitialAd.LoadAd();
    }
    private void LoadRewardAds()
    {
        rewardedAd.LoadAd();
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
    private void ImpressionDataReadyEvent(LevelPlayImpressionData impressionData)
    {
        Debug.Log("unity-script: ImpressionDataReadyEvent impressionData = " + impressionData);
        //if (impressionData != null)
        //{
        //    Firebase.Analytics.Parameter[] AdParameters = {
        //    new Firebase.Analytics.Parameter("ad_platform", "ironSource"),
        //    new Firebase.Analytics.Parameter("ad_source", impressionData.adNetwork),
        //    new Firebase.Analytics.Parameter("ad_unit_name", impressionData.adUnit),
        //    new Firebase.Analytics.Parameter("ad_format", impressionData.instanceName),
        //    new Firebase.Analytics.Parameter("currency", "USD"),
        //    new Firebase.Analytics.Parameter("value", impressionData.revenue.Value)
        //};
        //    Firebase.Analytics.FirebaseAnalytics.LogEvent("custom_ad_impression", AdParameters);
        //}
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
        if (interstitialAd.IsAdReady() && !LevelPlayInterstitialAd.IsPlacementCapped(placementName))
        {
            interstitialAd.ShowAd(placementName);
        }
    }
    public void ShowRewardedAds(string placementName = "", Action callback = null)
    {
        if (rewardedAd.IsAdReady() && !LevelPlayRewardedAd.IsPlacementCapped(placementName))
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
        callBackAction?.Invoke();
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