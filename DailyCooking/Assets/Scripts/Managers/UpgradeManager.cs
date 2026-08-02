using System;

public class UpgradeManager : PersistentSingleton<UpgradeManager>
{
    public event Action<UpgradeSO> OnUpgradePurchased;
    public bool PurchaseUpgrade(UpgradeSO upgrade)
    {
        if(GameManager.Instance.GameData.RestaurantData.Coins >= upgrade.UpgradeCosts)
        {
            if (!GameManager.Instance.GameData.UpgradeData.PurchasedUpgrades.Contains(upgrade.Guid))
            {
                GameManager.Instance.PurchaseUpgradeServerRpc(upgrade.Guid);
                GameManager.Instance.UpdateRestaurantCoinServerRpc(-upgrade.UpgradeCosts);
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
                GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId).UpdatePlayerMoveSpeed(amount);
                break;
            case UpgradeTarget.CookingSpeed:
                GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId).UpdatePlayerCookingSpeed(amount);
                break;
            case UpgradeTarget.CarryingCapacity:
                GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId).UpdatePlayerCarryingCapacity(amount);
                break;
            case UpgradeTarget.TipIncrease:
                GameManager.Instance.GameData.GetPlayerStatsById(SessionManager.Instance.PlayerId).UpdatePlayerTipIncrease(amount);
                break;
            case UpgradeTarget.ExpansionRestaurant:
                GridBuildingSystem.Instance.ExpandGrid(amount);
                break;
        }
    }
}