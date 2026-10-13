using Unity.Netcode;

// Restaurant economy. Clients only send intents ("buy shop item 7"); the server looks prices
// up in config, checks the balance, applies the change to its own GameData (the save) and
// mirrors it to the clients with SendTo.NotServer RPCs.
public partial class GameManager
{
    #region Balances (server only)

    public void ServerAddCoins(int amount)
    {
        if (!IsServer || GameData == null || amount <= 0) return;
        ApplyCoins(amount);
    }

    public void ServerAddGems(int amount)
    {
        if (!IsServer || GameData == null || amount <= 0) return;
        ApplyGems(amount);
    }

    // Also pays the level-up reward, so it is granted exactly once, by the server.
    public void ServerAddExp(int amount)
    {
        if (!IsServer || GameData == null || amount <= 0) return;
        int levelBefore = GameData.RestaurantData.Level;
        GameData.RestaurantData.UpdateRestaurantExp(amount);
        UpdateRestaurantExpClientRpc(amount);
        int levelAfter = GameData.RestaurantData.Level;
        if (levelAfter > levelBefore)
            ApplyCoins(EconomyRules.GetLevelUpRewardCoins(levelAfter));
    }

    private bool TrySpendCoins(int cost)
    {
        if (cost < 0 || GameData.RestaurantData.Coins < cost) return false;
        if (cost > 0) ApplyCoins(-cost);
        return true;
    }

    private bool TrySpendGems(int cost)
    {
        if (cost < 0 || GameData.RestaurantData.Gems < cost) return false;
        if (cost > 0) ApplyGems(-cost);
        return true;
    }

    private void ApplyCoins(int delta)
    {
        GameData.RestaurantData.UpdateRestaurantCoins(delta);
        UpdateRestaurantCoinClientRpc(delta);
    }

    private void ApplyGems(int delta)
    {
        GameData.RestaurantData.UpdateRestaurantGems(delta);
        UpdateRestaurantGemsClientRpc(delta);
    }

    // Changes that happened before a joining client's snapshot arrived are already in it.
    [Rpc(SendTo.NotServer)]
    private void UpdateRestaurantCoinClientRpc(int delta)
    {
        GameData?.RestaurantData.UpdateRestaurantCoins(delta);
    }

    [Rpc(SendTo.NotServer)]
    private void UpdateRestaurantGemsClientRpc(int delta)
    {
        GameData?.RestaurantData.UpdateRestaurantGems(delta);
    }

    [Rpc(SendTo.NotServer)]
    private void UpdateRestaurantExpClientRpc(int amount)
    {
        GameData?.RestaurantData.UpdateRestaurantExp(amount);
    }

    #endregion

    #region Purchases

