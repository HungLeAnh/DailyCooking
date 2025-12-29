using System;
using System.Collections.Generic;

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

