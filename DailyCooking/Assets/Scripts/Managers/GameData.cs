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
    public UpgradeData UpgradeData { get; private set; } = new UpgradeData();
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
    public bool RemoveDishFromMenu(FoodSO dish)
    {
        return MenuData.RemoveDishFromMenu(dish);
    }

    public bool PurchaseUpgrade(UpgradeSO upgradeData)
    {
        return UpgradeData.PurchaseUpgrade(upgradeData);
    }
}

[System.Serializable]
public class UpgradeData
{

    [System.NonSerialized]
    public Action OnMenuDataChanged;

    private List<string> purchasedUpgrades = new List<string>();

    public List<string> PurchasedUpgrades => purchasedUpgrades;

    public bool PurchaseUpgrade(UpgradeSO upgradeData)
    {
        if (!purchasedUpgrades.Contains(upgradeData.Guid))
        {
            purchasedUpgrades.Add(upgradeData.Guid);
            OnMenuDataChanged?.Invoke();
            return true;
        }
        return false;
    }
}

