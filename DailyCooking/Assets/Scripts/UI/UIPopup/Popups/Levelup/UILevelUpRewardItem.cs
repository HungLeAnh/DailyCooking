using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILevelUpRewardItem : MonoBehaviour
{
    [SerializeField] private Image rewardIcon;
    [SerializeField] private TextMeshProUGUI itemAmountText;
    public void SetItem(Sprite icon, int amount)
    {
        rewardIcon.sprite = icon;
        itemAmountText.text = MathUtil.NumberFormat(amount);
    }
}
