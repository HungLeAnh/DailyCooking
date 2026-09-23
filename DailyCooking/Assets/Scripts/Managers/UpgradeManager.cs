// Client-side front for upgrades: the server checks the level, price and ownership again,
// charges the coins and applies the reward (GameManager.Economy.PurchaseUpgradeServerRpc).
public class UpgradeManager : PersistentSingleton<UpgradeManager>
{
    // Returns false when the purchase was refused locally; the UI updates from UpgradeData once
    // the server confirms.
    public bool PurchaseUpgrade(UpgradeSO upgrade)
    {
        GameData gameData = GameManager.Instance.GameData;
        if (gameData.IsUpgradePurchased(upgrade))
            return false;
        if (gameData.RestaurantData.Level < upgrade.LevelUnlocked)
        {
            UIManager.Instance.ShowAlertMessage($"Reach Level {upgrade.LevelUnlocked} to unlock this upgrade.");
            return false;
        }
        if (gameData.RestaurantData.Coins < upgrade.UpgradeCosts)
        {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameNotiPopup,
                  new UIGameNotiPopup.Param
                  {
                      Title = "warning",
                      Message = "Not enough money to buy this item."
                  });
            return false;
        }
        GameManager.Instance.PurchaseUpgradeServerRpc(upgrade.Guid);
        return true;
    }
}
