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
    private const string MilkPath = "Assets/SO/KitchenObjectSO/Milk.asset";
    private const string OrangePath = "Assets/SO/KitchenObjectSO/Orange.asset";
    private const string PeachPath = "Assets/SO/KitchenObjectSO/Peach.asset";
    private const string MilkDrinkPath = "Assets/SO/DrinkRecipeSO/Drink_Milk_Milkshake.asset";
    private const string OrangeJuiceDrinkPath = "Assets/SO/DrinkRecipeSO/Drink_Orange_OrangeJuice.asset";
    private const string PeachSmoothieDrinkPath = "Assets/SO/DrinkRecipeSO/Drink_PeachOrange_PeachSmoothie.asset";
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

    [Test] public void BlenderConfig_HoldsMultipleIngredients()
    {
        CookingToolConfigSO config = Load<CookingToolConfigSO>(BlenderConfigPath);

        Assert.GreaterOrEqual(config.maxFoodSlots, 2,
            "The Blender must be able to hold more than one ingredient.");
        Assert.IsTrue(config.requiresManualStart,
            "The Blender must wait for the player to press start.");
    }

    [Test] public void OtherToolConfigs_StaySingleSlot()
    {
        string[] paths =
        {
            OvenConfigPath,
            FryerConfigPath,
            "Assets/SO/CookingToolConfigSO/Pan.asset",
            "Assets/SO/CookingToolConfigSO/CuttingBoard.asset",
            "Assets/SO/CookingToolConfigSO/Pot.asset",
        };

        foreach (string path in paths)
        {
            CookingToolConfigSO config = Load<CookingToolConfigSO>(path);
            Assert.AreEqual(1, config.maxFoodSlots, path);
            Assert.IsFalse(config.requiresManualStart, $"{path} must keep auto-starting.");
        }
    }

    [Test] public void RecipeDatabase_IngredientsMatch_IgnoresOrderAndCountsDuplicates()
    {
        KitchenObjectSO milk = ScriptableObject.CreateInstance<KitchenObjectSO>();
        KitchenObjectSO orange = ScriptableObject.CreateInstance<KitchenObjectSO>();
        KitchenObjectSO peach = ScriptableObject.CreateInstance<KitchenObjectSO>();
        try
        {
            List<KitchenObjectSO> recipe = new List<KitchenObjectSO> { milk, orange };

            Assert.IsTrue(RecipeDatabaseSO.IngredientsMatch(recipe, new List<KitchenObjectSO> { orange, milk }));
            Assert.IsFalse(RecipeDatabaseSO.IngredientsMatch(recipe, new List<KitchenObjectSO> { milk }));
            Assert.IsFalse(RecipeDatabaseSO.IngredientsMatch(recipe, new List<KitchenObjectSO> { milk, peach }));

            Assert.IsTrue(RecipeDatabaseSO.IngredientsMatch(
                new List<KitchenObjectSO> { milk, milk },
                new List<KitchenObjectSO> { milk, milk }));
            Assert.IsFalse(RecipeDatabaseSO.IngredientsMatch(
                new List<KitchenObjectSO> { milk, milk },
                new List<KitchenObjectSO> { milk, orange }));

            Assert.IsTrue(RecipeDatabaseSO.IsSubset(recipe, new List<KitchenObjectSO> { milk }));
            Assert.IsFalse(RecipeDatabaseSO.IsSubset(recipe, new List<KitchenObjectSO> { peach }));
        }
        finally
        {
            Object.DestroyImmediate(milk);
            Object.DestroyImmediate(orange);
            Object.DestroyImmediate(peach);
        }
    }

    [Test] public void DrinkRecipes_ResolveOnlyWithTheirExactIngredientSet()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        database.Initialize();

        DrinkRecipeSO milkshake = Load<DrinkRecipeSO>(MilkDrinkPath);
        DrinkRecipeSO orangeJuice = Load<DrinkRecipeSO>(OrangeJuiceDrinkPath);
        DrinkRecipeSO peachSmoothie = Load<DrinkRecipeSO>(PeachSmoothieDrinkPath);
        KitchenObjectSO milk = Load<KitchenObjectSO>(MilkPath);
        KitchenObjectSO orange = Load<KitchenObjectSO>(OrangePath);
        KitchenObjectSO peach = Load<KitchenObjectSO>(PeachPath);

        // Exact sets resolve, and the order the ingredients were added does not matter.
        Assert.AreSame(milkshake, database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { milk }));
        Assert.AreSame(orangeJuice, database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { orange }));
        Assert.AreSame(peachSmoothie, database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { peach, orange }));
        Assert.AreSame(peachSmoothie, database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { orange, peach }),
            "Drink recipes must not depend on the order the ingredients were added.");

        // Incomplete or wrong sets must not resolve to a recipe.
        Assert.IsNull(database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { peach }));
        Assert.IsNull(database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { milk, orange }),
            "A one-ingredient recipe must not run when a second ingredient is also inside the tool.");
        Assert.IsNull(database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { orange, orange }),
            "No recipe uses two oranges.");
        Assert.IsNull(database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { peach, orange, orange }),
            "Extra ingredients must not match a smaller recipe.");

        // The blender keeps accepting ingredients until a recipe is complete.
        Assert.IsTrue(database.CanAcceptDrinkIngredient(new List<KitchenObjectSO>(), milk));
        Assert.IsTrue(database.CanAcceptDrinkIngredient(new List<KitchenObjectSO>(), orange));
        Assert.IsTrue(database.CanAcceptDrinkIngredient(new List<KitchenObjectSO> { orange }, peach));
        Assert.IsTrue(database.CanAcceptDrinkIngredient(new List<KitchenObjectSO> { peach }, orange));
        Assert.IsFalse(database.CanAcceptDrinkIngredient(new List<KitchenObjectSO> { orange }, orange),
            "No recipe uses two oranges.");
        Assert.IsFalse(database.CanAcceptDrinkIngredient(new List<KitchenObjectSO> { milk }, orange));
        Assert.IsFalse(database.CanAcceptDrinkIngredient(new List<KitchenObjectSO> { milk }, milk),
            "A single-input recipe must not accept a second ingredient.");
        Assert.IsFalse(database.CanAcceptDrinkIngredient(new List<KitchenObjectSO> { peach, orange }, milk),
            "A complete two-ingredient drink must not take a third ingredient.");
    }

    // Menus look a drink up by its finished item, so the reverse lookup must find the
    // recipe that produced it (and return the raw ingredients, not the drink again).
    [Test] public void DrinkRecipes_OutputLookupReturnsTheRecipeThatMakesTheDrink()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        database.Initialize();

        DrinkRecipeSO milkshake = Load<DrinkRecipeSO>(MilkDrinkPath);
        DrinkRecipeSO orangeJuice = Load<DrinkRecipeSO>(OrangeJuiceDrinkPath);
        DrinkRecipeSO peachSmoothie = Load<DrinkRecipeSO>(PeachSmoothieDrinkPath);

        Assert.AreSame(milkshake, database.GetDrinkRecipeForOutput(milkshake.output));
        Assert.AreSame(orangeJuice, database.GetDrinkRecipeForOutput(orangeJuice.output));
        Assert.AreSame(peachSmoothie, database.GetDrinkRecipeForOutput(peachSmoothie.output));

        foreach (DrinkRecipeSO recipe in new[] { milkshake, orangeJuice, peachSmoothie })
        {
            DrinkRecipeSO found = database.GetDrinkRecipeForOutput(recipe.output);
            Assert.IsNotNull(found);
            Assert.IsNotNull(found.input);
            Assert.Greater(found.input.Count, 0);
            CollectionAssert.DoesNotContain(found.input, recipe.output,
                $"{recipe.name} must list its ingredients, not the finished drink.");
        }

        // Ingredients are not outputs, so they must not resolve to a drink recipe.
        Assert.IsNull(database.GetDrinkRecipeForOutput(Load<KitchenObjectSO>(MilkPath)));
        Assert.IsNull(database.GetDrinkRecipeForOutput(Load<KitchenObjectSO>(OrangePath)));
        Assert.IsNull(database.GetDrinkRecipeForOutput(null));
    }

    // A multi-ingredient recipe must never resolve from only some of its ingredients.
    [Test] public void DrinkRecipes_MultiInputRecipesNeverResolveFromAPartialSet()
    {
        string[] guids = AssetDatabase.FindAssets("t:DrinkRecipeSO");
        Assert.Greater(guids.Length, 0, "No drink recipes found.");

        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        database.Initialize();

        foreach (string guid in guids)
        {
            DrinkRecipeSO recipe = AssetDatabase.LoadAssetAtPath<DrinkRecipeSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (recipe == null || recipe.input == null || recipe.input.Count < 2)
                continue;

            for (int skip = 0; skip < recipe.input.Count; skip++)
            {
                List<KitchenObjectSO> partial = new List<KitchenObjectSO>(recipe.input);
                partial.RemoveAt(skip);
                Assert.AreNotSame(recipe, database.GetDrinkRecipeForIngredients(partial),
                    $"{recipe.name} must not resolve from a partial set (missing '{recipe.input[skip].name}').");
            }
        }
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
            Assert.AreSame(recipe, database.GetDrinkRecipeForIngredients(recipe.input), recipe.name);
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

    [Test] public void FoodSlot_KeepsSlotIndexesWhenAnItemLeaves()
    {
        GameObject root = new GameObject("FoodSlotTest");
        try
        {
            KitchenObject first = root.AddComponent<KitchenObject>();
            KitchenObject second = root.AddComponent<KitchenObject>();
            FoodSlot slot = new FoodSlot();

            slot.Set(first, 0);
            slot.Set(second, 1);

            Assert.AreEqual(2, slot.FoodCount);
            Assert.AreSame(first, slot.Current);
            Assert.AreEqual(-1, slot.FirstFreeIndex(2), "A full tool has no free slot.");

            slot.ClearAt(0);

            Assert.AreSame(second, slot.Get(1), "Removing slot 0 must not shift the item in slot 1.");
            Assert.AreSame(second, slot.Current);
            Assert.AreEqual(0, slot.FirstFreeIndex(2));
            Assert.AreEqual(1, slot.CopyFoods().Count);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
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
