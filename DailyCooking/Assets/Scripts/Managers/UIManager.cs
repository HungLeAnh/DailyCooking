using UnityEngine;

public class UIManager : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.GameData.PlayerStats.OnLevelUp += ShowLevelUpPopup;
    }

    private void OnDestroy()
    {
        GameManager.Instance.GameData.PlayerStats.OnLevelUp -= ShowLevelUpPopup;
    }

    private void ShowLevelUpPopup(int level)
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UILevelUpPopup.ToString(), 
            new UILevelUpPopup.Param { reward = new RewardData[] { new RewardData(UILevelUpPopup.RewardType.Coin.ToString(), level * 100) } });
    }
}