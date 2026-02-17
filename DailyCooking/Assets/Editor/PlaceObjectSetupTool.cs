using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public class PlaceObjectSetupTool 
{
    [MenuItem("Tools/Setup Place Object SO")]
    public static void PlaceObjectSetupCounter()
    {
        string prefabFolderPath = "Assets/Prefab/Counters"; 
        string scriptableObjectFolderPath = "Assets/SO/BuildingSO/Counters"; 
        string iconFolderPath = "Assets/_Assets/Icons/Counters"; 

        // Ensure the ScriptableObject folder exists
        if (!AssetDatabase.IsValidFolder(scriptableObjectFolderPath))
        {
            Debug.LogError("ScriptableObject folder does not exist: " + scriptableObjectFolderPath);
            return;
        }        
        // Ensure the icon folder exists
        if (!AssetDatabase.IsValidFolder(iconFolderPath))
        {
            Debug.LogError("ScriptableObject folder does not exist: " + iconFolderPath);
            return;
        }
        string[] iconGuids = AssetDatabase.FindAssets("t:Sprite", new[] { iconFolderPath });
        Debug.LogWarning(iconGuids.Length);
        List<Sprite> sprites = new List<Sprite>();
        foreach (string guid in iconGuids)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
            sprites.Add(sprite);
            Debug.Log(sprite.name);
        }

        int count = 0;
        // Find all prefabs in the specified folder
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabFolderPath });
        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (prefab != null && !prefab.name.Contains("base",System.StringComparison.OrdinalIgnoreCase))
            {
                count++;
                // Create a new ScriptableObject
                PlacedObjectTypeSO placedObjectTypeSO = ScriptableObject.CreateInstance<PlacedObjectTypeSO>();
                placedObjectTypeSO.id = count.ToString();
                placedObjectTypeSO.nameString = prefab.name + "PlaceObjectSO";
                placedObjectTypeSO.prefab = prefab;
                placedObjectTypeSO.icon = sprites.FirstOrDefault(x => x.name == prefab.name);
                placedObjectTypeSO.width = 1;
                placedObjectTypeSO.height = 1;
                placedObjectTypeSO.itemType = new ItemType();
                placedObjectTypeSO.itemType.TabType = InventoryTabType.Counter;

                // Save the ScriptableObject asset
                string assetPath = $"{scriptableObjectFolderPath}/{prefab.name}.asset";
                AssetDatabase.CreateAsset(placedObjectTypeSO, assetPath);
                EditorUtility.SetDirty(placedObjectTypeSO);
            }
        }

        // Save and refresh the AssetDatabase
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}