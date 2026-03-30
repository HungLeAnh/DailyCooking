using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class UICurrencyItem : MonoBehaviour
{
    [SerializeField] private int currencyAmount;
    [SerializeField] private ShopItemType type;
    [SerializeField] private int cost;
    [SerializeField] private Button buyButton;
    private void Awake()
    {
        buyButton.onClick.AddListener(OnBuyClicked);
    }
    private void OnBuyClicked()
    {
        if(GameManager.Instance.GameData.RestaurantData.Gems < cost)
        {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameNotiPopup,
                new UIGameNotiPopup.Param
                {
                    Title = "Warning",
                    Message = "Not enough gems to buy this currency."
                });
            return;
        }
        GameManager.Instance.UpdateRestaurantGemsServerRpc(-cost);
        ShopManager.Instance.BuyCurrency(type,currencyAmount);
    }

}
