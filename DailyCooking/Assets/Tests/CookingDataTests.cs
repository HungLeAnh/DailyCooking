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
    private const string FryerPath = "Assets/SO/BuildingSO/Counters/Tools/Fryer.asset";
    private const string FryerConfigPath = "Assets/SO/CookingToolConfigSO/Fryer.asset";
    private const string FryerPrefabPath = "Assets/Prefab/CookingTool/FryerCookingTool.prefab";
    private const string BlenderPath = "Assets/SO/BuildingSO/Counters/Tools/Blender.asset";
    private const string BlenderConfigPath = "Assets/SO/CookingToolConfigSO/Blender.asset";
    private const string BlenderPrefabPath = "Assets/Prefab/CookingTool/BlenderCookingTool.prefab";
    private const string PlacedObjectDatabasePath = "Assets/SO/PlacedObjectDatabase/PlacedObjectDatabase.asset";
    private const string DefaultNetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
    private const string RecipeDatabasePath = "Assets/SO/RecipeDatabase/RecipeDatabase.asset";
    private const string PotatoPath = "Assets/SO/KitchenObjectSO/PotatoSO.asset";
    private const string ChoppedPotatoPath = "Assets/SO/KitchenObjectSO/PotatoChoppedSO.asset";
    private const string MashedPotatoPath = "Assets/SO/KitchenObjectSO/PotatoMashedSO.asset";
    private const string StewPath = "Assets/SO/CombineRecipeSO/StewCombineRecipeSO.asset";
    private const string ShopPath = "Assets/Configs/ConfigShop/ConfigShopSO.asset";
    private const string FriedChickenPrefabPath = "Assets/Prefab/KitchenObjects/food_ingredient_friedchicken.prefab";

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
        Assert.IsTrue(config.supportsBurning);
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

    [Test] public void FryerConfig_SupportsDeepFryOnly()
    {
        CookingToolConfigSO config = Load<CookingToolConfigSO>(FryerConfigPath);

        Assert.IsTrue(config.Supports(CookingToolConfigSO.CookingToolType.DeepFry));
        Assert.IsFalse(config.Supports(CookingToolConfigSO.CookingToolType.Frying));
        Assert.IsFalse(config.Supports(CookingToolConfigSO.CookingToolType.Baking));
        Assert.IsFalse(config.supportsOptionMenu);
    }

    [Test] public void BlenderConfig_SupportsBeverageOnly()
    {
        CookingToolConfigSO config = Load<CookingToolConfigSO>(BlenderConfigPath);

        Assert.IsTrue(config.Supports(CookingToolConfigSO.CookingToolType.Beverage));
        Assert.IsFalse(config.Supports(CookingToolConfigSO.CookingToolType.DeepFry));
        Assert.IsFalse(config.Supports(CookingToolConfigSO.CookingToolType.Baking));
        Assert.IsFalse(config.supportsOptionMenu);
        Assert.IsFalse(config.supportsBurning, "The Blender must not burn its output.");
    }

    [Test] public void FriedChickenPrefab_UsesDrumstickModelAtLegSize()
    {
        GameObject prefab = Load<GameObject>(FriedChickenPrefabPath);
        MeshFilter meshFilter = prefab.GetComponentInChildren<MeshFilter>(true);

        Assert.IsNotNull(meshFilter, "Fried chicken prefab has no MeshFilter.");
        Assert.IsNotNull(meshFilter.sharedMesh, "Fried chicken prefab has no mesh.");
        Assert.AreEqual("Fried chicken B", meshFilter.sharedMesh.name);
        Assert.AreEqual(1.5f, prefab.transform.localScale.x, 0.001f);
        Assert.AreEqual(1.5f, prefab.transform.localScale.y, 0.001f);
        Assert.AreEqual(1.5f, prefab.transform.localScale.z, 0.001f);
    }

    [Test] public void Fryer_IsToolOnStove()
    {
        PlacedObjectTypeSO fryer = Load<PlacedObjectTypeSO>(FryerPath);

        Assert.IsTrue(fryer.isTool);
        Assert.AreEqual(1, fryer.width);
        Assert.AreEqual(1, fryer.height);
        Assert.IsNotNull(fryer.prefab);
        Assert.AreEqual("FryerCookingTool", fryer.prefab.name);
        Assert.AreEqual(1, fryer.allowedUnderlyingCounters.Count);
        Assert.AreEqual("StoveCounter", fryer.allowedUnderlyingCounters[0].name);
    }

    [Test] public void Blender_IsToolOnClearCounter()
    {
        PlacedObjectTypeSO blender = Load<PlacedObjectTypeSO>(BlenderPath);

        Assert.IsTrue(blender.isTool);
        Assert.AreEqual(1, blender.width);
        Assert.AreEqual(1, blender.height);
        Assert.IsNotNull(blender.prefab);
        Assert.AreEqual("BlenderCookingTool", blender.prefab.name);
        Assert.AreEqual(1, blender.allowedUnderlyingCounters.Count);
        Assert.AreEqual("ClearCounter", blender.allowedUnderlyingCounters[0].name);
    }

    [Test] public void PlacedObjectDatabase_ContainsFryerAndBlender()
    {
        PlacedObjectTypeSO fryer = Load<PlacedObjectTypeSO>(FryerPath);
        PlacedObjectTypeSO blender = Load<PlacedObjectTypeSO>(BlenderPath);
        ScriptableObject database = Load<ScriptableObject>(PlacedObjectDatabasePath);
        SerializedProperty placeObjects = new SerializedObject(database).FindProperty("placeObjects");
        bool foundFryer = false;
        bool foundBlender = false;

        for (int i = 0; i < placeObjects.arraySize; i++)
        {
            Object placedObject = placeObjects.GetArrayElementAtIndex(i).objectReferenceValue;
            if (placedObject == fryer) foundFryer = true;
            if (placedObject == blender) foundBlender = true;
        }

        Assert.IsTrue(foundFryer, "Fryer is missing from PlacedObjectDatabase.");
        Assert.IsTrue(foundBlender, "Blender is missing from PlacedObjectDatabase.");
    }

    [Test] public void DefaultNetworkPrefabs_ContainsFryerAndBlender()
    {
        GameObject fryerPrefab = Load<GameObject>(FryerPrefabPath);
        GameObject blenderPrefab = Load<GameObject>(BlenderPrefabPath);
        ScriptableObject networkPrefabs = Load<ScriptableObject>(DefaultNetworkPrefabsPath);
        SerializedProperty list = new SerializedObject(networkPrefabs).FindProperty("List");
        bool foundFryer = false;
        bool foundBlender = false;

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty prefab = list.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab");
            if (prefab != null && prefab.objectReferenceValue == fryerPrefab) foundFryer = true;
            if (prefab != null && prefab.objectReferenceValue == blenderPrefab) foundBlender = true;
        }

        Assert.IsTrue(foundFryer, "Fryer is missing from DefaultNetworkPrefabs.");
        Assert.IsTrue(foundBlender, "Blender is missing from DefaultNetworkPrefabs.");
    }

    [Test] public void FryerAndBlenderPrefabs_HaveCookingToolAndConvexCollider()
    {
        string[] prefabPaths = { FryerPrefabPath, BlenderPrefabPath };

        foreach (string path in prefabPaths)
        {
            GameObject prefab = Load<GameObject>(path);
            MeshCollider[] colliders = prefab.GetComponentsInChildren<MeshCollider>(true);

            Assert.IsNotNull(prefab.GetComponent<CookingTool>(), path);
            Assert.Greater(colliders.Length, 0, $"{path} has no MeshCollider.");
            foreach (MeshCollider collider in colliders)
                Assert.IsTrue(collider.convex, $"{path}: {collider.name} collider must be convex.");
        }
    }

    [Test] public void ShopItems38And39_UseNewToolRewards()
    {
        ConfigShop shop = Load<ConfigShop>(ShopPath);
        PlacedObjectTypeSO fryer = Load<PlacedObjectTypeSO>(FryerPath);
        PlacedObjectTypeSO blender = Load<PlacedObjectTypeSO>(BlenderPath);

        ConfigShopItem fryerItem = null;
        ConfigShopItem blenderItem = null;
        foreach (ConfigShopItem item in shop.ShopItems)
        {
            if (item == null) continue;
            if (item.Id == 38) fryerItem = item;
            if (item.Id == 39) blenderItem = item;
        }

        Assert.IsNotNull(fryerItem, "Missing shop item 38.");
        Assert.AreEqual("Fryer Tool", fryerItem.Name);
        Assert.AreEqual(ShopItemCategory.Tools, fryerItem.Category);
        Assert.AreEqual(ShopItemType.Item, fryerItem.Type);
        Assert.AreEqual(250, fryerItem.Price);
        Assert.AreEqual(3, fryerItem.UnlockLevel);
        Assert.AreEqual(fryer.Guid, fryerItem.Rewards[0].Guid);

        Assert.IsNotNull(blenderItem, "Missing shop item 39.");
        Assert.AreEqual("Blender Tool", blenderItem.Name);
        Assert.AreEqual(ShopItemCategory.Tools, blenderItem.Category);
        Assert.AreEqual(ShopItemType.Item, blenderItem.Type);
        Assert.AreEqual(220, blenderItem.Price);
        Assert.AreEqual(2, blenderItem.UnlockLevel);
        Assert.AreEqual(blender.Guid, blenderItem.Rewards[0].Guid);
    }

    [Test] public void RecipeDatabase_DeepFryAndDrinkRecipesAreComplete()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        SerializedObject serializedDatabase = new SerializedObject(database);
        database.Initialize();

        SerializedProperty deepFryRecipes = serializedDatabase.FindProperty("deepFryRecipes");
        Assert.GreaterOrEqual(deepFryRecipes.arraySize, 4, "Expected all deep-fry recipes in the database.");
        for (int i = 0; i < deepFryRecipes.arraySize; i++)
        {
            DeepFryRecipeSO recipe = deepFryRecipes.GetArrayElementAtIndex(i).objectReferenceValue as DeepFryRecipeSO;
            Assert.IsNotNull(recipe, $"Deep-fry recipe {i} is null.");
            Assert.IsNotNull(recipe.input, $"{recipe.name} has no input.");
            Assert.IsNotNull(recipe.output, $"{recipe.name} has no output.");
            Assert.Greater(recipe.deepFryTimerMax, 0f, $"{recipe.name} has no timer.");
            Assert.AreSame(recipe, database.GetDeepFryRecipe(recipe.input), recipe.name);
            Assert.IsNotNull(database.GetBurningRecipe(recipe.output), $"{recipe.name}: output has no burning recipe.");
        }

        SerializedProperty drinkRecipes = serializedDatabase.FindProperty("drinkRecipes");
        Assert.GreaterOrEqual(drinkRecipes.arraySize, 3, "Expected all drink recipes in the database.");
        for (int i = 0; i < drinkRecipes.arraySize; i++)
        {
            DrinkRecipeSO recipe = drinkRecipes.GetArrayElementAtIndex(i).objectReferenceValue as DrinkRecipeSO;
            Assert.IsNotNull(recipe, $"Drink recipe {i} is null.");
            Assert.IsNotNull(recipe.input, $"{recipe.name} has no input list.");
            Assert.Greater(recipe.input.Count, 0, $"{recipe.name} has no ingredients.");
            Assert.IsNotNull(recipe.output, $"{recipe.name} has no output.");
            Assert.Greater(recipe.drinkTimerMax, 0f, $"{recipe.name} has no timer.");
            Assert.AreSame(recipe, database.GetDrinkRecipeByIngredient(recipe.input[0]), recipe.name);
        }
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
