using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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

    private readonly List<UIFoodItem> foodItemList = new List<UIFoodItem>();
    private readonly List<UIMenuCategoryItem> categoryItemList = new List<UIMenuCategoryItem>();
    private readonly List<UIMenuItem> menuItemList = new List<UIMenuItem>();
    private List<FoodSO> emptyFoodList = new List<FoodSO>();
    private Dictionary<FoodType, List<FoodSO>> foodItemsByType;
    private UIMenuCategoryItem allCategoryItem;
    private bool initialized;

    public Button BtnClose { get => btnClose; }
    public void Awake()
    {
        btnClose.onClick.AddListener(OnCloseClick);
    }
    private void Start()
    {
        // ShowPopup already initializes the freshly created popup; don't rebuild it again
        // in the same frame (that was the old double-build on first open).
        if (!initialized)
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
        initialized = true;
        BuildCategoriesIfNeeded();
        RefreshMenuList();

        // Always start on the All tab, exactly like the previous rebuild-every-open version.
        if (allCategoryItem != null)
        {
            allCategoryItem.SetSelected(true);
            allCategoryItem.CategoryButton.onClick.Invoke();
        }
    }

    // Categories are fixed by the FoodType enum: build the row once and reuse it.
    private void BuildCategoriesIfNeeded()
    {
        if (categoryItemList.Count > 0)
            return;

        foreach (FoodType type in Enum.GetValues(typeof(FoodType)))
        {
            if (type == FoodType.None) continue;

            GameObject menuCategory = Instantiate(categoryItemPrefab, categoryListContainer);
            var item = menuCategory.GetComponent<UIMenuCategoryItem>();
            item.SetCategory(type, this);
            FoodType capturedType = type;
            item.CategoryButton.onClick.AddListener(() =>
            {
                ShowMenuOfType(capturedType);
                OnCategoryItemClick?.Invoke(capturedType);
            });
            categoryItemList.Add(item);
            if (type == FoodType.All)
                allCategoryItem = item;
        }
    }

    // Dishes in the menu change while the popup is open: grow the pool and re-bind every
    // card, hiding the leftovers instead of destroying them (no Instantiate per open).
    private void RefreshMenuList()
    {
        List<FoodSO> dishes = GameManager.Instance.GameData.MenuData.menuDished;
        for (int i = 0; i < dishes.Count; i++)
        {
            while (menuItemList.Count <= i)
                menuItemList.Add(CreateMenuItem());

            UIMenuItem item = menuItemList[i];
            FoodSO dish = dishes[i];
            item.gameObject.SetActive(true);
            item.SetMenuFoodItem(dish);
            item.ButtonRemove.onClick.AddListener(() => OnRemoveDishClicked(dish, item));
        }

        for (int i = dishes.Count; i < menuItemList.Count; i++)
            menuItemList[i].gameObject.SetActive(false);

        totalDish.text = $"{dishes.Count}";
    }

    private UIMenuItem CreateMenuItem()
    {
        GameObject menuCategory = Instantiate(menuItemPrefab, menuListContainer);
        var item = menuCategory.GetComponent<UIMenuItem>();
        item.gameObject.SetActive(true);
        return item;
    }

    private void OnRemoveDishClicked(FoodSO dish, UIMenuItem item)
    {
        bool canRemoved = GameManager.Instance.GameData.MenuData.menuDished.Contains(dish);
        if (canRemoved)
        {
            GameManager.Instance.RemoveDishFromMenuServerRpc(dish.Guid);
            // The dish's card is not in the list while another category is shown.
            foodItemList.Find(x => x.FoodSO == dish)?.SetSelectedState(false);
            item.gameObject.SetActive(false);
            totalDish.text = $"{GameManager.Instance.GameData.MenuData.menuDished.Count}";
        }
    }

    private void ShowMenuOfType(FoodType type)
    {
        List<FoodSO> menuItems = GetFoodItems(type);
        for (int i = 0; i < menuItems.Count; i++)
        {
            UIFoodItem uifooditem = GetOrCreateFoodItem(i);
            FoodSO item = menuItems[i];
            uifooditem.gameObject.SetActive(true);
            uifooditem.SetMenuFoodItem(item,
                GameManager.Instance.GameData.MenuData.menuDished.Contains(item));
            uifooditem.FoodButton.onClick.AddListener(() => OnAddDishClicked(item, uifooditem));
        }

        for (int i = menuItems.Count; i < foodItemList.Count; i++)
            foodItemList[i].gameObject.SetActive(false);
    }

    private UIFoodItem GetOrCreateFoodItem(int index)
    {
        while (foodItemList.Count <= index)
        {
            GameObject fooditem = Instantiate(foodItemPrefab, foodGridContainer);
            fooditem.gameObject.SetActive(true);
            foodItemList.Add(fooditem.GetComponent<UIFoodItem>());
        }
        return foodItemList[index];
    }

    private void OnAddDishClicked(FoodSO item, UIFoodItem uifooditem)
    {
        if (menuItemList.Exists(x => x.gameObject.activeSelf && x.FoodSO == item))
            return;

        bool canAdded = GameManager.Instance.GameData.MenuData.menuDished.Contains(item) == false;
        if (canAdded)
        {
            GameManager.Instance.AddDishToMenuServerRpc(item.Guid);
            AddMenuItemCard(item);
            totalDish.text = $"{GameManager.Instance.GameData.MenuData.menuDished.Count}";
            uifooditem.SetSelectedState(true);
        }
    }

    // Adds one card immediately (the server may not have applied the RPC yet on clients).
    private void AddMenuItemCard(FoodSO dish)
    {
        int index = 0;
        while (index < menuItemList.Count && menuItemList[index].gameObject.activeSelf)
            index++;

        UIMenuItem item;
        if (index < menuItemList.Count)
            item = menuItemList[index];
        else
        {
            item = CreateMenuItem();
            menuItemList.Add(item);
        }

        item.gameObject.SetActive(true);
        item.SetMenuFoodItem(dish);
        item.ButtonRemove.onClick.AddListener(() => OnRemoveDishClicked(dish, item));
    }

    // FoodSO lists per category are fixed config data: cache them once instead of filtering
    // with LINQ on every category click.
    private List<FoodSO> GetFoodItems(FoodType type)
    {
        if (foodItemsByType == null)
        {
            foodItemsByType = new Dictionary<FoodType, List<FoodSO>>();
            foreach (FoodSO food in ConfigManager.Instance.ConfigFood.FoodItems)
            {
                if (!foodItemsByType.TryGetValue(food.foodType, out List<FoodSO> list))
                {
                    list = new List<FoodSO>();
                    foodItemsByType.Add(food.foodType, list);
                }
                list.Add(food);
            }
        }

        if (type == FoodType.All)
            return ConfigManager.Instance.ConfigFood.FoodItems;

        return foodItemsByType.TryGetValue(type, out List<FoodSO> items) ? items : emptyFoodList;
    }

    public void OnCloseClick()
    {
        UIPopupManager.Instance.HidePopup(UIPopupType.UIMenuPopup);
    }
    public UIFoodItem GetFirstUnlockedFoodItem()
    {
        foreach (var item in foodItemList)
        {
            if (item.gameObject.activeSelf && !item.IsLocked)
                return item;
        }
        return null;
    }
}
