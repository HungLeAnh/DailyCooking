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
    public void AddDishToMenu(FoodSO dish)
    {
        MenuData.AddDishToMenu(dish);
    }
}
public class MenuData
{
    [System.NonSerialized]
    public Action OnMenuDataChanged;
    public List<FoodSO> UnlockedDishes { get; private set; } = new List<FoodSO>();

    public void AddDishToMenu(FoodSO dish)
    {
        if (!UnlockedDishes.Contains(dish))
        {
            UnlockedDishes.Add(dish);
            OnMenuDataChanged?.Invoke();
        }
    }
}

