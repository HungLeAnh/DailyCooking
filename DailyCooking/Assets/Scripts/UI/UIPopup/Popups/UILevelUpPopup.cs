using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class UILevelUpPopup : UIPopup
{
    public enum RewardType
    {
        Coin,
        Gem,
    }
    public class Param
    {
        public string reward;
    }

    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject RewardItemPrefab;
    [SerializeField] private RectTransform RewardContainer;

    [Header("Icon")]
    [SerializeReference] private Sprite coinIcon;
    [SerializeReference] private Sprite gemIcon;


    private Dictionary<string, int> parsedData = new Dictionary<string, int>();
    private List<UILevelUpRewardItem> rewardItems = new List<UILevelUpRewardItem>();
    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        Show();
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
        Hide();
    }

    private void Show()
    {
        levelText.text = GameManager.Instance.GameData.playerData.level.ToString();
        if (_openParam != null)
        {
            Param param = _openParam as Param;
            string[] pairs = param.reward.Split(';');

            foreach (var pair in pairs)
            {
                // Split each pair by '_' to get id and amount
                string[] parts = pair.Split('_');
                if (parts.Length == 2)
                {
                    string id = parts[0];
                    int amount = int.Parse(parts[1]);
                    parsedData[id] = amount;
                }
            }

            foreach (var data in parsedData)
            {
                GameObject rewardItem = Instantiate(RewardItemPrefab, RewardContainer);
                rewardItem.SetActive(true);
                var item = rewardItem.GetComponent<UILevelUpRewardItem>();
                item.SetItem(GetRewardIcon(data.Key),data.Value);
                rewardItems.Add(item);
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

    private void Hide()
    {
        foreach (var item in rewardItems)
        {
            Destroy(item.gameObject);
        }
        foreach(var item in parsedData)
        {
            if (item.Key == nameof(RewardType.Coin))
            {
                GameManager.Instance.GameData.UpdatePlayerResources(item.Value);
            }
            else if (item.Key == nameof(RewardType.Gem))
            {
            }
        }
    }
    public void OnCloseButtonClicked()
    {
        HidePopup();
    }
    
}
