using UnityEditor;
using UnityEngine;

public class ConfigUpgradeTool
{
    public const string CONFIG_UPGRADE_CSV_PATH = "Assets/Configs/ConfigUpgrade/ConfigUpgrade.csv";
    public const string CONFIG_CONFIG_UPGRADE_SO_PATH = "Assets/Configs/ConfigUpgrade/ConfigUpgradeSO.asset";
    public const string CONFIG_UPGRADE_SO_PATH = "Assets/SO/UpgradeSO/";
    [MenuItem("Tools/ConfigUpgrade/Create Config Upgrade")]
    public static void CreateConfigShopSO()
    {
        ConfigUpgrade configUpgrade = ScriptableObject.CreateInstance<ConfigUpgrade>();
        var customDeserializedList = CSVParser<UpgradeSO>.ParseCSV(CONFIG_UPGRADE_CSV_PATH);
        foreach (var item in customDeserializedList)
        {
            UpgradeSO upgrade = ScriptableObject.CreateInstance<UpgradeSO>();
            upgrade = item;
            upgrade.name = upgrade.UpgradeName;
            AssetDatabase.CreateAsset(upgrade, CONFIG_UPGRADE_SO_PATH+ upgrade.UpgradeName+".asset");
            EditorUtility.SetDirty(configUpgrade);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            configUpgrade.Upgrades.Add(upgrade);
        }
        AssetDatabase.CreateAsset(configUpgrade, CONFIG_CONFIG_UPGRADE_SO_PATH);
        EditorUtility.SetDirty(configUpgrade);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

    }

}
