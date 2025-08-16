using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIShopPopup : UIPopup
{
    [SerializeField] private Transform _shopParent;
    [SerializeField] private GameObject _shopCategoryPrefab;
    private List<UIShopCategoryItem> _shopCategoryItems = new List<UIShopCategoryItem>();

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

    }
    public override void ShowPopup(object param)
    {
        base.ShowPopup(param);

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
    public void OnCloseClick()
    {
        UIPopupManager.Instance.HidePopup(UIPopupType.UIShopPopup);
    }
}
