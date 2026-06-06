using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICharacterItem : MonoBehaviour
{
    public Action<int> ItemSelected;
    public Action<int> ItemUnlocked;

    [SerializeField] private Image itemPreviewImage = default;
    [SerializeField] private Image bgImage = default;
    [SerializeField] private Button itemButton = default;
    [SerializeField] private TextMeshProUGUI priceText = default;
    [SerializeField] private GameObject LockItem;
    [SerializeField] private Button UnlockItemButton;
    bool isSelected = false;
    private Cosmetic cosmetic;
    private int currentIndex;
    private bool isLocked = false;

    private void Awake()
    {
        if (UnlockItemButton != null)
        {
            UnlockItemButton.onClick.AddListener(() =>
            {
                ItemUnlocked?.Invoke(currentIndex);
            });
        }
    }

    public void SetItem(int index, Cosmetic cosmetic, bool isSelected, bool isLocked)
    {
        this.cosmetic = cosmetic;
        this.currentIndex = index;
        this.isSelected = isSelected;
        this.isLocked = isLocked;

        gameObject.SetActive(true);
        itemPreviewImage.gameObject.SetActive(true);
        itemButton.gameObject.SetActive(true);
        itemPreviewImage.sprite = cosmetic.Icon;

        LockItem.SetActive(isLocked);
        itemButton.interactable = !isLocked;
        
        if (UnlockItemButton != null)
        {
            UnlockItemButton.gameObject.SetActive(isLocked);
        }

        if (priceText != null)
        {
            priceText.text = cosmetic.Price.ToString();
            priceText.gameObject.SetActive(isLocked);
        }
    }

    public void SetInactiveItem()
    {
        gameObject.SetActive(false);
        isSelected = false;
    }

    private void OnEnable()
    {
        if (isSelected)
        { SelectItem(); }
    }
    public void OnItemClick()
    {
        isSelected = !isSelected;
        if (isSelected)
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
        isSelected = true;

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
        isSelected = false;
    }
}
