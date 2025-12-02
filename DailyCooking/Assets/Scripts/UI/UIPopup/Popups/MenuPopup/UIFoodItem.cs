using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFoodItem :MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image frameImage;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button foodButton;
    [SerializeField] private Image foodImage;
    [SerializeField] private TextMeshProUGUI foodNameText;
    [SerializeField] private TextMeshProUGUI foodPriceText;
    [SerializeField] private TextMeshProUGUI foodStatusText;

    [SerializeField] private Color selectedBackgroundColor;
    [SerializeField] private Color normalBackgroundColor;

    [SerializeField] private Color selectedFrameColor;
    [SerializeField] private Color normalFrameColor;

    private FoodSO foodSO;
    public FoodSO FoodSO { get => foodSO; }
    public Button FoodButton { get => foodButton; }
    public void SetMenuFoodItem(FoodSO item, bool isSelected)
    {
        this.foodSO = item;
        foodImage.sprite = item.Sprite;
        foodNameText.text = item.recipeName;
        foodPriceText.text = $"${item.price}";
        infoButton.onClick.AddListener(() =>
        {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIFoodDetailPopup,
                new UIFoodDetailPopup.Param { foodSO = item });
        });
        SetSelectedState(isSelected);
    }

    public void SetSelectedState(bool isSelected)
    {
        foodStatusText.text = isSelected ? "In Menu" : "Add to Menu";
        backgroundImage.color = isSelected ? selectedBackgroundColor : normalBackgroundColor;
        frameImage.color = isSelected ? selectedFrameColor : normalFrameColor;
    }

}