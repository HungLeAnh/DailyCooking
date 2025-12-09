using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescripeText;
    [SerializeField] private TextMeshProUGUI upgradeCostText;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject dimGameObject;

    private UpgradeSO upgradeData;
    private void Start()
    {
        GameManager.Instance.GameData.PlayerStats.OnLevelChange += PlayerStats_OnLevelChange;
    }

    private void PlayerStats_OnLevelChange()
    {
        var isLocked = GameManager.Instance.GameData.PlayerStats.playerData.Level < upgradeData.LevelUnlocked;
        dimGameObject.SetActive(isLocked);
    }

    public void SetupItem(UpgradeSO upgradeSO)
    {
        upgradeData = upgradeSO;
        itemNameText.text = upgradeSO.UpgradeName;
        itemDescripeText.text = upgradeSO.UpgradeDescription;
        itemIconImage.sprite = upgradeSO.UpgradeIcon;
        upgradeCostText.text = upgradeSO.UpgradeCosts.ToString();

        var isLocked = GameManager.Instance.GameData.PlayerStats.playerData.Level < upgradeSO.LevelUnlocked;
        dimGameObject.SetActive(isLocked);
        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OnUpgradeButtonClick);

    }

    private void OnUpgradeButtonClick()
    {
        UpgradeManager.Instance.PurchaseUpgrade(upgradeData);
    }
}
