using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ConfigFoodTool
{
    public static string SOFolderPath = "Assets/SO/FoodSO";
    public static string SavedPath = "Assets/Configs/ConfigFood/ConfigFoodSO.asset";

    [MenuItem("Tools/ConfigFood/Create ConfigFoodSO")]
    public static void GeneratePopupDatabase()
    {
        // Find all prefab assets in the folder
        string[] scriptableObjectGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { SOFolderPath });
        var menu = Resources.Load<ConfigFood>(SavedPath) ;
        if (menu != null)
        {
            Debug.LogWarning($"ConfigMenuSO already exists at {SavedPath}. Add new data cho file");
            foreach (string guid in scriptableObjectGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FoodSO prefab = AssetDatabase.LoadAssetAtPath<FoodSO>(path);
                if (menu.FoodItems.Find(x => x == prefab) != null)
                {
                    continue;
                }
                menu.FoodItems.Add(prefab);
            }
            SaveConfigMenuAsset(menu);
        }
        else
        {
            ConfigFood configMenu = ScriptableObject.CreateInstance<ConfigFood>();
            foreach (string guid in scriptableObjectGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                FoodSO prefab = AssetDatabase.LoadAssetAtPath<FoodSO>(path);
                configMenu.FoodItems.Add(prefab);
            }
            SaveConfigMenuAsset(configMenu);
        }
        Debug.Log($"Prefab list created at {SavedPath}");
    }

    private static void SaveConfigMenuAsset(ConfigFood menu)
    {
        AssetDatabase.CreateAsset(menu, SavedPath);
        EditorUtility.SetDirty(menu);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
