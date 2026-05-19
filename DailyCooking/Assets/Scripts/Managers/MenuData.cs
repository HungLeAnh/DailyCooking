using System;
using System.Collections.Generic;

public class MenuData
{

    [System.NonSerialized]
    public Action OnMenuDataChanged;

    [System.NonSerialized]
    public List<FoodSO> unlockedDishes = new List<FoodSO>();

    [System.NonSerialized]
    public List<FoodSO> menuDished = new List<FoodSO>();

    public List<string> menu = new List<string>();
    public List<string> unlocked = new List<string>();
    public bool AddDishToMenu(FoodSO dish)
    {
        if (!menuDished.Contains(dish))
        {
            menuDished.Add(dish);
            menu.Add(dish.Guid);
            OnMenuDataChanged?.Invoke();
            return true;
        }
        return false;
    }    
    public bool RemoveDishFromMenu(FoodSO dish)
    {
        if (menuDished.Contains(dish))
        {
            menuDished.Remove(dish);
            menu.Remove(dish.Guid);
            OnMenuDataChanged?.Invoke();
            return true;
        }
        return false;
    }
    public bool AddUnlockedDish(FoodSO dish)
    {
        if (!unlockedDishes.Contains(dish))
        {
            unlockedDishes.Add(dish);
            unlocked.Add(dish.Guid); 
            OnMenuDataChanged?.Invoke();
            return true;
        }
        return false;
    }
    public void LoadMenuData()
    {
        unlockedDishes.Clear();
        menuDished.Clear();

        foreach (var guid in menu)
        {
            var dish = ConfigManager.Instance.ConfigFood.FoodItems.Find(x => x.Guid == guid);
            if (dish != null)
            {
                menuDished.Add(dish);
            }
        }
        foreach (var guid in unlocked)
        {
            var dish = ConfigManager.Instance.ConfigFood.FoodItems.Find(x => x.Guid == guid);
            if (dish != null)
            {
                unlockedDishes.Add(dish);
            }
        }
    }
}

