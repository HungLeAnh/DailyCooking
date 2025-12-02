using System;
using System.Collections.Generic;

public class MenuData
{

    [System.NonSerialized]
    public Action OnMenuDataChanged;

    [System.NonSerialized]
    public List<FoodSO> unlockedDishes = new List<FoodSO>();

    public List<FoodSO> menuDished = new List<FoodSO>();

    public List<string> data = new List<string>();
    public bool AddDishToMenu(FoodSO dish)
    {
        if (!unlockedDishes.Contains(dish))
        {
            unlockedDishes.Add(dish);
            data.Add(dish.Guid);
            OnMenuDataChanged?.Invoke();
            return true;
        }
        return false;
    }    
    public void RemoveDishFromMenu(FoodSO dish)
    {
        if (unlockedDishes.Contains(dish))
        {
            unlockedDishes.Remove(dish);
            data.Remove(dish.Guid);
            OnMenuDataChanged?.Invoke();
        }
    }
    public void LoadMenuData()
    {
        unlockedDishes.Clear();
        foreach (var guid in data)
        {
            var dish = ConfigManager.Instance.ConfigFood.FoodItems.Find(x => x.Guid == guid);
            if (dish != null)
            {
                unlockedDishes.Add(dish);
            }
        }
    }
}

