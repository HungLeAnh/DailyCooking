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
    // The server only accepts offers listed in ShopManager.CurrencyExchangeOffers.
    private void OnBuyClicked()
    {
        ShopManager.Instance.ExchangeCurrency(type, cost, currencyAmount);
    }

}
