using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICharacterItem : MonoBehaviour
{
    public Action<int> ItemSelected;

    [SerializeField] private Image _itemPreviewImage = default;
    [SerializeField] private Image _bgImage = default;
    [SerializeField] public Button _itemButton = default;

    bool _isSelected = false;
    private Cosmetic cosmetic;
    private int currentIndex;
    public void SetItem(int index, Cosmetic cosmetic, bool isSelected)
    {
        this.cosmetic = cosmetic;
        this.currentIndex = index;
        _isSelected = isSelected;

        gameObject.SetActive(true);
        _itemPreviewImage.gameObject.SetActive(true);
        _itemButton.gameObject.SetActive(true);
        _itemPreviewImage.sprite = cosmetic.Icon;
    }

    public void SetInactiveItem()
    {
        gameObject.SetActive(false);
        _isSelected = false;
    }

    private void OnEnable()
    {
        if (_isSelected)
        { SelectItem(); }
    }
    public void OnItemClick()
    {
        _isSelected = !_isSelected;
        if (_isSelected)
        {
            SelectItem();
        }
        else
        {
            UnselectItem();
        }
    }

    public void SelectItem()
    {
        _isSelected = true;

        if (ItemSelected != null)
        {
            ItemSelected.Invoke(currentIndex);
        }
        else
        {
        }
    }

    public void UnselectItem()
    {
        _isSelected = false;
    }
}
