using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuItem :MonoBehaviour
{
    [SerializeField] private Button buttonRemove;
    [SerializeField] private Image foodImage;
    [SerializeField] private TextMeshProUGUI foodNameText;
    [SerializeField] private TextMeshProUGUI foodPriceText;
    [SerializeField] private TextMeshProUGUI foodEXPText;

    public Button ButtonRemove => buttonRemove; 

    public void SetMenuFoodItem(FoodSO item)
    {
        foodImage.sprite = item.Sprite;
        foodNameText.text = item.name;
        foodPriceText.text = $"${item.price}";
        foodEXPText.text = $"{item.exp} XP";
    }
}