using System.Linq;
using UnityEngine;
public class UIUpgradePopup : UIPopup
{
    [SerializeField] private Transform upgradeSkillContainer;
    [SerializeField] private Transform upgradeRestaurantContainer;

    [SerializeField] private GameObject upgradeItemPrefab;

    public override void SetupPopup()
    {
        base.SetupPopup();
        foreach (Transform child in upgradeSkillContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in upgradeRestaurantContainer)
        {
            Destroy(child.gameObject);
        }

        var skillList = ConfigManager.Instance.ConfigUpgrade.Upgrades.Select(x=>x)
            .Where(x=>x.UpgradeType==UpgradeType.Skill)
            .OrderBy(x=>x.LevelUnlocked)
            .ToList();

        foreach (var upgradeSO in skillList)
        {
            var upgradeItem = Instantiate(upgradeItemPrefab, upgradeSkillContainer);
            var uiUpgradeItem  = upgradeItem.GetComponent<UIUpgradeItem>();
            uiUpgradeItem.SetupItem(upgradeSO);
        }

        var restaurantList = ConfigManager.Instance.ConfigUpgrade.Upgrades.Select(x=>x)
            .Where(x=>x.UpgradeType==UpgradeType.Restaurant)
            .OrderBy(x=>x.LevelUnlocked)
            .ToList();
        foreach (var upgradeSO in restaurantList)
        {
            var upgradeItem = Instantiate(upgradeItemPrefab, upgradeRestaurantContainer);
            var uiUpgradeItem = upgradeItem.GetComponent<UIUpgradeItem>();
            uiUpgradeItem.SetupItem(upgradeSO);
        }
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
    }

    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
    }
    public void OnCloseClick()
    {
        HidePopup();
    }
}