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
    }

    // Products may still be loading from the store, and buying is only for the host.
    private void OnEnable()
    {
        if (IAPManager.Instance == null) return;
        IAPManager.Instance.OnProductsReady -= UpdateUI;
        IAPManager.Instance.OnProductsReady += UpdateUI;
        UpdateUI();
    }

    private void OnDisable()
    {
        if (IAPManager.Instance != null)
            IAPManager.Instance.OnProductsReady -= UpdateUI;
    }

    private void OnBuyClicked()
    {
        IAPManager.Instance.BuyProduct(product);
    }

    private void UpdateUI()
    {
        buyButton.interactable = IAPManager.Instance.CanPurchase();
        var meta = IAPManager.Instance.GetProductMetaData(product)?.metadata;
        if (meta != null)
        {
            productName.text = meta.localizedTitle;
            price.text = meta.localizedPriceString;
        }
    }
}
