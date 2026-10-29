using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Player;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildChecks
{
    private const string OUTPUT_FOLDER = "Temp/BuildChecks/AndroidPlayerScripts";

    // Pinned snapshot of the enums that get serialized as numbers into assets, prefabs,
    // scenes and the network payload. Unity never reports a renumber: it silently reinterprets
    // every already-written value. This project has been bitten once already (inserting
    // Ingredient in May 2026 shifted Gem/Coin data in ShopItemType), so the build fails
    // loudly instead if the numbers below stop matching the code.
    //
    // If you intentionally add an enum value, append it here with its new number in the same
    // commit. Do NOT renumber an existing entry to make the build pass.
    private static readonly (string Name, int Value)[] PinnedShopItemCategory =
    {
        ("Counters", 0), ("Tables", 1), ("Walls", 2), ("Vegetables", 3),
        ("Bakery", 4), ("Dairy", 5), ("Patties", 6), ("Meats", 7), ("Tools", 8), ("None", 9999),
    };

    private static readonly (string Name, int Value)[] PinnedShopItemType =
    {
        ("None", 0), ("Item", 1), ("Ingredient", 2), ("Gem", 3), ("Coin", 4),
    };

    // Runs automatically on every build (Android, iOS, standalone).
    public class PinnedShopEnumGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            VerifyPinnedEnums();
            VerifyPlacedObjectGuids();
        }
    }

    [MenuItem("Tools/Build Checks/Verify Pinned Shop Enums")]
    public static void VerifyPinnedEnumsMenuItem()
    {
        VerifyPinnedEnums();
    }

    // Compares each enum's declared numeric values against the pinned snapshot. A mismatch
    // means someone reordered, renumbered or deleted a value, which would silently corrupt
    // ConfigShopSO.asset and desync the wire protocol.
    public static void VerifyPinnedEnums()
    {
        List<string> problems = new List<string>();
        CheckEnum<ShopItemCategory>(PinnedShopItemCategory, problems);
        CheckEnum<ShopItemType>(PinnedShopItemType, problems);

        if (problems.Count == 0)
        {
            Debug.Log("[BuildChecks] Pinned shop enums match the snapshot.");
            return;
        }

        string message = "[BuildChecks] Pinned shop enum mismatch:\n  " +
                         string.Join("\n  ", problems) +
                         "\nRenumbering an already-serialized enum silently reinterprets every " +
                         "value in ConfigShopSO.asset, prefabs, scenes and the network payload. " +
                        "If this change is intentional, append the new value and update the " +
                        "snapshot in BuildChecks.cs in the same commit.";
        if (Application.isBatchMode)
            throw new BuildFailedException(message);
        Debug.LogError(message);
    }

    [MenuItem("Tools/Build Checks/Verify Placed Object GUIDs")]
    public static void VerifyPlacedObjectGuidsMenuItem()
    {
        VerifyPlacedObjectGuids();
    }

    // PlacedObjectTypeSO stores a copy of its asset GUID; inventory and shop rewards use
    // that copy as the key. An empty/stale copy makes a purchased item disappear after the
    // coins are spent, so fail the build before it can ship.
    public static void VerifyPlacedObjectGuids()
    {
        List<string> problems = new List<string>();
        Dictionary<string, PlacedObjectTypeSO> placedObjects =
            new Dictionary<string, PlacedObjectTypeSO>();

        foreach (string assetGuid in AssetDatabase.FindAssets("t:PlacedObjectTypeSO", new[] { "Assets/SO/BuildingSO" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGuid);
            PlacedObjectTypeSO placedObject = AssetDatabase.LoadAssetAtPath<PlacedObjectTypeSO>(path);
            if (placedObject == null)
                continue;

            if (string.IsNullOrEmpty(placedObject.Guid))
                problems.Add($"{path}: _guid is empty (asset guid is {assetGuid}).");
            else
            {
                if (placedObject.Guid != assetGuid)
                    problems.Add($"{path}: _guid {placedObject.Guid} does not match asset guid {assetGuid}.");
                placedObjects[placedObject.Guid] = placedObject;
            }
        }

        ConfigShop shop = AssetDatabase.LoadAssetAtPath<ConfigShop>(
            "Assets/Configs/ConfigShop/ConfigShopSO.asset");
        if (shop != null && shop.ShopItems != null)
        {
            foreach (ConfigShopItem item in shop.ShopItems)
            {
                if (item == null || item.Type != ShopItemType.Item || item.Rewards == null)
                    continue;
                foreach (ShopReward reward in item.Rewards)
                {
                    if (string.IsNullOrEmpty(reward.Guid) || !placedObjects.ContainsKey(reward.Guid))
                        problems.Add($"Shop item {item.Id} ({item.Name}): reward guid '{reward.Guid}' does not resolve.");
                }
            }
        }

        if (problems.Count == 0)
        {
            Debug.Log("[BuildChecks] Placed object GUIDs and shop rewards resolve.");
            return;
        }

        string message = "[BuildChecks] Invalid placed object GUIDs:\n  " + string.Join("\n  ", problems);
        if (Application.isBatchMode)
            throw new BuildFailedException(message);
        Debug.LogError(message);
    }

    private static void CheckEnum<TEnum>((string Name, int Value)[] pinned, List<string> problems)
        where TEnum : Enum
    {
        Type type = typeof(TEnum);
        Dictionary<string, int> actual = new Dictionary<string, int>(pinned.Length);
        foreach (TEnum value in Enum.GetValues(type))
            actual[value.ToString()] = Convert.ToInt32(value);

        foreach ((string name, int expected) in pinned)
        {
            if (!actual.TryGetValue(name, out int found))
                problems.Add($"{type.Name}.{name}: expected {expected}, but the value is gone from the enum.");
            else if (found != expected)
                problems.Add($"{type.Name}.{name}: expected {expected}, code now says {found}.");
        }

        foreach (KeyValuePair<string, int> pair in actual)
        {
            bool known = false;
            foreach ((string name, int _) in pinned)
            {
                if (name == pair.Key) { known = true; break; }
            }
            if (!known)
                problems.Add($"{type.Name}.{pair.Key}: new value {pair.Value} is not in the pinned snapshot in BuildChecks.cs.");
        }
    }

    // Compiles runtime assemblies the way an Android player build does (no UNITY_EDITOR,
    // no editor-only assemblies) so editor-only API leaks show up without a full build.
    [MenuItem("Tools/Build Checks/Compile Android Player Scripts")]
    public static void CompileAndroidPlayerScripts()
    {
        var settings = new ScriptCompilationSettings
        {
            target = BuildTarget.Android,
            group = BuildTargetGroup.Android,
            options = ScriptCompilationOptions.None
        };

        Directory.CreateDirectory(OUTPUT_FOLDER);
        ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings, OUTPUT_FOLDER);

        if (result.assemblies == null || result.assemblies.Count == 0)
            Debug.LogError("[BuildChecks] Android player script compilation failed. See the errors above.");
        else
            Debug.Log($"[BuildChecks] Android player scripts compiled: {result.assemblies.Count} assemblies.");
    }
}
