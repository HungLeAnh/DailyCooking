using UnityEditor;
using UnityEngine;

public class ConfigShopTool
{
    public const string CONFIG_SHOP_CSV_PATH = "Assets/Configs/ConfigShop/ConfigShop.csv";
    public const string CONFIG_SHOP_SO_PATH = "Assets/Configs/ConfigShop/ConfigShopSO.asset";
    [MenuItem("Tools/ConfigShop/Create ConfigShopSO")]
    public static void CreateConfigShopSO()
    {
        ConfigShop configShop = ScriptableObject.CreateInstance<ConfigShop>();
        var customDeserializedList = CSVParser<ConfigShopItem>.ParseCSV(CONFIG_SHOP_CSV_PATH);
        foreach (var item in customDeserializedList)
        {
            configShop.ShopItems.Add(item);
        }
        AssetDatabase.CreateAsset(configShop, CONFIG_SHOP_SO_PATH);
        EditorUtility.SetDirty(configShop);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

    }

}
