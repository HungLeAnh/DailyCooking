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
    private GameData subscribedGameData;

    private void OnDestroy()
    {
        Unsubscribe();
    }

    // The popup is built once but GameData is replaced when another restaurant is loaded or
    // joined: follow the current one and re-read its state (called when the popup is shown).
    public void Refresh()
    {
        if (upgradeData == null)
            return;
        GameData gameData = GameManager.Instance.GameData;
        if (gameData == null)
            return;
        if (gameData != subscribedGameData)
        {
            Unsubscribe();
            subscribedGameData = gameData;
            gameData.RestaurantData.OnLevelChange += PlayerStats_OnLevelChange;
            gameData.UpgradeData.OnMenuDataChanged += UpgradeData_OnChanged;
        }
        isPurchased = gameData.IsUpgradePurchased(upgradeData);
        dimGameObject.SetActive(gameData.RestaurantData.Level < upgradeData.LevelUnlocked);
        SetPurchased(isPurchased);
    }

    private void Unsubscribe()
    {
        if (subscribedGameData == null)
            return;
        subscribedGameData.RestaurantData.OnLevelChange -= PlayerStats_OnLevelChange;
        subscribedGameData.UpgradeData.OnMenuDataChanged -= UpgradeData_OnChanged;
        subscribedGameData = null;
    }

    // The purchase is confirmed by the server (GameManager.PurchaseUpgradeServerRpc).
    private void UpgradeData_OnChanged()
    {
        if (upgradeData == null || isPurchased)
            return;
        isPurchased = GameManager.Instance.GameData.IsUpgradePurchased(upgradeData);
        SetPurchased(isPurchased);
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

        upgradeButton.onClick.RemoveAllListeners();
        upgradeButton.onClick.AddListener(OnUpgradeButtonClick);

        Refresh();
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
        UpgradeManager.Instance.PurchaseUpgrade(upgradeData);
    }
}
