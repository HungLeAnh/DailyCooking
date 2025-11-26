using System;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    public PlayerStats PlayerStats { get; private set; } = new PlayerStats();
    public InventoryData InventoryData { get; private set; } = new InventoryData();
    public GridData GridData { get; private set; } = new GridData();
    public TutorialData TutorialData { get; private set; } = new TutorialData();
    public MenuData MenuData { get; private set; } = new MenuData();

    public void UpdateGridData(GridXZ<GridObject> grid)
    {
        GridData.UpdateGridData(grid);
    }
    public void AddInventoryData(InventoryItemData item)
    {
        InventoryData.Add(item);
    }    
    public void AddInventoryData(string guid)
    {
        InventoryData.Add(guid);
    }
    public void RemoveInventoryData(InventoryItemData item)
    {
        InventoryData.Remove(item);
    }    
    public void RemoveInventoryData(string id)
    {
        InventoryData.Remove(id);
    }
    public bool AddDishToMenu(FoodSO dish)
    {
        return MenuData.AddDishToMenu(dish);
    }
}
public class MenuData
{

    [System.NonSerialized]
    public Action OnMenuDataChanged;

    [System.NonSerialized]
    public List<FoodSO> unlockedDishes = new List<FoodSO>();

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