    [Rpc(SendTo.Server)]
    public void BuyShopItemServerRpc(int shopItemId, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (GameData == null || ConfigManager.Instance == null) return;
        // List.Find -> O(1) dictionary lookup (ConfigShop.GetById caches by id).
        ConfigShopItem item = ConfigManager.Instance.ConfigShop.GetById(shopItemId);
        if (item == null || (item.Type != ShopItemType.Item && item.Type != ShopItemType.Ingredient)) return;
        if (item.UnlockLevel > GameData.RestaurantData.Level)
        {
            AlertClient(sender, $"Reach Level {item.UnlockLevel} to buy this item.");
            return;
        }
        if (item.Type == ShopItemType.Ingredient && GridBuildingSystem.Instance?.PostBox == null)
            return;
        if (!TrySpendCoins(item.Price))
        {
            AlertClient(sender, "Not enough money to buy this item.");
            return;
        }

        foreach (ShopReward reward in item.Rewards)
        {
            if (item.Type == ShopItemType.Item)
            {
                for (int i = 0; i < reward.Amount; i++)
                    ServerAddInventory(reward.Guid);
            }
            else if (KitchenGameManager.Instance.GetKitchenObjectSOByGuid(reward.Guid) != null)
            {
                GridBuildingSystem.Instance.PostBox.AddPackage(reward.Guid);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void ExchangeCurrencyServerRpc(ShopItemType rewardType, int gemCost, int rewardAmount, RpcParams rpcParams = default)
    {
        if (GameData == null || ShopManager.Instance == null) return;
        if (ShopManager.Instance.FindExchangeOffer(rewardType, gemCost, rewardAmount) == null) return;
        if (!TrySpendGems(gemCost))
        {
            AlertClient(rpcParams.Receive.SenderClientId, "Not enough gems to buy this currency.");
            return;
        }
        if (rewardType == ShopItemType.Coin)
            ApplyCoins(rewardAmount);
        else if (rewardType == ShopItemType.Gem)
            ApplyGems(rewardAmount);
    }

    [Rpc(SendTo.Server)]
    public void UnlockDishServerRpc(string foodGuid, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        FoodSO dish = FindDish(foodGuid);
        if (GameData == null || dish == null || GameData.MenuData.unlockedDishes.Contains(dish)) return;
        if (GameData.RestaurantData.Level < dish.unlockLevel)
        {
            AlertClient(sender, $"Reach Level {dish.unlockLevel} to unlock this dish.");
            return;
        }
        if (!TrySpendCoins(dish.unlockPrice))
        {
            AlertClient(sender, "Not enough coins to unlock this dish.");
            return;
        }
        GameData.UnlockDish(foodGuid);
        UnlockDishClientRpc(foodGuid);
    }

    [Rpc(SendTo.NotServer)]
    private void UnlockDishClientRpc(string foodGuid)
    {
        GameData?.UnlockDish(foodGuid);
    }

    [Rpc(SendTo.Server)]
    public void PurchaseUpgradeServerRpc(string upgradeGuid, RpcParams rpcParams = default)
    {
        ulong sender = rpcParams.Receive.SenderClientId;
        if (GameData == null || string.IsNullOrEmpty(upgradeGuid)) return;
        UpgradeSO upgrade = ConfigManager.Instance?.ConfigUpgrade?.Upgrades?.Find(x => x.Guid == upgradeGuid);
        if (upgrade == null || GameData.IsUpgradePurchased(upgrade)) return;
        if (GameData.RestaurantData.Level < upgrade.LevelUnlocked)
        {
            AlertClient(sender, $"Reach Level {upgrade.LevelUnlocked} to unlock this upgrade.");
            return;
        }

        string playerId = GetPlayerIdForClient(sender);
        PlayerStats buyerStats = string.IsNullOrEmpty(playerId) ? null : GameData.GetPlayerStatsById(playerId);
        if (upgrade.UpgradeTarget != UpgradeTarget.ExpansionRestaurant && buyerStats == null) return;

        if (!TrySpendCoins(upgrade.UpgradeCosts))
        {
            AlertClient(sender, "Not enough money to buy this item.");
            return;
        }
        GameData.PurchaseUpgrade(upgrade);
        PurchaseUpgradeClientRpc(upgradeGuid);

        if (upgrade.UpgradeTarget == UpgradeTarget.ExpansionRestaurant)
        {
            GridBuildingSystem.Instance.ExpandGrid(upgrade.UpgradeValue);
        }
        else
        {
            // Skill upgrades belong to the player who bought them.
            ApplyUpgradeToStats(buyerStats, upgrade.UpgradeTarget, upgrade.UpgradeValue);
            ApplyUpgradeToStatsClientRpc(playerId, upgrade.UpgradeTarget, upgrade.UpgradeValue);
        }
    }

    [Rpc(SendTo.NotServer)]
    private void PurchaseUpgradeClientRpc(string upgradeGuid)
    {
        UpgradeSO upgrade = ConfigManager.Instance.ConfigUpgrade.Upgrades.Find(x => x.Guid == upgradeGuid);
        if (upgrade != null)
            GameData?.PurchaseUpgrade(upgrade);
    }

    [Rpc(SendTo.NotServer)]
    private void ApplyUpgradeToStatsClientRpc(string playerId, UpgradeTarget upgradeTarget, float amount)
    {
        PlayerStats stats = GameData?.GetPlayerStatsById(playerId);
        if (stats != null)
            ApplyUpgradeToStats(stats, upgradeTarget, amount);
    }

    private static void ApplyUpgradeToStats(PlayerStats stats, UpgradeTarget upgradeTarget, float amount)
    {
        switch (upgradeTarget)
        {
            case UpgradeTarget.MoveSpeed:
                stats.UpdatePlayerMoveSpeed(amount);
                break;
            case UpgradeTarget.CookingSpeed:
                stats.UpdatePlayerCookingSpeed(amount);
                break;
            case UpgradeTarget.CarryingCapacity:
                stats.UpdatePlayerCarryingCapacity(amount);
                break;
            case UpgradeTarget.TipIncrease:
                stats.UpdatePlayerTipIncrease(amount);
                break;
        }
    }

    [Rpc(SendTo.Server)]
    public void UnlockCosmeticServerRpc(string cosmeticType, int index, RpcParams rpcParams = default)
    {
        if (GameData == null || ConfigManager.Instance == null) return;
        CosmeticsData cosmeticsData = ConfigManager.Instance.CustomizationData.CosmeticDatas.Find(x => x.Type == cosmeticType);
        if (cosmeticsData == null || index < 0 || index >= cosmeticsData.Cosmetics.Count) return;
        if (GameData.CosmeticData.IsCosmeticUnlocked(cosmeticType, index)) return;
        if (!TrySpendCoins(cosmeticsData.Cosmetics[index].Price))
        {
            AlertClient(rpcParams.Receive.SenderClientId, "Not enough coins to unlock this cosmetic.");
            return;
        }
        GameData.CosmeticData.UnlockCosmetic(cosmeticType, index);
        UnlockCosmeticClientRpc(cosmeticType, index);
    }

    [Rpc(SendTo.NotServer)]
    private void UnlockCosmeticClientRpc(string cosmeticType, int index)
    {
        GameData?.CosmeticData.UnlockCosmetic(cosmeticType, index);
    }

    #endregion

    #region Rewards

    // The rewarded ad itself is watched on the player's device and cannot be verified here;
    // the server enforces the daily count (by the host's clock) and the reward amount.
    [Rpc(SendTo.Server)]
    public void ClaimDailyFreeServerRpc(ShopItemType rewardType, RpcParams rpcParams = default)
    {
        if (GameData == null || ShopManager.Instance == null) return;
        DailyFreeCurrency offer = ShopManager.Instance.DailyFreeCurrency.Find(x => x.Id == rewardType);
        if (offer == null || !int.TryParse(offer.Reward, out int amount) || amount <= 0) return;

        GameData.ShopData.RefreshDailyShopOffer();
        if (GameData.GetShopDailyFreeWatchCount(rewardType) >= offer.Count)
        {
            AlertClient(rpcParams.Receive.SenderClientId, "No free rewards left today.");
            return;
        }
        GameData.IncreaseShopDailyFreeWatchCount(rewardType);
        IncreaseDailyFreeCountClientRpc(rewardType);

        if (rewardType == ShopItemType.Coin)
            ApplyCoins(amount);
        else if (rewardType == ShopItemType.Gem)
            ApplyGems(amount);
    }

    [Rpc(SendTo.NotServer)]
    private void IncreaseDailyFreeCountClientRpc(ShopItemType rewardType)
    {
        if (GameData == null) return;
        GameData.ShopData.RefreshDailyShopOffer();
        GameData.IncreaseShopDailyFreeWatchCount(rewardType);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [Rpc(SendTo.Server)]
    public void CheatAddServerRpc(int coins, int exp)
    {
        ServerAddCoins(coins);
        ServerAddExp(exp);
    }
#endif

    #endregion
}
