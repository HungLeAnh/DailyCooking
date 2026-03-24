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
    [SerializeField] private GameObject checkButton;

    private UpgradeSO upgradeData;
    private bool isPurchased = false;
    private void Start()
    {
        GameManager.Instance.GameData.RestaurantData.OnLevelChange += PlayerStats_OnLevelChange;
    }

    private void PlayerStats_OnLevelChange()
    {
        if(isPurchased)
        {
            return;
        }
        var isLocked = GameManager.Instance.GameData.RestaurantData.Level < upgradeData.LevelUnlocked;
        dimGameObject.SetActive(isLocked);
    }

    public void SetupItem(UpgradeSO upgradeSO)
    {
        upgradeData = upgradeSO;
        itemNameText.text = upgradeSO.UpgradeName;
        itemDescripeText.text = upgradeSO.UpgradeDescription;
        itemIconImage.sprite = upgradeSO.UpgradeIcon;
        upgradeCostText.text = upgradeSO.UpgradeCosts.ToString();

        var isLocked = GameManager.Instance.GameData.RestaurantData.Level < upgradeSO.LevelUnlocked;
        dimGameObject.SetActive(isLocked);
        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OnUpgradeButtonClick);

        isPurchased = GameManager.Instance.GameData.IsUpgradePurchased(upgradeSO);
        SetPurchased(isPurchased);
    }
    public void SetPurchased(bool isPurchased)
    {
        if(isPurchased)
        {
            dimGameObject.SetActive(true);
            checkButton.SetActive(true);
            upgradeButton.interactable = false;
        }
        else
        {
            checkButton.SetActive(false);
            upgradeButton.interactable = true;
        }
    }

    private void OnUpgradeButtonClick()
    {
        if (UpgradeManager.Instance.PurchaseUpgrade(upgradeData))
        {
            SetPurchased(true);
            isPurchased = true;
        }
    }
}
