using System;

public class UpgradeManager : PersistentSingleton<UpgradeManager>
{
    public event Action<UpgradeSO> OnUpgradePurchased;
    public void PurchaseUpgrade(UpgradeSO upgrade)
    {
       
            GameManager.Instance.GameData.PlayerStats.UpdatePlayerCoins(-upgrade.UpgradeCosts);
            GetUpgradeReward(upgrade.UpgradeTarget, upgrade.UpgradeValue);
            OnUpgradePurchased?.Invoke(upgrade);
        // if (GameManager.Instance.GameData.PurchaseUpgrade(upgrade))
        //{}
    }
    public void GetUpgradeReward(UpgradeTarget upgradeTarget,float amount)
    {
        switch (upgradeTarget)
        {
            case UpgradeTarget.MoveSpeed:
                GameManager.Instance.GameData.PlayerStats.UpdatePlayerMoveSpeed(amount);
                break;
            case UpgradeTarget.CookingSpeed:
                GameManager.Instance.GameData.PlayerStats.UpdatePlayerCookingSpeed(amount);
                break;
            case UpgradeTarget.CarryingCapacity:
                GameManager.Instance.GameData.PlayerStats.UpdatePlayerCarryingCapacity(amount);
                break;
            case UpgradeTarget.TipIncrease:
                GameManager.Instance.GameData.PlayerStats.UpdatePlayerTipIncrease(amount);
                break;
            case UpgradeTarget.ExpansionRestaurant:
                GridBuildingSystem.Instance.ExpandGrid(amount);
                break;
        }
    }
}