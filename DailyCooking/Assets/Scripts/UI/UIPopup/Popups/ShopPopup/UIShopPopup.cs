using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShopPopup : UIPopup
{
    [SerializeField] private Transform _shopParent;
    [SerializeField] private GameObject _shopCategoryPrefab;
    [Header("Free Currency")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private RectTransform _dailyOfferContainer;
     
    private List<UIShopCategoryItem> _shopCategoryItems = new List<UIShopCategoryItem>();
    private DateTime _targetTime;
    private IEnumerator _timer24HrsCoroutine;

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
        SetUpShopDailyFree();

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
}
[Serializable]
public class DailyFreeCurrency
{
    public ShopItemType Id;
    public int Count;
    public string Reward;
}
