using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class CookingDataTests
{
    private const string CuttingBoardPath = "Assets/SO/BuildingSO/Counters/Tools/CuttingBoard.asset";
    private const string OvenPath = "Assets/SO/BuildingSO/Counters/Tools/Oven.asset";
    private const string OvenConfigPath = "Assets/SO/CookingToolConfigSO/Oven.asset";
    private const string OvenPrefabPath = "Assets/Prefab/CookingTool/OvenCookingTool.prefab";
    private const string PlacedObjectDatabasePath = "Assets/SO/PlacedObjectDatabase/PlacedObjectDatabase.asset";
    private const string DefaultNetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
    private const string RecipeDatabasePath = "Assets/SO/RecipeDatabase/RecipeDatabase.asset";
    private const string PotatoPath = "Assets/SO/KitchenObjectSO/PotatoSO.asset";
    private const string ChoppedPotatoPath = "Assets/SO/KitchenObjectSO/PotatoChoppedSO.asset";
    private const string MashedPotatoPath = "Assets/SO/KitchenObjectSO/PotatoMashedSO.asset";
    private const string StewPath = "Assets/SO/CombineRecipeSO/StewCombineRecipeSO.asset";
    private const string ShopPath = "Assets/Configs/ConfigShop/ConfigShopSO.asset";

    private static T Load<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        Assert.IsNotNull(asset, $"Missing asset at {path}");
        return asset;
    }

    [Test] public void CuttingBoardGuid_MatchesAssetGuid()
    {
        PlacedObjectTypeSO cuttingBoard = Load<PlacedObjectTypeSO>(CuttingBoardPath);
        string assetGuid = AssetDatabase.AssetPathToGUID(CuttingBoardPath);

        Assert.IsFalse(string.IsNullOrEmpty(cuttingBoard.Guid));
        Assert.AreEqual(assetGuid, cuttingBoard.Guid);
    }

    [Test] public void OvenConfig_SupportsBakingOnly()
    {
        CookingToolConfigSO config = Load<CookingToolConfigSO>(OvenConfigPath);

        Assert.IsTrue(config.Supports(CookingToolConfigSO.CookingToolType.Baking));
        Assert.IsFalse(config.Supports(CookingToolConfigSO.CookingToolType.Cutting));
        Assert.IsFalse(config.Supports(CookingToolConfigSO.CookingToolType.Combine));
        Assert.IsFalse(config.supportsOptionMenu);
    }

    [Test] public void OvenGuid_MatchesAssetGuid()
    {
        PlacedObjectTypeSO oven = Load<PlacedObjectTypeSO>(OvenPath);
        string assetGuid = AssetDatabase.AssetPathToGUID(OvenPath);

        Assert.IsFalse(string.IsNullOrEmpty(oven.Guid));
        Assert.AreEqual(assetGuid, oven.Guid);
    }

    [Test] public void Oven_IsToolWithExpectedPlacement()
    {
        PlacedObjectTypeSO oven = Load<PlacedObjectTypeSO>(OvenPath);

        Assert.IsTrue(oven.isTool);
        Assert.AreEqual(1, oven.width);
        Assert.AreEqual(1, oven.height);
        Assert.IsNotNull(oven.prefab);
        Assert.AreEqual("OvenCookingTool", oven.prefab.name);
    }

    [Test] public void PlacedObjectDatabase_ContainsOven()
    {
        PlacedObjectTypeSO oven = Load<PlacedObjectTypeSO>(OvenPath);
        ScriptableObject database = Load<ScriptableObject>(PlacedObjectDatabasePath);
        SerializedProperty placeObjects = new SerializedObject(database).FindProperty("placeObjects");
        bool found = false;

        for (int i = 0; i < placeObjects.arraySize; i++)
        {
            if (placeObjects.GetArrayElementAtIndex(i).objectReferenceValue == oven)
            {
                found = true;
                break;
            }
        }

        Assert.IsTrue(found, "Oven is missing from PlacedObjectDatabase.");
    }

    [Test] public void DefaultNetworkPrefabs_ContainsOven()
    {
        GameObject ovenPrefab = Load<GameObject>(OvenPrefabPath);
        ScriptableObject networkPrefabs = Load<ScriptableObject>(DefaultNetworkPrefabsPath);
        SerializedProperty list = new SerializedObject(networkPrefabs).FindProperty("List");
        bool found = false;

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty prefab = list.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab");
            if (prefab != null && prefab.objectReferenceValue == ovenPrefab)
            {
                found = true;
                break;
            }
        }

        Assert.IsTrue(found, "Oven is missing from DefaultNetworkPrefabs.");
    }

    [Test] public void CookingToolPrefabs_HaveUniqueNetworkHashes()
    {
        HashSet<int> hashes = new HashSet<int>();

        foreach (string assetGuid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab/CookingTool" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGuid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Unity.Netcode.NetworkObject networkObject = prefab != null ? prefab.GetComponent<Unity.Netcode.NetworkObject>() : null;
            if (networkObject == null)
                continue;

            SerializedProperty hash = new SerializedObject(networkObject).FindProperty("GlobalObjectIdHash");
            Assert.IsNotNull(hash, path);
            Assert.IsTrue(hashes.Add(hash.intValue), $"Duplicate NetworkObject hash in {path}");
        }
    }

    [Test] public void OvenPrefab_HasCookingToolAndCollider()
    {
        GameObject prefab = Load<GameObject>(OvenPrefabPath);
        MeshCollider[] colliders = prefab.GetComponentsInChildren<MeshCollider>(true);

        Assert.IsNotNull(prefab.GetComponent<CookingTool>());
        Assert.Greater(colliders.Length, 0, "Oven has no MeshCollider.");
        foreach (MeshCollider collider in colliders)
            Assert.IsTrue(collider.convex, $"{collider.name} collider must be convex.");
    }

    [Test] public void RecipeDatabase_BakingRecipesAreComplete()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        SerializedProperty recipes = new SerializedObject(database).FindProperty("bakingRecipes");
        database.Initialize();

        Assert.GreaterOrEqual(recipes.arraySize, 7, "Expected all baking recipes in the database.");
        for (int i = 0; i < recipes.arraySize; i++)
        {
            BakingRecipeSO recipe = recipes.GetArrayElementAtIndex(i).objectReferenceValue as BakingRecipeSO;
            Assert.IsNotNull(recipe, $"Baking recipe {i} is null.");
            Assert.IsNotNull(recipe.input, $"{recipe.name} has no input.");
            Assert.IsNotNull(recipe.output, $"{recipe.name} has no output.");
            Assert.Greater(recipe.bakingTimerMax, 0f, $"{recipe.name} has no timer.");
            Assert.AreSame(recipe, database.GetBakingRecipe(recipe.input), recipe.name);
            Assert.IsNotNull(database.GetBurningRecipe(recipe.output), $"{recipe.name}: output has no burning recipe.");
        }
    }

    [Test] public void ShopItem37_UsesOvenToolReward()
    {
        ConfigShop shop = Load<ConfigShop>(ShopPath);
        PlacedObjectTypeSO oven = Load<PlacedObjectTypeSO>(OvenPath);
        ConfigShopItem item = null;

        foreach (ConfigShopItem candidate in shop.ShopItems)
        {
            if (candidate != null && candidate.Id == 37)
            {
                item = candidate;
                break;
            }
        }

        Assert.IsNotNull(item, "Missing shop item 37.");
        Assert.AreEqual("Oven Tool", item.Name);
        Assert.AreEqual(ShopItemCategory.Tools, item.Category);
        Assert.AreEqual(ShopItemType.Item, item.Type);
        Assert.AreEqual(200, item.Price);
        Assert.AreEqual(3, item.UnlockLevel);
        Assert.IsNotNull(item.Rewards);
        Assert.AreEqual(1, item.Rewards.Count);
        Assert.AreEqual(oven.Guid, item.Rewards[0].Guid);
    }

    [Test] public void RecipeDatabase_PotatoResolvesToChoppedPotato()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        database.Initialize();

        KitchenObjectSO potato = Load<KitchenObjectSO>(PotatoPath);
        KitchenObjectSO choppedPotato = Load<KitchenObjectSO>(ChoppedPotatoPath);
        CuttingRecipeSO recipe = database.GetCuttingRecipe(potato);

        Assert.IsNotNull(recipe);
        Assert.AreSame(choppedPotato, recipe.output);
    }

    [Test] public void RecipeDatabase_PotatoHasChoppedAndMashedOptions()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        database.Initialize();

        KitchenObjectSO potato = Load<KitchenObjectSO>(PotatoPath);
        KitchenObjectSO choppedPotato = Load<KitchenObjectSO>(ChoppedPotatoPath);
        KitchenObjectSO mashedPotato = Load<KitchenObjectSO>(MashedPotatoPath);
        List<CuttingRecipeSO> recipes = database.GetCuttingRecipesForInput(potato);

        Assert.IsNotNull(recipes);
        Assert.AreEqual(2, recipes.Count);
        CollectionAssert.Contains(recipes.ConvertAll(recipe => recipe.output), choppedPotato);
        CollectionAssert.Contains(recipes.ConvertAll(recipe => recipe.output), mashedPotato);
    }

    [Test] public void RecipeDatabase_CuttingOutputsAreUniquePerInput()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        SerializedObject serializedDatabase = new SerializedObject(database);
        SerializedProperty recipes = serializedDatabase.FindProperty("cuttingRecipes");
        Dictionary<KitchenObjectSO, HashSet<KitchenObjectSO>> outputsByInput =
            new Dictionary<KitchenObjectSO, HashSet<KitchenObjectSO>>();

        for (int i = 0; i < recipes.arraySize; i++)
        {
            CuttingRecipeSO recipe = recipes.GetArrayElementAtIndex(i).objectReferenceValue as CuttingRecipeSO;
            Assert.IsNotNull(recipe, $"Cutting recipe {i} is null.");
            Assert.IsNotNull(recipe.input, $"Cutting recipe {i} has no input.");
            Assert.IsNotNull(recipe.output, $"Cutting recipe {i} has no output.");

            if (!outputsByInput.TryGetValue(recipe.input, out HashSet<KitchenObjectSO> outputs))
            {
                outputs = new HashSet<KitchenObjectSO>();
                outputsByInput[recipe.input] = outputs;
            }
            Assert.IsTrue(outputs.Add(recipe.output),
                $"Duplicate cutting output: {recipe.input.name} -> {recipe.output.name}");
        }
    }

    [Test] public void StewCombineRecipe_UsesChoppedPotato()
    {
        CombineRecipeSO stew = Load<CombineRecipeSO>(StewPath);
        KitchenObjectSO choppedPotato = Load<KitchenObjectSO>(ChoppedPotatoPath);

        CollectionAssert.Contains(stew.input, choppedPotato);
    }

    [Test] public void ShopToolItems_UseToolsCategoryAndResolveRewards()
    {
        ConfigShop shop = Load<ConfigShop>(ShopPath);
        Dictionary<string, PlacedObjectTypeSO> placedObjects = new Dictionary<string, PlacedObjectTypeSO>();
        foreach (string assetGuid in AssetDatabase.FindAssets("t:PlacedObjectTypeSO", new[] { "Assets/SO/BuildingSO" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(assetGuid);
            PlacedObjectTypeSO placedObject = AssetDatabase.LoadAssetAtPath<PlacedObjectTypeSO>(path);
            if (placedObject != null && !string.IsNullOrEmpty(placedObject.Guid))
                placedObjects[placedObject.Guid] = placedObject;
        }

        int toolItemCount = 0;
        foreach (ConfigShopItem item in shop.ShopItems)
        {
            if (item.Type != ShopItemType.Item || !item.Name.Contains("Tool"))
                continue;

            toolItemCount++;
            Assert.AreEqual(ShopItemCategory.Tools, item.Category, item.Name);
            Assert.IsNotNull(item.Rewards, item.Name);
            foreach (ShopReward reward in item.Rewards)
                Assert.IsTrue(placedObjects.ContainsKey(reward.Guid), $"{item.Name}: missing {reward.Guid}");
        }

        Assert.Greater(toolItemCount, 0, "Expected at least one tool in the shop.");
    }
}
