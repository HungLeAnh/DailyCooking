using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIInventoryTab : MonoBehaviour
{
    public UnityAction<InventoryTab> TabClicked;

    [SerializeField] private Image _tabImage = default;
    [SerializeField] private TextMeshProUGUI _tabText = default;
    [SerializeField] private Button _actionButton = default;
    [SerializeField] private Color _selectedIconColor = default;
    [SerializeField] private Color _deselectedIconColor = default;

    private InventoryTab _tabType;
    public InventoryTab TabType => _tabType;

    public void SetTab(InventoryTab tabType, bool isSelected)
    {
        _tabType = tabType;
        _tabImage.sprite = tabType.TabIcon;
        _tabText.text = tabType.TabType.ToString();
        UpdateState(isSelected);
    }

    public void UpdateState(bool isSelected)
    {
        _actionButton.interactable = !isSelected;

        if (isSelected)
        {
            _tabImage.color = _selectedIconColor;
        }
        else
        {
            _tabImage.color = _deselectedIconColor;
        }
    }

    public void ClickButton()
    {
        TabClicked.Invoke(_tabType);
    }
}
