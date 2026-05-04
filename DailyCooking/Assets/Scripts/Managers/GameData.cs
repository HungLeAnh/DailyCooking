using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public List<PlayerStats> PlayersStats { get; private set; } = new List<PlayerStats>();
    public RestaurantData RestaurantData { get; private set; } = new RestaurantData();
    public InventoryData InventoryData { get; private set; } = new InventoryData();
    public GridData GridData { get; private set; } = new GridData();
    public TutorialData TutorialData { get; private set; } = new TutorialData();
    public MenuData MenuData { get; private set; } = new MenuData();
    public UpgradeData UpgradeData { get; private set; } = new UpgradeData();
    public ShopData ShopData { get; private set; } = new ShopData();
    public void UpdateGridData(GridXZ<GridObject> grid)
    {
        GridData.UpdateGridData(grid);
    }   
    public void AddInventoryData(string guid)
    {
        InventoryData.Add(guid);
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
    public bool IsUpgradePurchased(UpgradeSO upgradeData)
    {
        return UpgradeData.PurchasedUpgrades.Contains(upgradeData.Guid);
    }
    public void IncreaseShopDailyFreeWatchCount(ShopItemType type)
    {
        switch (type)
        {
            case ShopItemType.Coin:
                ShopData.UpdateDailyFreeItemCoinCount();
                break;
            case ShopItemType.Gem:
                ShopData.UpdateDailyFreeItemGemCount();
                break;
            default:
                break;
        }
    }
    public int GetShopDailyFreeWatchCount(ShopItemType id)
    {
        switch (id)
        {
            case ShopItemType.Gem:
                return ShopData.DailyFreeItemGemCount;
            case ShopItemType.Coin:
                return ShopData.DailyFreeItemCoinCount;

            default:
                return -1;

        }
    }
    public PlayerStats GetPlayerStatsById(string id)
    {
        //Debug.Log($"Getting player stats for player {id}");
        return PlayersStats.Find(player => player.PlayerId == id);
    }
    public void TryAddPlayerStats(string playerId)
    {
        if (GetPlayerStatsById(playerId) != null) return;
        //Debug.Log($"Adding player stats for player {playerId}");
        var playerStats = new PlayerStats(playerId);
        playerStats.OnResourceChange += () => GameManager.Instance.SaveGame();
        PlayersStats.Add(playerStats);
        GameManager.Instance.SaveGame();
    }
}

