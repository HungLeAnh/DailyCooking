using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFoodItem :MonoBehaviour
{
    [SerializeField] private Button infoButton;
    [SerializeField] private Button foodButton;
    [SerializeField] private Image foodImage;
    [SerializeField] private TextMeshProUGUI foodNameText;
    [SerializeField] private TextMeshProUGUI foodPriceText;
    [SerializeField] private TextMeshProUGUI foodEXPText;
    [SerializeField] private TextMeshProUGUI foodStatusText;
    [SerializeField] private Image lockedOverlayImage;

    [Header("Background Setting")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color selectedBackgroundColor;
    [SerializeField] private Color normalBackgroundColor;
    [SerializeField] private Color lockedBackgroundColor;

    [Header("Frame Setting")]
    [SerializeField] private Image frameImage;
    [SerializeField] private Color selectedFrameColor;
    [SerializeField] private Color normalFrameColor;
    [SerializeField] private Color lockedFrameColor;



    private bool isLocked = false;
    private FoodSO foodSO;
    public FoodSO FoodSO { get => foodSO; }
    public Button FoodButton { get => foodButton; }
    public bool IsLocked { get => isLocked; set => isLocked = value; }

    public void SetMenuFoodItem(FoodSO item, bool isSelected)
    {
        this.foodSO = item;
        foodImage.sprite = item.Sprite;
        foodNameText.text = item.recipeName;
        foodPriceText.text = $"${item.price}";
        foodEXPText.text = $"{item.exp} XP";
        infoButton.onClick.AddListener(() =>
        {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIFoodDetailPopup,
                new UIFoodDetailPopup.Param { foodSO = item });
        });
        if (!GameManager.Instance.GameData.MenuData.unlockedDishes.Contains(item))
        {
            isLocked = true;
            SetLockState(isLocked);
        }
        else
        {
            isLocked = false;
            SetLockState(isLocked);
            SetSelectedState(isSelected);

        }
    }

    private void SetLockState(bool isLocked)
    {
        backgroundImage.color = isLocked ? lockedBackgroundColor : normalBackgroundColor;
        frameImage.color = isLocked ? lockedFrameColor : normalFrameColor;
        if(GameManager.Instance.GameData.RestaurantData.Level < foodSO.unlockLevel)
        {
            foodStatusText.text = isLocked ? $"Unlocked at Level {foodSO.unlockLevel}" : "Add to Menu";
        }
        else
        {
            foodStatusText.text = isLocked ? $"Buy for {foodSO.unlockPrice}" : "Add to Menu";
        }
        foodButton.interactable = !isLocked;
        lockedOverlayImage.gameObject.SetActive(isLocked);
    }

    public void SetSelectedState(bool isSelected)
    {
        foodStatusText.text = isSelected ? "In Menu" : "Add to Menu";
        backgroundImage.color = isSelected ? selectedBackgroundColor : normalBackgroundColor;
        frameImage.color = isSelected ? selectedFrameColor : normalFrameColor;
    }
    public void OnUnlockClick()
    {
        if(GameManager.Instance.GameData.RestaurantData.Level < foodSO.unlockLevel)
        {
            UIManager.Instance.ShowAlertMessage($"Reach Level {foodSO.unlockLevel} to unlock this dish.");
            return;
        }
        if (GameManager.Instance.GameData.RestaurantData.Coins >= foodSO.unlockPrice)
        {
            GameManager.Instance.UpdateRestaurantCoinServerRpc(-foodSO.unlockPrice);
            GameManager.Instance.UnlockDishServerRpc(foodSO.Guid);
            SetLockState(false);
        }
        else
        {
            UIManager.Instance.ShowAlertMessage("Not enough coins to unlock this dish.");
        }
    }
}