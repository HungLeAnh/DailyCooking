using UnityEngine;

public class UIManager : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.GameData.playerStats.OnLevelUp += ShowLevelUpPopup;
    }

    private void OnDestroy()
    {
        GameManager.Instance.GameData.playerStats.OnLevelUp -= ShowLevelUpPopup;
    }

    private void ShowLevelUpPopup(int level)
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UILevelUpPopup.ToString(), 
            new UILevelUpPopup.Param { reward = $"{UILevelUpPopup.RewardType.Coin}_{level*100}"});
    }
}
