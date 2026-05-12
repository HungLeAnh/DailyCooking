using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

public class UIShopPopup : UIPopup
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform _shopParent;
    [SerializeField] private Transform _ingredientParent;
    [SerializeField] private GameObject _shopCategoryPrefab;
    [SerializeField] private Button closeButton;
    [Header("Free Currency")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private RectTransform _dailyOfferContainer;
     
    private List<UIShopCategoryItem> _shopCategoryItems = new List<UIShopCategoryItem>();

    private DateTime _targetTime;
    private IEnumerator _timer24HrsCoroutine;

    public Button CloseButton => closeButton;

    public void Awake()
    {
        _shopCategoryPrefab.SetActive(false);

    }
    public override void SetupPopup()
    {
        base.SetupPopup();
        Initialize();
        _targetTime = DateTime.Today.AddDays(1);
    }
    public override void HidePopup(object param)
    {
        base.HidePopup(param);
        if (_timer24HrsCoroutine != null)
            StopCoroutine(_timer24HrsCoroutine);
    }
    public override void ShowPopup(object param)
    {
        base.ShowPopup(param);
        SetUpShopDailyFree();

    }
    public void Initialize()
    {
        var categoryList = ConfigManager.Instance.ConfigShop.ShopItems.ToLookup(x => x.Category);
        foreach (var category in categoryList)
        {
            GameObject shopCategory = Instantiate(_shopCategoryPrefab, _shopParent);
            shopCategory.gameObject.SetActive(true);
            UIShopCategoryItem shopCategoryItem = shopCategory.GetComponent<UIShopCategoryItem>();
            shopCategoryItem.SetCategory(category);
            _shopCategoryItems.Add(shopCategoryItem);
        }
    }
    public void SnapTo(RectTransform target,Action cb = null)
    {
        Canvas.ForceUpdateCanvases();
        var contentPanel = _shopParent as RectTransform;
        Vector2 targetLocalPos = scrollRect.transform.InverseTransformPoint(target.position);
        Vector2 contentLocalPos = scrollRect.transform.InverseTransformPoint(contentPanel.position);

        float viewportHeight = scrollRect.GetComponent<RectTransform>().rect.height;
        float centerOffset = viewportHeight / 2f;

        float centeredY = (contentLocalPos.y - targetLocalPos.y) - centerOffset;
        contentPanel.DOAnchorPosY(centeredY, 0.5f).SetEase(Ease.OutCubic)
            .OnComplete(() => { cb?.Invoke(); }); 
    }

    public void ScrollTo(ShopItemCategory targetCatergory, UIShopItem shopItem = null,Action cb = null)
    {        
        foreach (var item in _shopCategoryItems)
        {
            if (item.ItemCategory == targetCatergory)
            {
                SnapTo(item.gameObject.transform as RectTransform, 
                () => {
                    if (shopItem != null)
                        item.SnapTo(shopItem.gameObject.transform as RectTransform,
                        () => { 
                            cb?.Invoke();
                        });
                });

                break;
            }
        }
    }
    public UIShopItem GetUIShopItem(ShopItemCategory targetCatergory, string itemName)
    {
        foreach (var categoryItem in _shopCategoryItems)
        {
            if (categoryItem.ItemCategory == targetCatergory)
            {
                foreach(var shopItem in categoryItem.ShopItems)
                {
                    if (shopItem.ConfigShopItem.Name.Contains(itemName,StringComparison.CurrentCultureIgnoreCase))
                    {
                        return shopItem;
                    }
                }
                return null;
            }
        }
        return null;
    }
    public void OnCloseClick()
    {
        UIPopupManager.Instance.HidePopup(UIPopupType.UIShopPopup);
    }
    private void SetUpShopDailyFree()
    {
        bool success = GameManager.Instance.GameData.ShopData.RefreshDailyShopOffer();
        if (success)
        {
            for (int i = 0; i < _dailyOfferContainer.childCount; i++)
            {
                GameObject item = _dailyOfferContainer.GetChild(i).gameObject;

                if (item == null)
                    continue;

                item.SetActive(true);
                var dailyFreeItem = item.GetComponent<UIDailyFreeItem>();
                dailyFreeItem.Setup(i);
            }
        }
        _timer24HrsCoroutine = CountDown24Hours();
        StartCoroutine(_timer24HrsCoroutine);
    }
    private IEnumerator CountDown24Hours()
    {
        TimeSpan timeRemaining = _targetTime - DateTime.Now;

        while(timeRemaining.TotalSeconds > 0)
        {
            string timeString = string.Format("{0:00}:{1:00}:{2:00}",
                timeRemaining.Hours + (timeRemaining.Days * 24),
                timeRemaining.Minutes,
                timeRemaining.Seconds);

            timerText.text = timeString;
            yield return new WaitForSeconds(1);
            timeRemaining = _targetTime - DateTime.Now;
        }
        yield return new WaitForSeconds(1);
        StopCoroutine(_timer24HrsCoroutine);
        SetUpShopDailyFree();
    }
    public void OnPurchase(int id)
    {
        IAPManager.Instance.BuyProduct((ProductKeys)id);
    }
}
[Serializable]
public class DailyFreeCurrency
{
    public ShopItemType Id;
    public int Count;
    public string Reward;
}
