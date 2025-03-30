using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class PopupDatabaseTool
{
    public static string PopupPrefabFolderPath = "Assets/Prefab/UI/Popups";
    public static string SavePopupDatabaseFolderPath = "Assets/SO/PopupDatabase/PopupDatabase.asset";

    [MenuItem("Tools/PopupDatabaseTool/GeneratePopupDatabase")]
    public static void GeneratePopupDatabase()
    {

        // Find all prefab assets in the folder
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PopupPrefabFolderPath });
        List<PopupData> popupDataList = new List<PopupData>();

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                PopupData popupData = new PopupData();
                popupData.popupName = prefab.name;
                popupData.popupPrefab = prefab;
                popupDataList.Add(popupData);
            }
        }

        // Create or update the ScriptableObject
        PopupDatabase popupDatabase = ScriptableObject.CreateInstance<PopupDatabase>();
        popupDatabase.Popups = popupDataList;

        // Save the ScriptableObject asset
        AssetDatabase.CreateAsset(popupDatabase, SavePopupDatabaseFolderPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Prefab list created with {popupDataList.Count} prefabs at {SavePopupDatabaseFolderPath}");
       
    }
}
