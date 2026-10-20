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

    private FoodSO foodSO;

    public Button ButtonRemove => buttonRemove; 
    public FoodSO FoodSO => foodSO;

    public void SetMenuFoodItem(FoodSO item)
    {
        foodSO = item;
        // The popup pools these cards and re-binds them, so drop the previous remove handler.
        buttonRemove.onClick.RemoveAllListeners();
        foodImage.sprite = item.Sprite;
        foodNameText.text = item.name;
        foodPriceText.text = $"${item.price}";
        foodEXPText.text = $"{item.exp} XP";
    }
}
