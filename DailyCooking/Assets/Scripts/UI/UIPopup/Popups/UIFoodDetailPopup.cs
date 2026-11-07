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
    private void Start()
    {
        ingredientTagPrefab.SetActive(false);
        closeButton.onClick.AddListener(OnCloseClick);

    }
    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
    }
    public override void SetupPopup()
    {
        base.SetupPopup();
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
        GenerateTags(ResolveIngredients(foodSO));
    }

    // Drinks are served as one finished item, so their food list holds the drink itself.
    // Show the drink recipe's ingredients instead (Milk, Orange, ...).
    private List<KitchenObjectSO> ResolveIngredients(FoodSO foodSO)
    {
        List<KitchenObjectSO> listed = foodSO.kitchenObjectSOList;
        if (listed == null || listed.Count == 0)
            return listed;

        RecipeDatabaseSO database = KitchenGameManager.Instance != null
            ? KitchenGameManager.Instance.RecipeDatabase
            : null;
        if (database == null)
            return listed;

        foreach (KitchenObjectSO item in listed)
        {
            DrinkRecipeSO drinkRecipe = database.GetDrinkRecipeForOutput(item);
            if (drinkRecipe != null && drinkRecipe.input != null && drinkRecipe.input.Count > 0)
                return drinkRecipe.input;
        }
        return listed;
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
