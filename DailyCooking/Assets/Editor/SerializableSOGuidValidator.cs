using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Second safety net for SerializableScriptableObject._guid. The lazy self-heal in the
// Guid getter fixes assets when their GUID is first read; this processor fixes them at
// save time, so an asset saved with an empty or stale _guid is corrected (and reported)
// before it can reach the repository or a build.
public class SerializableSOGuidValidator : AssetModificationProcessor
{
    private static readonly string[] WatchedTypes =
    {
        "FoodSO",
        "KitchenObjectSO",
        "PlacedObjectTypeSO",
        "UpgradeSO",
    };

    private static string[] OnWillSaveAssets(string[] paths)
    {
        foreach (string path in paths)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset"))
                continue;

            var asset = AssetDatabase.LoadAssetAtPath<SerializableScriptableObject>(path);
            if (asset == null)
                continue;

            string typeName = asset.GetType().Name;
            bool watched = false;
            foreach (string t in WatchedTypes)
            {
                if (typeName == t) { watched = true; break; }
            }
            if (!watched)
                continue;

            string assetGuid = AssetDatabase.AssetPathToGUID(path);
            string current = asset.Guid; // goes through the self-healing getter

            if (string.IsNullOrEmpty(current) || current != assetGuid)
            {
                var so = new SerializedObject(asset);
                so.FindProperty("_guid").stringValue = assetGuid;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                Debug.LogWarning(
                    $"[SerializableSOGuidValidator] Fixed _guid on '{asset.name}' ({typeName}) at {path}: " +
                    $"'{current}' -> '{assetGuid}'.");
            }
        }

        return paths;
    }
}
