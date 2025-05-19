using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIShopPopup : UIPopup
{
    [SerializeField] private Transform _shopParent;
    [SerializeField] private GameObject _shopCategoryPrefab;
    private List<UIShopCategoryItem> _shopCategoryItems = new List<UIShopCategoryItem>();

    public override void SetupPopup()
    {
        base.SetupPopup();
        Initialize();
    }
    public override void HidePopup(object param)
    {
        base.HidePopup();   

    }
    public override void ShowPopup()
    {
        base.ShowPopup();

    }   
    public void Initialize()
    {
        var groupList = ConfigManager.Instance.ConfigShop.ShopItems.ToLookup(x => x.Category);
        foreach (var category in groupList)
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
        UIPopupManager.Instance.HidePopup(UIPopupType.UIShopPopup.ToString());
    }
}
