using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
[Serializable]
public class ConfigUpgrade: ScriptableObject
{
    [SerializeField] private List<UpgradeSO> upgrades = new List<UpgradeSO>();

    public List<UpgradeSO> Upgrades { get => upgrades; set => upgrades = value; }

    public List<UpgradeSO> GetUpgradeOfType(UpgradeType upgradeType)
    {
        return Upgrades.FindAll(upgrade => upgrade.UpgradeType == upgradeType);
    }
}