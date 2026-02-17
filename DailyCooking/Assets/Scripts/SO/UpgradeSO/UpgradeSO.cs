using System;
using UnityEngine;
[CreateAssetMenu()]
[Serializable]
public class UpgradeSO : SerializableScriptableObject
{
    [SerializeField] private string upgradeName;
    [SerializeField] private string upgradeDescription;
    [SerializeField] private Sprite upgradeIcon;
    [SerializeField] private int upgradeCosts;
    [SerializeField] private int levelUnlocked;
    [SerializeField] private UpgradeType upgradeType;
    [SerializeField] private UpgradeTarget upgradeTarget;
    [SerializeField] private float upgradeValue;

    public string UpgradeName { get => upgradeName; set => upgradeName = value; }
    public string UpgradeDescription { get => upgradeDescription; set => upgradeDescription = value; }
    public Sprite UpgradeIcon { get => upgradeIcon; set => upgradeIcon = value; }
    public int UpgradeCosts { get => upgradeCosts; set => upgradeCosts = value; }
    public int LevelUnlocked { get => levelUnlocked; set => levelUnlocked = value; }
    public UpgradeType UpgradeType { get => upgradeType; set => upgradeType = value; }
    public UpgradeTarget UpgradeTarget { get => upgradeTarget; set => upgradeTarget = value; }
    public float UpgradeValue { get => upgradeValue; set => upgradeValue = value; }
}
public enum UpgradeType
{
    Restaurant,
    Skill
}
public enum UpgradeTarget
{
    MoveSpeed,
    CookingSpeed,
    CarryingCapacity,
    TipIncrease,
    ExpansionRestaurant,
}
