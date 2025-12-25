using System;
using Unity.Services.LevelPlay;
using UnityEngine;
using UnityEngine.Tilemaps;
public class AdsManager : PersistentSingleton<AdsManager>
{
    private const string APPKEY = "24b0ee825";
#if UNITY_ANDROID
    private const string InterstitialAdUnitId = "5rzvxdtegit9amc8";
    private const string RewardedAdUnitId = "4hamsbcysnxm8xlp";
    private const string BannerAdUnitId = "";
#endif
    private LevelPlayRewardedAd rewardedAd;
    private LevelPlayInterstitialAd interstitialAd;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        // Register OnInitFailed and OnInitSuccess listeners
        LevelPlay.OnInitSuccess += SdkInitializationCompletedEvent;
        LevelPlay.OnInitFailed += SdkInitializationFailedEvent;
        LevelPlay.OnImpressionDataReady += ImpressionDataReadyEvent;
        // SDK init        
        LevelPlay.Init(APPKEY);
        LevelPlay.SetMetaData("is_test_suite", "enable");
        LevelPlay.SetMetaData("do_not_sell", "true");
        LevelPlay.SetConsent(true);
        LevelPlay.SetMetaData("is_child_directed", "true");

        CreateRewardedAd();
        CreateInterstitialAd();
        LevelPlay.ValidateIntegration();
        
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
        LevelPlay.LaunchTestSuite();
    }
    public void LoadInterstitialAd()
    {
        interstitialAd.LoadAd();
    }
    public void PlayInterstitialAds(string placementName = null)
    {
        if (interstitialAd.IsAdReady() && !LevelPlayInterstitialAd.IsPlacementCapped(placementName))
        {
            interstitialAd.ShowAd(placementName);
        }
    }
    public void LoadRewardedAd()
    {
        rewardedAd.LoadAd();
    }
    public void PlayRewardedAds(string placementName = null)
    {
        if (rewardedAd.IsAdReady() && !LevelPlayRewardedAd.IsPlacementCapped(placementName))
        {
            rewardedAd.ShowAd(placementName);
        }
    }
    // Implement the RewardAds events
    private void RewardedOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }
    private void RewardedOnAdLoadFailedEvent(LevelPlayAdError error) { }
    private void RewardedOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
    private void RewardedOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) { }
    private void RewardedOnAdRewardedEvent(LevelPlayAdInfo adInfo, LevelPlayReward adReward) { }
    private void RewardedOnAdClosedEvent(LevelPlayAdInfo adInfo) { }
    private void RewardedOnAdClickedEvent(LevelPlayAdInfo adInfo) { }
    private void RewardedOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) { }
    // Implement the InterstitialAds events
    private void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }
    private void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error) { }
    private void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
    private void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) { }
    private void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo) { }
    private void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo) { }
    private void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) { }
}