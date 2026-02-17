using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Reflection.Emit;
using System.Reflection;
using System;
using System.Text;
using System.IO;
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
        List<string> popupNames = new List<string>();
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                if(prefab.GetComponent<UIPopup>() == null)
                {
                    continue;
                }
                else
                {
                    PopupData popupData = new PopupData();
                    popupData.popupName = prefab.name;
                    popupData.popupPrefab = prefab;
                    popupDataList.Add(popupData);
                    popupNames.Add(popupData.popupName);
                }
            }
        }

        // Create or update the ScriptableObject
        PopupDatabase popupDatabase = ScriptableObject.CreateInstance<PopupDatabase>();
        popupDatabase.Popups = popupDataList;

        // Save the ScriptableObject asset
        AssetDatabase.CreateAsset(popupDatabase, SavePopupDatabaseFolderPath);
        AssetDatabase.SaveAssets();
        CreateEnum(popupNames);
        Debug.Log($"Prefab list created with {popupDataList.Count} prefabs at {SavePopupDatabaseFolderPath}");
       
    }


    public static void CreateEnum(List<string> values)
    {
        string enumName = "UIPopupType";

        var sb = new StringBuilder();
        sb.AppendLine("public enum " + enumName);
        sb.AppendLine("{");
        for (int i = 0; i < values.Count; i++)
        {
            sb.Append("    " + values[i]);
            if (i < values.Count - 1)
                sb.Append(",");
            sb.AppendLine();
        }
        sb.AppendLine("}");

        File.WriteAllText("Assets/Scripts/UI/UIPopup/UIPopupType.cs", sb.ToString());

    }

}
