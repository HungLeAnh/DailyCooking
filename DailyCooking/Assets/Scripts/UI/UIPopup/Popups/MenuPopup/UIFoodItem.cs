using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFoodItem :MonoBehaviour
{
    [SerializeField] private Button foodButton;
    [SerializeField] private Image foodImage;
    [SerializeField] private TextMeshProUGUI foodNameText;
    [SerializeField] private TextMeshProUGUI foodPriceText;
    [SerializeField] private TextMeshProUGUI foodStatusText;

    public Button FoodButton { get => foodButton; }

    public void SetMenuFoodItem(FoodSO item)
    {
        foodImage.sprite = item.Sprite;
        foodNameText.text = item.name;
        foodPriceText.text = $"Price: ${item.price}";
    }
}