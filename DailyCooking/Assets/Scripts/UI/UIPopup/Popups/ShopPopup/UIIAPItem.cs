using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class UIIAPItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI productName;
    [SerializeField] private TextMeshProUGUI price;
    [SerializeField] private Button buyButton;

    [SerializeField] private ProductKeys product;
    private void Awake()
    {
        buyButton.onClick.AddListener(OnBuyClicked);
        UpdateUI();
    }

    private void OnBuyClicked()
    {
        IAPManager.Instance.BuyProduct(product);
    }

    private void UpdateUI()
    {
        var meta = IAPManager.Instance.GetProductMetaData(product).metadata;
        if (meta != null)
        {
            productName.text = meta.localizedTitle;
            price.text = meta.localizedPriceString;
        }
    }
}
