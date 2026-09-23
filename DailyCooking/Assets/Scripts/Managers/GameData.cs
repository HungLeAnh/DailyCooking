using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public const int CURRENT_SAVE_VERSION = 2;

    [System.NonSerialized]
    public Action<PlayerStats> OnPlayerStatsAdded;

    // Saves written before versioning have no SaveVersion field and load as 1.
    public int SaveVersion { get; set; } = 1;
    public List<PlayerStats> PlayersStats { get; private set; } = new List<PlayerStats>();
    public RestaurantData RestaurantData { get; private set; } = new RestaurantData();
    public InventoryData InventoryData { get; private set; } = new InventoryData();
    public GridData GridData { get; private set; } = new GridData();
    public TutorialData TutorialData { get; private set; } = new TutorialData();
    public MenuData MenuData { get; private set; } = new MenuData();
    public UpgradeData UpgradeData { get; private set; } = new UpgradeData();
    public ShopData ShopData { get; private set; } = new ShopData();
    public PostBoxData PostBoxData { get; private set; } = new PostBoxData();
    public CosmeticData CosmeticData { get; private set; } = new CosmeticData();

    public static GameData CreateNew()
    {
        return new GameData { SaveVersion = CURRENT_SAVE_VERSION };
    }

    // Upgrades data loaded from an older save to the current layout.
    public void Migrate()
    {
        if (SaveVersion < 2)
        {
            // v1 saves could keep a grid array smaller than the grid size after an expansion.
            GridData.EnsureArraySize();
        }
        SaveVersion = CURRENT_SAVE_VERSION;
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
    public void UnlockDish(string guid)
    {
        var dish = ConfigManager.Instance.ConfigFood.FoodItems.Find(x => x.Guid == guid);
        if (dish != null)
        {
            MenuData.AddUnlockedDish(dish);
        }
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
        PlayersStats.Add(playerStats);
        OnPlayerStatsAdded?.Invoke(playerStats);
    }

}
