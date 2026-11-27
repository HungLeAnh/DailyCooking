using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILevelUpPopup : UIPopup
{

    public class Param
    {
        public RewardData[] reward;
    }

    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject RewardItemPrefab;
    [SerializeField] private RectTransform RewardContainer;

    [Header("Icon")]
    [SerializeReference] private Sprite coinIcon;
    [SerializeReference] private Sprite gemIcon;


    private List<UILevelUpRewardItem> rewardItems = new List<UILevelUpRewardItem>();
    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);

        levelText.text = GameManager.Instance.GameData.RestaurantData.Level.ToString();
        if (_openParam != null)
        {
            Param popupParam = _openParam as Param;

            foreach (var data in popupParam.reward)
            {
                GameObject rewardItem = Instantiate(RewardItemPrefab, RewardContainer);
                rewardItem.SetActive(true);
                var item = rewardItem.GetComponent<UILevelUpRewardItem>();
                item.SetItem(GetRewardIcon(data.id),data.amount);
                rewardItems.Add(item);
            }
        }
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);

        foreach (var item in rewardItems)
        {
            Destroy(item.gameObject);
        }
        rewardItems.Clear();
        // Display only: the server granted the reward when the level went up (GameManager.ServerAddExp).
        _openParam = null;
    }

    private Sprite GetRewardIcon(string rewardId)
    {
        switch (rewardId)
        {
            case nameof(RewardData.RewardType.Coin):
                return coinIcon;
            case nameof(RewardData.RewardType.Gem):
                return gemIcon;
            default:
                return coinIcon;
        }
    }

    public void OnCloseButtonClicked()
    {
        HidePopup();
        // Every client shows its own Level_Up ad; a host with others in the room skips its own.
        if (GameManager.Instance != null && GameManager.Instance.HostShouldDelegateInterstitials)
            return;
        if (AdsManager.Instance.IsInterstitialAdsLoaded())
        {
            AdsManager.Instance.ShowInterstitialAds("Level_Up");
            GameManager.NoteLocalInterstitialShown();
        }
    }
    
}
