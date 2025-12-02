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
    public void RemoveDishFromMenu(FoodSO dish)
    {
        MenuData.RemoveDishFromMenu(dish);
    }
}

