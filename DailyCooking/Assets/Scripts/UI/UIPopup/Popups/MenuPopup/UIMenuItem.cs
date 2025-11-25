using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuItem :MonoBehaviour
{
    [SerializeField] private Button buttonRemove;
    [SerializeField] private Image foodImage;
    [SerializeField] private TextMeshProUGUI foodNameText;
    [SerializeField] private TextMeshProUGUI foodPriceText;

    public Button ButtonRemove => buttonRemove; 

    public void SetMenuFoodItem(FoodSO item)
    {
        foodImage.sprite = item.Sprite;
        foodNameText.text = item.name;
        foodPriceText.text = $"Price: ${item.price}";
    }
}