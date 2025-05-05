using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class UIShopCategoryItem : MonoBehaviour
{
    [SerializeField] private Transform _categoryParent;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private TextMeshProUGUI categoryTitle;
    private List<UIShopItem> _shopItems = new List<UIShopItem>();
    private ShopItemCategory itemCategory;

    public void SetCategory(IGrouping<ShopItemCategory, ConfigShopItem> category)
    {
        itemCategory = category.Key;
        categoryTitle.text = itemCategory.ToString();
        foreach (var item in category)
        {
            GameObject shopItem = Instantiate(_itemPrefab, _categoryParent);
            UIShopItem shopItemComponent = shopItem.GetComponent<UIShopItem>();
            shopItemComponent.SetItem(item,itemCategory);
            _shopItems.Add(shopItemComponent);
        }
    }
}
