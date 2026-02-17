using System;

public class UpgradeManager : PersistentSingleton<UpgradeManager>
{
    public event Action<UpgradeSO> OnUpgradePurchased;
    public bool PurchaseUpgrade(UpgradeSO upgrade)
    {
        if(GameManager.Instance.GameData.PlayerStats.playerData.Coins >= upgrade.UpgradeCosts)
        {
            if (GameManager.Instance.GameData.PurchaseUpgrade(upgrade))
            {
                GameManager.Instance.GameData.PlayerStats.UpdatePlayerCoins(-upgrade.UpgradeCosts);
                GetUpgradeReward(upgrade.UpgradeTarget, upgrade.UpgradeValue);
                OnUpgradePurchased?.Invoke(upgrade);
                return true;
            }
            return false;
        }
        else
        {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameNotiPopup,
                  new UIGameNotiPopup.Param
                  {
                      Title = "warning",
                      Message = "Not enough money to buy this item."
                  });
            return false;
        }
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