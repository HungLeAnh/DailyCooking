using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuPopup : UIPopup
{
    public Action<FoodType> OnCategoryItemClick;

    [SerializeField]private Button btnClose;
    [SerializeField] private TextMeshProUGUI totalDish;
    [SerializeField] private Transform menuListContainer;
    [SerializeField] private GameObject menuItemPrefab;

    [SerializeField] private Transform categoryListContainer;
    [SerializeField] private GameObject categoryItemPrefab;

    [SerializeField] private Transform foodGridContainer;
    [SerializeField] private GameObject foodItemPrefab;

    private List<UIFoodItem> foodItemList= new List<UIFoodItem>();

    public Button BtnClose { get => btnClose; }
    public void Awake()
    {
        btnClose.onClick.AddListener(OnCloseClick);
    }
    private void Start()
    {
        Initialize();

    }
    public override void SetupPopup()
    {
        base.SetupPopup();
    }
    public override void HidePopup(object param)
    {
        base.HidePopup(param);

    }
    public override void ShowPopup(object param)
    {
        base.ShowPopup(param);
        Initialize();


    }
    public void Initialize()
    {
        foreach (Transform child in categoryListContainer)
        {
            Destroy(child.gameObject);
        }
        foodItemList.Clear();

        foreach (FoodType type in Enum.GetValues(typeof(FoodType)))
        {
            if (type == FoodType.None) continue;

            GameObject menuCategory = Instantiate(categoryItemPrefab, categoryListContainer);
            var item = menuCategory.GetComponent<UIMenuCategoryItem>();
            item.SetCategory(type,this);
            item.CategoryButton.onClick.AddListener(() =>
            {
                ShowMenuOfType(type);
                OnCategoryItemClick?.Invoke(type);
            });
            if (type == FoodType.All)
            {
                item.SetSelected(true);
                item.CategoryButton.onClick.Invoke();
            }
        }

        totalDish.text = $"{GameManager.Instance.GameData.MenuData.menuDished.Count}";

        foreach (Transform child in menuListContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (var dish in GameManager.Instance.GameData.MenuData.menuDished)
        {
            CreateMenuItem(dish);
        }
    }

    private void ShowMenuOfType(FoodType type)
    {
        foreach (Transform child in foodGridContainer)
        {
            Destroy(child.gameObject);
        }
        foodItemList.Clear();
        List<FoodSO> menuItems = new List<FoodSO>();

        if (type == FoodType.All)
        {
            menuItems = ConfigManager.Instance.ConfigFood.FoodItems;
        }
        else
        {
            menuItems = ConfigManager.Instance.ConfigFood.FoodItems
                .Where(item => item.foodType == type)
                .ToList();
        }

        CreateMenuFoodItem(menuItems);
    }
    private void CreateMenuFoodItem(List<FoodSO> menuItems)
    {
        foreach (var item in menuItems)
        {
            GameObject fooditem = Instantiate(foodItemPrefab, foodGridContainer);
            fooditem.gameObject.SetActive(true);
            UIFoodItem uifooditem = fooditem.GetComponent<UIFoodItem>();
            foodItemList.Add(uifooditem);

            uifooditem.SetMenuFoodItem(item, 
                GameManager.Instance.GameData.MenuData.menuDished.Contains(item));
            uifooditem.FoodButton.onClick.AddListener(() =>
            {
                bool canAdded = GameManager.Instance.GameData.MenuData.menuDished.Contains(item) == false;
                if (canAdded)
                {
                    int index = ConfigManager.Instance.ConfigFood.FoodItems.IndexOf(item);
                    GameManager.Instance.AddDishToMenuServerRpc(index);
                    CreateMenuItem(item);
                    totalDish.text = $"{GameManager.Instance.GameData.MenuData.menuDished.Count}";
                    uifooditem.SetSelectedState(true);
                }
            });
        }
    }    
    private void CreateMenuItem(FoodSO dish)
    {
        GameObject menuCategory = Instantiate(menuItemPrefab, menuListContainer);
        var item = menuCategory.GetComponent<UIMenuItem>();
        item.SetMenuFoodItem(dish);
        item.ButtonRemove.onClick.AddListener(() =>
        {
            bool canRemoved = GameManager.Instance.GameData.MenuData.menuDished.Contains(dish);
            if (canRemoved)
            {
                int index = ConfigManager.Instance.ConfigFood.FoodItems.IndexOf(dish);
                GameManager.Instance.RemoveDishFromMenuServerRpc(index);
                foodItemList.Find(x => x.FoodSO == dish ).SetSelectedState(false);
                Destroy(menuCategory);
                totalDish.text = $"{GameManager.Instance.GameData.MenuData.menuDished.Count}";
            }
        });
    }
    public void OnCloseClick()
    {
        UIPopupManager.Instance.HidePopup(UIPopupType.UIMenuPopup);
    }
    public UIFoodItem GetFirstUnlockedFoodItem()
    {
        if(foodItemList.Count > 0)
        {
            foreach (var item in foodItemList)
            {
                if (!item.IsLocked)
                    return item;
            }
        }
        return null;
    }
}
