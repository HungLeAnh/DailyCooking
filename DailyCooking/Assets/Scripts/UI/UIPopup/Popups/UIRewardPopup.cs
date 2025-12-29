using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
public class UIRewardPopup : UIPopup
{
    public enum RewardType
    {
        Coin,
        Gem,
    }
    public class Param
    {
        public RewardData[] reward;
    }

    [SerializeField] private GameObject RewardItemPrefab;
    [SerializeField] private RectTransform RewardContainer;

    [Header("Icon")]
    [SerializeReference] private Sprite coinIcon;
    [SerializeReference] private Sprite gemIcon;


    private List<UILevelUpRewardItem> rewardItems = new List<UILevelUpRewardItem>();
    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);

        if (_openParam != null)
        {
            Param popupParam = _openParam as Param;

            foreach (var data in popupParam.reward)
            {
                GameObject rewardItem = Instantiate(RewardItemPrefab, RewardContainer);
                rewardItem.SetActive(true);
                var item = rewardItem.GetComponent<UILevelUpRewardItem>();
                item.SetItem(GetRewardIcon(data.id), data.amount);
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
        if (_openParam != null)
        {
            Param popupParam = _openParam as Param;
            foreach (var item in popupParam.reward)
            {
                if (item.id == nameof(RewardType.Coin))
                {
                    GameManager.Instance.GameData.PlayerStats.UpdatePlayerCoins(item.amount);
                }
                else if (item.id == nameof(RewardType.Gem))
                {
                    GameManager.Instance.GameData.PlayerStats.UpdatePlayerGems(item.amount);

                }
            }
        }
    }

    private Sprite GetRewardIcon(string rewardId)
    {
        switch (rewardId)
        {
            case nameof(RewardType.Coin):
                return coinIcon;
            case nameof(RewardType.Gem):
                return gemIcon;
            default:
                return coinIcon;
        }
    }

    public void OnCloseButtonClicked()
    {
        HidePopup();
    }

}