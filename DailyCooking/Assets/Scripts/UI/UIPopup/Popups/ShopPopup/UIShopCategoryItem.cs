using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIShopCategoryItem : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform _categoryParent;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private TextMeshProUGUI categoryTitle;
    private List<UIShopItem> _shopItems = new List<UIShopItem>();
    private ShopItemCategory itemCategory;

    public ShopItemCategory ItemCategory  => itemCategory;

    public List<UIShopItem> ShopItems { get => _shopItems; set => _shopItems = value; }

    public void SetCategory(IGrouping<ShopItemCategory, ConfigShopItem> category)
    {        
        _itemPrefab.SetActive(false);

        var listItem = category.OrderBy(x => x.UnlockLevel).ToList();
        itemCategory = category.Key;
        categoryTitle.text = ItemCategory.ToString();
        foreach (var item in listItem)
        {
            GameObject shopItem = Instantiate(_itemPrefab, _categoryParent);
            UIShopItem shopItemComponent = shopItem.GetComponent<UIShopItem>();
            shopItemComponent.SetItem(item,ItemCategory);
            ShopItems.Add(shopItemComponent);
        }

    }
    public void SnapTo(RectTransform target, Action cb = null)
    {
        Canvas.ForceUpdateCanvases();
        var contentPanel = _categoryParent as RectTransform;

        Vector2 targetLocalPos = scrollRect.transform.InverseTransformPoint(target.position);
        Vector2 contentLocalPos = scrollRect.transform.InverseTransformPoint(contentPanel.position);
        float viewportWidth = scrollRect.GetComponent<RectTransform>().rect.width;
        float centerOffset = viewportWidth / 2f;

        float centeredX = (contentLocalPos.x - targetLocalPos.x) + centerOffset;
        contentPanel.DOAnchorPosX(centeredX, 0.5f).SetEase(Ease.OutCubic)
            .OnComplete(() => { cb?.Invoke(); });
    }
    public UIShopItem GetItemByName(string name)
    {
        var item = _shopItems.Find(x => x.ConfigShopItem.Name.Contains(name));
        return item;
    }
}
