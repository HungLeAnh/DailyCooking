using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class PlacedObjectDatabaseTool 
{
    public static string scriptableObjectFolderPath = "Assets/SO/BuildingSO/Counters";
    public static string SavePlacedObjectDatabaseFolderPath = "Assets/SO/PlacedObjectDatabase/PlacedObjectDatabase.asset";
    public static string SavePlacedObjectCSVPath = "Assets/Configs/PlacedObject/PlacedObjectConfig.csv";

    [MenuItem("Tools/PlacedObjectDatabaseTool/GeneratePlacedObjectDatabase")]
    public static void GeneratePopupDatabase()
    {

        // Find all prefab assets in the folder
        string[] scriptableObjectGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { scriptableObjectFolderPath });
        List<PlacedObjectTypeSO> placedObjectDataList = new List<PlacedObjectTypeSO>();

        foreach (string guid in scriptableObjectGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            PlacedObjectTypeSO prefab = AssetDatabase.LoadAssetAtPath<PlacedObjectTypeSO>(path);
            if (prefab != null)
            {
                if(prefab.nameString.Contains("base",StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                placedObjectDataList.Add(prefab);

                using (StreamWriter writer = new StreamWriter(SavePlacedObjectCSVPath, true)) // Open in append mode
                {
                    writer.WriteLine($"{prefab.id},{prefab.name},{prefab.Guid}");
                }
            }
        }

        // Create or update the ScriptableObject
        PlacedObjectDatabase placedObjectDatabase = ScriptableObject.CreateInstance<PlacedObjectDatabase>();
        placedObjectDatabase.PlacedObjects = placedObjectDataList;

        // Save the ScriptableObject asset
        AssetDatabase.CreateAsset(placedObjectDatabase, SavePlacedObjectDatabaseFolderPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Prefab list created with {placedObjectDataList.Count} prefabs at {SavePlacedObjectDatabaseFolderPath}");
    }

}