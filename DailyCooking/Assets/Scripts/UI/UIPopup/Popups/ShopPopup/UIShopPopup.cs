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

    private DateTime _shownResetDate;
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
    }
    public override void HidePopup(object param)
    {
        base.HidePopup(param);
        if (_timer24HrsCoroutine != null)
            StopCoroutine(_timer24HrsCoroutine);
        _timer24HrsCoroutine = null;
        if (GameManager.Instance != null && GameManager.Instance.GameData != null)
            GameManager.Instance.GameData.ShopData.OnResourceChange -= RefreshDailyFreeItems;
    }
    public override void ShowPopup(object param)
    {
        base.ShowPopup(param);
        ShopData shopData = GameManager.Instance.GameData.ShopData;
        shopData.OnResourceChange -= RefreshDailyFreeItems;
        shopData.OnResourceChange += RefreshDailyFreeItems;
        RefreshDailyFreeItems();
        // Items are built once; re-read the lock state for the current restaurant.
        foreach (UIShopItem item in GetComponentsInChildren<UIShopItem>(true))
            item.Refresh();

        if (_timer24HrsCoroutine != null)
            StopCoroutine(_timer24HrsCoroutine);
        _timer24HrsCoroutine = CountDownToDailyReset();
        StartCoroutine(_timer24HrsCoroutine);
    }
    public void Initialize()
    {
        var listItem = ConfigManager.Instance.ConfigShop.ShopItems.FindAll(x => x.Type == ShopItemType.Item);
        var itemCategoryList = listItem.ToLookup(x => x.Category);
        foreach (var category in itemCategoryList)
        {
            GameObject shopCategory = Instantiate(_shopCategoryPrefab, _shopParent);
            shopCategory.gameObject.SetActive(true);
            UIShopCategoryItem shopCategoryItem = shopCategory.GetComponent<UIShopCategoryItem>();
            shopCategoryItem.SetCategory(category);
            _shopCategoryItems.Add(shopCategoryItem);
        }

        var ingredientList = ConfigManager.Instance.ConfigShop.ShopItems.FindAll(x => x.Type == ShopItemType.Ingredient);
        var ingredientCategoryList = ingredientList.ToLookup(x => x.Category);
        foreach (var ingredient in ingredientCategoryList)
        {
            GameObject shopCategory = Instantiate(_shopCategoryPrefab, _ingredientParent);
            shopCategory.gameObject.SetActive(true);
            UIShopCategoryItem shopCategoryItem = shopCategory.GetComponent<UIShopCategoryItem>();
            shopCategoryItem.SetCategory(ingredient);
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
    // Re-reads today's claim counts (the server resets them each UTC day) into the daily items.
    public void RefreshDailyFreeItems()
    {
        GameManager.Instance.GameData.ShopData.RefreshDailyShopOffer();
        _shownResetDate = DateTime.UtcNow.Date;
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
    // Counts down to the next UTC midnight, when the daily offers reset.
    private IEnumerator CountDownToDailyReset()
    {
        var wait = new WaitForSeconds(1);
        while (true)
        {
            DateTime utcNow = DateTime.UtcNow;
            if (utcNow.Date != _shownResetDate)
                RefreshDailyFreeItems();

            TimeSpan timeRemaining = utcNow.Date.AddDays(1) - utcNow;
            timerText.text = string.Format("{0:00}:{1:00}:{2:00}",
                timeRemaining.Hours + (timeRemaining.Days * 24),
                timeRemaining.Minutes,
                timeRemaining.Seconds);
            yield return wait;
        }
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
