using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIFoodDetailPopup : UIPopup
{
    public class Param
    {
        public FoodSO foodSO;
    }

    [SerializeField] private Button closeButton;
    [SerializeField] private Image foodImage;
    [SerializeField] private TextMeshProUGUI foodNameText;
    [SerializeField] private TextMeshProUGUI foodTypeText;
    [SerializeField] private TextMeshProUGUI foodPriceText;
    
    private FoodSO currentFoodSO;


    private void Start()
    {
        closeButton.onClick.AddListener(OnCloseClick);
    }
    public override void ShowPopup(object param)
    {
        base.ShowPopup(param);
        Param inputParam = param as Param;
        if (inputParam != null && inputParam.foodSO != null)
            SetupFoodDetail(inputParam.foodSO);
    }
    public void SetupFoodDetail(FoodSO foodSO)
    {
        currentFoodSO = foodSO;
        foodImage.sprite = foodSO.Sprite;
        foodNameText.text = foodSO.recipeName;
        foodTypeText.text = foodSO.foodType.ToString();
    }
    public void OnCloseClick()
    {
        UIPopupManager.Instance.HidePopup(UIPopupType.UIFoodDetailPopup);
    }
}
