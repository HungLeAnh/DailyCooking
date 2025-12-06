using System.Collections.Generic;
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
    [SerializeField] private TextMeshProUGUI foodEXPText;

    [SerializeField] private GameObject ingredientTagPrefab;
    [SerializeField] private Transform container;

    private FoodSO currentFoodSO;
    private List<Transform> ingredientTransformList = new List<Transform>();
    void Start()
    {
        ingredientTagPrefab.SetActive(false);
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
        foodPriceText.text = $"${foodSO.price}";
        foodEXPText.text = $"{foodSO.exp} XP";
        GenerateTags(foodSO.kitchenObjectSOList);
    }
    public void GenerateTags(List<KitchenObjectSO> ingredients)
    {
        foreach (Transform child in ingredientTransformList)
        {
            Destroy(child.gameObject);
        }
        ingredientTransformList.Clear();
        foreach (var ingredient in ingredients)
        {
            GameObject newTag = Instantiate(ingredientTagPrefab, container);
            newTag.SetActive(true);
            TextMeshProUGUI tagText = newTag.GetComponentInChildren<TextMeshProUGUI>();
            if (tagText != null)
            {
                tagText.text = ingredient.objectName;
            }
            ingredientTransformList.Add(newTag.transform);
        }

    }
    public void OnCloseClick()
    {
        UIPopupManager.Instance.HidePopup(UIPopupType.UIFoodDetailPopup);
    }
}
