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
    private const string CarrotSlicesPath = "Assets/SO/KitchenObjectSO/CarrotSlicesSO.asset";
    private const string PotConfigPath = "Assets/SO/CookingToolConfigSO/Pot.asset";
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
        };

        foreach (string path in paths)
        {
            CookingToolConfigSO config = Load<CookingToolConfigSO>(path);
            Assert.AreEqual(1, config.maxFoodSlots, path);
            Assert.IsFalse(config.requiresManualStart, $"{path} must keep auto-starting.");
        }
    }

    // The Pot is the pot that takes the two halves of a Stew, so it needs a second slot
    // and waits for the player to press start instead of cooking on the first ingredient.
    [Test] public void PotConfig_HoldsTwoIngredientsAndWaitsForStart()
    {
        CookingToolConfigSO config = Load<CookingToolConfigSO>(PotConfigPath);

        Assert.IsTrue(config.Supports(CookingToolConfigSO.CookingToolType.Combine), PotConfigPath);
        Assert.IsTrue(config.Supports(CookingToolConfigSO.CookingToolType.DeepFry), PotConfigPath);
        Assert.AreEqual(2, config.maxFoodSlots, PotConfigPath);
        Assert.IsTrue(config.requiresManualStart, PotConfigPath);
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

    // The Pot's Stew needs both halves inside: the multiset must match before a recipe
    // resolves, and the Pot must still accept the second ingredient while it can.
    [Test] public void CombineRecipes_ResolveOnlyWithTheWholeIngredientSet()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        database.Initialize();

        CombineRecipeSO stew = Load<CombineRecipeSO>(StewPath);
        KitchenObjectSO potato = Load<KitchenObjectSO>(ChoppedPotatoPath);
        KitchenObjectSO carrot = Load<KitchenObjectSO>(CarrotSlicesPath);

        Assert.AreEqual(2, stew.input.Count, "The Stew recipe must use two ingredients.");
        Assert.AreSame(stew, database.GetCombineRecipeForIngredients(new List<KitchenObjectSO> { potato, carrot }));
        Assert.AreSame(stew, database.GetCombineRecipeForIngredients(new List<KitchenObjectSO> { carrot, potato }),
            "The order the ingredients go into the pot must not matter.");

        Assert.IsNull(database.GetCombineRecipeForIngredients(new List<KitchenObjectSO> { potato }),
            "A two-ingredient Stew must not resolve with only the potato.");
        Assert.IsNull(database.GetCombineRecipeForIngredients(new List<KitchenObjectSO> { carrot }),
            "A two-ingredient Stew must not resolve with only the carrot.");
        Assert.IsNull(database.GetCombineRecipeForIngredients(new List<KitchenObjectSO> { potato, potato }),
            "No recipe uses two potatoes.");
        Assert.IsNull(database.GetCombineRecipeForIngredients(new List<KitchenObjectSO> { potato, carrot, carrot }),
            "Extra ingredients must not match a smaller recipe.");

        Assert.IsTrue(database.CanAcceptCombineIngredient(new List<KitchenObjectSO>(), potato));
        Assert.IsTrue(database.CanAcceptCombineIngredient(new List<KitchenObjectSO> { potato }, carrot));
        Assert.IsTrue(database.CanAcceptCombineIngredient(new List<KitchenObjectSO> { carrot }, potato));
        Assert.IsFalse(database.CanAcceptCombineIngredient(new List<KitchenObjectSO> { potato }, potato),
            "The Stew needs exactly one potato.");
        Assert.IsFalse(database.CanAcceptCombineIngredient(new List<KitchenObjectSO> { potato, carrot }, carrot),
            "A complete Stew must not take a third ingredient.");
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

    // -----------------------------------------------------------------------------------------
    // New Cooking Tools: Grill, Coffee Machine, Tea Machine, Soda Machine
    // -----------------------------------------------------------------------------------------

    private static readonly string[] NewToolPlacedPaths = new[]
    {
        "Assets/SO/BuildingSO/Counters/Tools/Grill.asset",
        "Assets/SO/BuildingSO/Counters/Tools/CoffeeMachine.asset",
        "Assets/SO/BuildingSO/Counters/Tools/TeaMachine.asset",
        "Assets/SO/BuildingSO/Counters/Tools/SodaMachine.asset"
    };

    private static readonly string[] NewToolConfigPaths = new[]
    {
        "Assets/SO/CookingToolConfigSO/Grill.asset",
        "Assets/SO/CookingToolConfigSO/CoffeeMachine.asset",
        "Assets/SO/CookingToolConfigSO/TeaMachine.asset",
        "Assets/SO/CookingToolConfigSO/SodaMachine.asset"
    };

    private static readonly string[] NewToolPrefabPaths = new[]
    {
        "Assets/Prefab/CookingTool/GrillCookingTool.prefab",
        "Assets/Prefab/CookingTool/CoffeeMachineCookingTool.prefab",
        "Assets/Prefab/CookingTool/TeaMachineCookingTool.prefab",
        "Assets/Prefab/CookingTool/SodaMachineCookingTool.prefab"
    };

    private static readonly string[] NewKitchenObjectPaths = new[]
    {
        "Assets/SO/KitchenObjectSO/CoffeeBean.asset",
        "Assets/SO/KitchenObjectSO/TeaLeaf.asset",
        "Assets/SO/KitchenObjectSO/Syrup.asset",
        "Assets/SO/KitchenObjectSO/Coffee.asset",
        "Assets/SO/KitchenObjectSO/Tea.asset",
        "Assets/SO/KitchenObjectSO/Soda.asset"
    };

    private static readonly string[] NewKitchenObjectPrefabPaths = new[]
    {
        "Assets/Prefab/KitchenObjects/food_ingredient_coffeebean.prefab",
        "Assets/Prefab/KitchenObjects/food_ingredient_tealeaf.prefab",
        "Assets/Prefab/KitchenObjects/food_ingredient_syrup.prefab",
        "Assets/Prefab/KitchenObjects/food_drink_coffee.prefab",
        "Assets/Prefab/KitchenObjects/food_drink_tea.prefab",
        "Assets/Prefab/KitchenObjects/food_drink_soda.prefab"
    };

    [Test] public void NewToolConfigs_MatchExpectedSettings()
    {
        CookingToolConfigSO grill = Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/Grill.asset");
        Assert.IsTrue(grill.Supports(CookingToolConfigSO.CookingToolType.Frying));
        Assert.IsFalse(grill.Supports(CookingToolConfigSO.CookingToolType.Beverage));
        Assert.IsFalse(grill.supportsOptionMenu);
        Assert.IsTrue(grill.supportsBurning, "Grill must support burning.");
        Assert.AreEqual(1, grill.maxFoodSlots);
        Assert.IsFalse(grill.requiresManualStart, "Grill is auto-start.");

        string[] machineConfigs = new[]
        {
            "Assets/SO/CookingToolConfigSO/CoffeeMachine.asset",
            "Assets/SO/CookingToolConfigSO/TeaMachine.asset",
            "Assets/SO/CookingToolConfigSO/SodaMachine.asset"
        };
        foreach (string path in machineConfigs)
        {
            CookingToolConfigSO cfg = Load<CookingToolConfigSO>(path);
            Assert.IsTrue(cfg.Supports(CookingToolConfigSO.CookingToolType.Beverage), path);
            Assert.IsFalse(cfg.Supports(CookingToolConfigSO.CookingToolType.Frying), path);
            Assert.IsFalse(cfg.supportsOptionMenu, path);
            Assert.IsFalse(cfg.requiresManualStart, $"{path} is auto-start.");
        }

        // Only the tea machine takes two ingredients (tea leaf + water).
        Assert.AreEqual(1, Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/CoffeeMachine.asset").maxFoodSlots);
        Assert.AreEqual(2, Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/TeaMachine.asset").maxFoodSlots,
            "The Tea Machine needs room for the leaf and the water.");
        Assert.AreEqual(1, Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/SodaMachine.asset").maxFoodSlots);

        // Only the tea machine models over-steeping: the finished tea goes bitter if it is
        // left inside instead of being taken out.
        CookingToolConfigSO tea = Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/TeaMachine.asset");
        Assert.IsTrue(tea.supportsBurning, "The Tea Machine must over-steep (burn) its tea.");
        Assert.IsFalse(Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/CoffeeMachine.asset").supportsBurning);
        Assert.IsFalse(Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/SodaMachine.asset").supportsBurning);
    }

    [Test] public void NewTools_GuidsMatchAssetGuids()
    {
        foreach (string path in NewToolPlacedPaths)
        {
            PlacedObjectTypeSO placed = Load<PlacedObjectTypeSO>(path);
            string assetGuid = AssetDatabase.AssetPathToGUID(path);
            Assert.IsFalse(string.IsNullOrEmpty(placed.Guid), path);
            Assert.AreEqual(assetGuid, placed.Guid, $"{path} _guid must match asset GUID.");
        }
    }

    [Test] public void NewTools_AreToolsOnExpectedCounters()
    {
        PlacedObjectTypeSO stove = Load<PlacedObjectTypeSO>("Assets/SO/BuildingSO/Counters/StoveCounter.asset");
        PlacedObjectTypeSO clear = Load<PlacedObjectTypeSO>("Assets/SO/BuildingSO/Counters/ClearCounter.asset");

        PlacedObjectTypeSO grill = Load<PlacedObjectTypeSO>("Assets/SO/BuildingSO/Counters/Tools/Grill.asset");
        Assert.IsTrue(grill.isTool);
        Assert.AreEqual(1, grill.width);
        Assert.AreEqual(1, grill.height);
        Assert.AreEqual(InventoryTabType.Tool, grill.itemType.TabType);
        Assert.IsNotNull(grill.allowedUnderlyingCounters);
        Assert.AreEqual(1, grill.allowedUnderlyingCounters.Count);
        Assert.AreSame(stove, grill.allowedUnderlyingCounters[0], "Grill sits on StoveCounter.");

        string[] machines = new[]
        {
            "Assets/SO/BuildingSO/Counters/Tools/CoffeeMachine.asset",
            "Assets/SO/BuildingSO/Counters/Tools/TeaMachine.asset",
            "Assets/SO/BuildingSO/Counters/Tools/SodaMachine.asset"
        };
        foreach (string path in machines)
        {
            PlacedObjectTypeSO tool = Load<PlacedObjectTypeSO>(path);
            Assert.IsTrue(tool.isTool, path);
            Assert.AreEqual(1, tool.width, path);
            Assert.AreEqual(1, tool.height, path);
            Assert.AreEqual(InventoryTabType.Tool, tool.itemType.TabType, path);
            Assert.IsNotNull(tool.allowedUnderlyingCounters, path);
            Assert.AreEqual(1, tool.allowedUnderlyingCounters.Count, path);
            Assert.AreSame(clear, tool.allowedUnderlyingCounters[0], $"{path} sits on ClearCounter.");
        }
    }

    [Test] public void PlacedObjectDatabase_ContainsAllNewTools()
    {
        ScriptableObject database = Load<ScriptableObject>(PlacedObjectDatabasePath);
        SerializedProperty placeObjects = new SerializedObject(database).FindProperty("placeObjects");

        foreach (string path in NewToolPlacedPaths)
        {
            PlacedObjectTypeSO tool = Load<PlacedObjectTypeSO>(path);
            bool found = false;
            for (int i = 0; i < placeObjects.arraySize; i++)
            {
                if (placeObjects.GetArrayElementAtIndex(i).objectReferenceValue == tool)
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, $"{path} missing from PlacedObjectDatabase.");
        }
    }

    [Test] public void DefaultNetworkPrefabs_ContainsAllNewToolsAndKitchenObjects()
    {
        ScriptableObject networkPrefabs = Load<ScriptableObject>(DefaultNetworkPrefabsPath);
        SerializedProperty list = new SerializedObject(networkPrefabs).FindProperty("List");

        List<string> allPaths = new List<string>(NewToolPrefabPaths);
        allPaths.AddRange(NewKitchenObjectPrefabPaths);

        foreach (string path in allPaths)
        {
            GameObject prefab = Load<GameObject>(path);
            bool found = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty p = list.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab");
                if (p != null && p.objectReferenceValue == prefab)
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, $"{path} missing from DefaultNetworkPrefabs.");
        }
    }

    [Test] public void NewToolPrefabs_HaveCookingToolAndConvexCollider()
    {
        foreach (string path in NewToolPrefabPaths)
        {
            GameObject prefab = Load<GameObject>(path);
            CookingTool tool = prefab.GetComponent<CookingTool>();
            Assert.IsNotNull(tool, $"{path} missing CookingTool component.");

            SerializedObject sTool = new SerializedObject(tool);
            Assert.IsNotNull(sTool.FindProperty("cookingToolConfig").objectReferenceValue,
                $"{path} has no cookingToolConfig assigned.");

            Transform placePoint = sTool.FindProperty("placePoint").objectReferenceValue as Transform;
            Assert.IsNotNull(placePoint, $"{path} has no placePoint assigned.");
            Assert.Greater(placePoint.localPosition.y, 0f, $"{path} placePoint must be above counter.");

            MeshCollider[] colliders = prefab.GetComponentsInChildren<MeshCollider>(true);
            Assert.Greater(colliders.Length, 0, $"{path} has no MeshCollider.");
            foreach (MeshCollider collider in colliders)
            {
                Assert.IsTrue(collider.convex, $"{collider.name} collider in {path} must be convex.");
                Assert.IsFalse(collider.isTrigger, $"{collider.name} in {path} should not be trigger.");
            }
        }
    }

    [Test] public void NewKitchenObjectPrefabs_AreFullyConfigured()
    {
        for (int i = 0; i < NewKitchenObjectPaths.Length; i++)
        {
            KitchenObjectSO so = Load<KitchenObjectSO>(NewKitchenObjectPaths[i]);
            GameObject prefab = Load<GameObject>(NewKitchenObjectPrefabPaths[i]);

            Assert.AreEqual(10, prefab.layer, $"{prefab.name} must be on layer 10 (KitchenObject).");
            Assert.AreSame(prefab.transform, so.prefab, $"{so.name}.prefab must point to {prefab.name}.");

            Unity.Netcode.NetworkObject netObj = prefab.GetComponent<Unity.Netcode.NetworkObject>();
            Assert.IsNotNull(netObj, $"{prefab.name} missing NetworkObject.");

            KitchenObject ko = prefab.GetComponent<KitchenObject>();
            Assert.IsNotNull(ko, $"{prefab.name} missing KitchenObject.");
            SerializedObject sKo = new SerializedObject(ko);
            Assert.AreSame(so, sKo.FindProperty("kitchenObjectSO").objectReferenceValue,
                $"{prefab.name} KitchenObject component must reference {so.name}.");

            Assert.IsNotNull(prefab.GetComponent<FollowTransform>(), $"{prefab.name} missing FollowTransform.");
        }
    }

    [Test] public void NewDrinkRecipes_ResolveToTheirDrinks()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        database.Initialize();

        KitchenObjectSO bean = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/CoffeeBean.asset");
        KitchenObjectSO leaf = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/TeaLeaf.asset");
        KitchenObjectSO syrup = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Syrup.asset");
        KitchenObjectSO water = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Water.asset");

        KitchenObjectSO coffee = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Coffee.asset");
        KitchenObjectSO tea = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Tea.asset");
        KitchenObjectSO soda = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Soda.asset");

        DrinkRecipeSO rCoffee = database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { bean });
        // Tea is brewed from the leaf AND water, in either order.
        DrinkRecipeSO rTea = database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { leaf, water });
        DrinkRecipeSO rSoda = database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { syrup });

        Assert.IsNotNull(rCoffee, "Coffee recipe not found for CoffeeBean.");
        Assert.AreSame(coffee, rCoffee.output);

        Assert.IsNotNull(rTea, "Tea recipe not found for TeaLeaf + Water.");
        Assert.AreSame(tea, rTea.output);
        Assert.AreSame(rTea, database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { water, leaf }),
            "Tea recipe must be order independent.");

        Assert.IsNotNull(rSoda, "Soda recipe not found for Syrup.");
        Assert.AreSame(soda, rSoda.output);

        Assert.AreSame(rCoffee, database.GetDrinkRecipeForOutput(coffee));
        Assert.AreSame(rTea, database.GetDrinkRecipeForOutput(tea));
        Assert.AreSame(rSoda, database.GetDrinkRecipeForOutput(soda));

        // Mixed/wrong ingredients must not resolve
        Assert.IsNull(database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { bean, leaf }));
        Assert.IsNull(database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { bean, bean }));
        Assert.IsNull(database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { leaf }),
            "Tea alone must not brew without water.");
        Assert.IsNull(database.GetDrinkRecipeForIngredients(new List<KitchenObjectSO> { leaf, leaf }),
            "Two leaves are not a tea recipe.");
    }

    [Test] public void ShopItems40To47_UseNewToolAndIngredientRewards()
    {
        ConfigShop shop = Load<ConfigShop>(ShopPath);

        // Tool rewards (items 40-43)
        var expectedTools = new (int id, string name, string path, ShopItemCategory cat, int price, int level)[]
        {
            (40, "Grill Tool", "Assets/SO/BuildingSO/Counters/Tools/Grill.asset", ShopItemCategory.Tools, 150, 2),
            (41, "Coffee Machine", "Assets/SO/BuildingSO/Counters/Tools/CoffeeMachine.asset", ShopItemCategory.Tools, 200, 3),
            (42, "Tea Machine", "Assets/SO/BuildingSO/Counters/Tools/TeaMachine.asset", ShopItemCategory.Tools, 190, 3),
            (43, "Soda Machine", "Assets/SO/BuildingSO/Counters/Tools/SodaMachine.asset", ShopItemCategory.Tools, 210, 4),
        };
        foreach (var def in expectedTools)
        {
            ConfigShopItem item = shop.GetById(def.id);
            Assert.IsNotNull(item, $"Missing shop item {def.id}");
            Assert.AreEqual(def.name, item.Name);
            Assert.AreEqual(ShopItemType.Item, item.Type);
            Assert.AreEqual(def.cat, item.Category);
            Assert.AreEqual(def.price, item.Price);
            Assert.AreEqual(def.level, item.UnlockLevel);
            Assert.AreEqual(1, item.Rewards.Count);
            PlacedObjectTypeSO placed = Load<PlacedObjectTypeSO>(def.path);
            Assert.AreEqual(placed.Guid, item.Rewards[0].Guid);
        }

        // Ingredient rewards (items 44-47)
        var expectedIngs = new (int id, string name, string path, ShopItemCategory cat, int price, int level)[]
        {
            (44, "Coffee Beans", "Assets/SO/KitchenObjectSO/CoffeeBean.asset", ShopItemCategory.Bakery, 15, 1),
            (45, "Tea Leaves", "Assets/SO/KitchenObjectSO/TeaLeaf.asset", ShopItemCategory.Vegetables, 15, 1),
            (46, "Syrup", "Assets/SO/KitchenObjectSO/Syrup.asset", ShopItemCategory.Dairy, 15, 1),
            (47, "Water", "Assets/SO/KitchenObjectSO/Water.asset", ShopItemCategory.Dairy, 10, 1),
        };
        foreach (var def in expectedIngs)
        {
            ConfigShopItem item = shop.GetById(def.id);
            Assert.IsNotNull(item, $"Missing shop item {def.id}");
            Assert.AreEqual(def.name, item.Name);
            Assert.AreEqual(ShopItemType.Ingredient, item.Type);
            Assert.AreEqual(def.cat, item.Category);
            Assert.AreEqual(def.price, item.Price);
            Assert.AreEqual(def.level, item.UnlockLevel);
            Assert.AreEqual(1, item.Rewards.Count);
            KitchenObjectSO ko = Load<KitchenObjectSO>(def.path);
            Assert.AreEqual(ko.Guid, item.Rewards[0].Guid);
        }
    }

    [Test] public void ConfigFood_ContainsNewDrinks()
    {
        ConfigFood cfg = Load<ConfigFood>("Assets/Configs/ConfigFood/ConfigFoodSO.asset");
        string[] drinkFoodNames = new[] { "Coffee", "Tea", "Soda" };

        foreach (string name in drinkFoodNames)
        {
            FoodSO food = cfg.FoodItems.Find(f => f != null && f.recipeName == name);
            Assert.IsNotNull(food, $"ConfigFood missing drink {name}.");
            Assert.AreEqual(FoodType.Drink, food.foodType);
            Assert.IsNotNull(food.kitchenObjectSOList);
            Assert.AreEqual(1, food.kitchenObjectSOList.Count);
            Assert.IsNotNull(food.kitchenObjectSOList[0]);
        }
    }

    [Test] public void BeverageMachines_RestrictTheirIngredients()
    {
        KitchenObjectSO bean = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/CoffeeBean.asset");
        KitchenObjectSO leaf = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/TeaLeaf.asset");
        KitchenObjectSO syrup = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Syrup.asset");
        KitchenObjectSO water = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Water.asset");

        CookingToolConfigSO coffee = Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/CoffeeMachine.asset");
        CookingToolConfigSO tea = Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/TeaMachine.asset");
        CookingToolConfigSO soda = Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/SodaMachine.asset");

        Assert.AreEqual(1, coffee.allowedIngredients.Count, "Coffee Machine must whitelist exactly one ingredient.");
        Assert.AreSame(bean, coffee.allowedIngredients[0]);
        Assert.IsTrue(coffee.AllowsIngredient(bean));
        Assert.IsFalse(coffee.AllowsIngredient(leaf));
        Assert.IsFalse(coffee.AllowsIngredient(syrup));

        // Tea is brewed from a leaf and water, so the machine accepts both (and nothing else).
        Assert.AreEqual(2, tea.allowedIngredients.Count, "Tea Machine must whitelist the leaf and the water.");
        CollectionAssert.Contains(tea.allowedIngredients, leaf);
        CollectionAssert.Contains(tea.allowedIngredients, water);
        Assert.IsTrue(tea.AllowsIngredient(leaf));
        Assert.IsTrue(tea.AllowsIngredient(water));
        Assert.IsFalse(tea.AllowsIngredient(bean));
        Assert.IsFalse(tea.AllowsIngredient(syrup));

        Assert.AreEqual(1, soda.allowedIngredients.Count, "Soda Machine must whitelist exactly one ingredient.");
        Assert.AreSame(syrup, soda.allowedIngredients[0]);
        Assert.IsTrue(soda.AllowsIngredient(syrup));
        Assert.IsFalse(soda.AllowsIngredient(bean));
        Assert.IsFalse(soda.AllowsIngredient(leaf));

        // The Blender keeps the unrestricted behaviour (empty whitelist).
        CookingToolConfigSO blender = Load<CookingToolConfigSO>("Assets/SO/CookingToolConfigSO/Blender.asset");
        Assert.AreEqual(0, blender.allowedIngredients.Count, "Blender must stay unrestricted.");
        Assert.IsTrue(blender.AllowsIngredient(bean));
        Assert.IsTrue(blender.AllowsIngredient(leaf));
        Assert.IsTrue(blender.AllowsIngredient(syrup));

        // The resolver honours the whitelist before it even looks at the recipe database.
        var resolver = new RecipeResolver();
        resolver.SetConfig(coffee);
        Assert.IsTrue(resolver.AllowsIngredient(bean));
        Assert.IsFalse(resolver.AllowsIngredient(leaf));

        resolver.SetConfig(blender);
        Assert.IsTrue(resolver.AllowsIngredient(bean));
        Assert.IsTrue(resolver.AllowsIngredient(leaf));
        Assert.IsTrue(resolver.AllowsIngredient(syrup));
    }

    [Test] public void Water_IsAnIngredientTheTeaMachineCanBrew()
    {
        KitchenObjectSO water = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Water.asset");
        Assert.IsNotNull(water, "Water SO is missing.");
        Assert.AreEqual("Water", water.objectName);

        GameObject prefab = Load<GameObject>("Assets/Prefab/KitchenObjects/food_ingredient_water.prefab");
        Assert.IsNotNull(prefab, "Water prefab is missing.");
        Assert.AreEqual(10, prefab.layer, "Water must be on the KitchenObject layer.");
        Assert.AreSame(prefab.transform, water.prefab, "Water SO must point at its prefab.");
        Assert.IsNotNull(prefab.GetComponent<Unity.Netcode.NetworkObject>(), "Water prefab needs a NetworkObject.");
        Assert.IsNotNull(prefab.GetComponent<FollowTransform>(), "Water prefab needs a FollowTransform.");

        KitchenObject kitchenObject = prefab.GetComponent<KitchenObject>();
        Assert.IsNotNull(kitchenObject);
        Assert.AreSame(water, new SerializedObject(kitchenObject).FindProperty("kitchenObjectSO").objectReferenceValue,
            "Water prefab must reference its own SO.");

        // The prefab has to be a registered network prefab, otherwise clients cannot spawn it.
        ScriptableObject networkPrefabs = Load<ScriptableObject>(DefaultNetworkPrefabsPath);
        SerializedProperty list = new SerializedObject(networkPrefabs).FindProperty("List");
        bool registered = false;
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty p = list.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab");
            if (p != null && p.objectReferenceValue == prefab)
            {
                registered = true;
                break;
            }
        }
        Assert.IsTrue(registered, "Water prefab missing from DefaultNetworkPrefabs.");
    }

    [Test] public void TeaMachine_OverSteepsIntoBitterTea()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        database.Initialize();

        KitchenObjectSO tea = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Tea.asset");
        KitchenObjectSO bitterTea = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/BitterTea.asset");

        Assert.IsNotNull(bitterTea, "BitterTea SO is missing.");
        Assert.AreEqual("Bitter Tea", bitterTea.objectName);

        GameObject prefab = Load<GameObject>("Assets/Prefab/KitchenObjects/food_drink_bittertea.prefab");
        Assert.AreEqual(10, prefab.layer, "Bitter Tea must be on the KitchenObject layer.");
        Assert.AreSame(prefab.transform, bitterTea.prefab, "BitterTea SO must point at its prefab.");
        Assert.IsNotNull(prefab.GetComponent<Unity.Netcode.NetworkObject>(), "Bitter Tea prefab needs a NetworkObject.");
        Assert.IsNotNull(prefab.GetComponent<FollowTransform>(), "Bitter Tea prefab needs a FollowTransform.");

        KitchenObject kitchenObject = prefab.GetComponent<KitchenObject>();
        Assert.IsNotNull(kitchenObject);
        Assert.AreSame(bitterTea, new SerializedObject(kitchenObject).FindProperty("kitchenObjectSO").objectReferenceValue,
            "Bitter Tea prefab must reference its own SO.");

        // Over-steeping is the burning recipe of the finished drink, not of the raw leaf.
        BurningRecipeSO burn = database.GetBurningRecipe(tea);
        Assert.IsNotNull(burn, "Tea needs a burning recipe so it can over-steep.");
        Assert.AreSame(bitterTea, burn.output, "Over-steeped tea must become Bitter Tea.");
        Assert.Greater(burn.burningTimerMax, 0f);
        Assert.IsNull(database.GetBurningRecipe(bitterTea), "Bitter Tea must not burn again.");

        ScriptableObject networkPrefabs = Load<ScriptableObject>(DefaultNetworkPrefabsPath);
        SerializedProperty list = new SerializedObject(networkPrefabs).FindProperty("List");
        bool registered = false;
        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty p = list.GetArrayElementAtIndex(i).FindPropertyRelative("Prefab");
            if (p != null && p.objectReferenceValue == prefab)
            {
                registered = true;
                break;
            }
        }
        Assert.IsTrue(registered, "Bitter Tea prefab missing from DefaultNetworkPrefabs.");
    }

    // -----------------------------------------------------------------------------------------
    // Regression guards for the grill / beverage-machine batch (2026-11-09).
    // ConfigFood keys FoodItemDic by FoodSO.Guid, so an empty _guid silently drops the dish out
    // of every GUID lookup (menu, orders, rewards). The cup is the only tableware that accepts
    // drinks, so a drink missing from its valid list can be brewed but never served.
    // -----------------------------------------------------------------------------------------

    private static readonly string[] AllFoodSOPaths = new[]
    {
        "Assets/SO/FoodSO/BaconBurger.asset",
        "Assets/SO/FoodSO/BakedPotatoes.asset",
        "Assets/SO/FoodSO/CheeseBurgerSO.asset",
        "Assets/SO/FoodSO/ChickenNuggetsMeal.asset",
        "Assets/SO/FoodSO/CoffeeDrink.asset",
        "Assets/SO/FoodSO/CorndogSpecial.asset",
        "Assets/SO/FoodSO/CroissantSnack.asset",
        "Assets/SO/FoodSO/GardenSalad.asset",
        "Assets/SO/FoodSO/GrilledSalmonPlate.asset",
        "Assets/SO/FoodSO/HamburgerFullSO.asset",
        "Assets/SO/FoodSO/HotDogMeal.asset",
        "Assets/SO/FoodSO/LoadedFries.asset",
        "Assets/SO/FoodSO/MilkshakeDrink.asset",
        "Assets/SO/FoodSO/OnionSaladSO.asset",
        "Assets/SO/FoodSO/OrangeJuiceDrink.asset",
        "Assets/SO/FoodSO/PeachSmoothieDrink.asset",
        "Assets/SO/FoodSO/RoastChickenMeal.asset",
        "Assets/SO/FoodSO/SaladSO.asset",
        "Assets/SO/FoodSO/SodaDrink.asset",
        "Assets/SO/FoodSO/SteakFrites.asset",
        "Assets/SO/FoodSO/StewSO.asset",
        "Assets/SO/FoodSO/TeaDrink.asset",
        "Assets/SO/FoodSO/VegetableHamBurgerFullSO.asset"
    };

    [Test] public void EveryFoodSO_HasGuidMatchingItsAsset()
    {
        foreach (string path in AllFoodSOPaths)
        {
            FoodSO food = Load<FoodSO>(path);
            string assetGuid = AssetDatabase.AssetPathToGUID(path);

            Assert.IsFalse(string.IsNullOrEmpty(food.Guid), $"{path} has an empty _guid; ConfigFood cannot key it.");
            Assert.AreEqual(assetGuid, food.Guid, $"{path} _guid must match the asset GUID.");
        }
    }

    [Test] public void ConfigFood_EveryEntryResolvesByItsGuid()
    {
        ConfigFood cfg = Load<ConfigFood>("Assets/Configs/ConfigFood/ConfigFoodSO.asset");
        cfg.Initialize();

        foreach (FoodSO food in cfg.FoodItems)
        {
            Assert.IsNotNull(food, "ConfigFood holds a null entry.");
            Assert.IsFalse(string.IsNullOrEmpty(food.Guid), $"{food.name} has an empty _guid.");
            Assert.IsTrue(cfg.FoodItemDic.ContainsKey(food.Guid),
                $"{food.name} is in foodItems but missing from FoodItemDic (duplicate or empty guid).");
            Assert.AreSame(food, cfg.FoodItemDic[food.Guid], $"{food.name} resolves to the wrong FoodSO.");
        }
    }

    private static bool CupAccepts(SerializedProperty valid, KitchenObjectSO drink)
    {
        for (int i = 0; i < valid.arraySize; i++)
        {
            if (valid.GetArrayElementAtIndex(i).objectReferenceValue == drink)
                return true;
        }
        return false;
    }

    [Test] public void Cup_AcceptsEveryDrinkKitchenObject()
    {
        string[] drinkKitchenObjectPaths = new[]
        {
            "Assets/SO/KitchenObjectSO/OrangeJuice.asset",
            "Assets/SO/KitchenObjectSO/Milkshake.asset",
            "Assets/SO/KitchenObjectSO/PeachSmoothie.asset",
            "Assets/SO/KitchenObjectSO/Coffee.asset",
            "Assets/SO/KitchenObjectSO/Tea.asset",
            "Assets/SO/KitchenObjectSO/Soda.asset"
        };

        GameObject cup = Load<GameObject>("Assets/Prefab/KitchenObjects/cup.prefab");
        TablewareKitchenObject tableware = cup.GetComponent<TablewareKitchenObject>();
        Assert.IsNotNull(tableware, "cup.prefab has no TablewareKitchenObject component.");

        SerializedProperty valid = new SerializedObject(tableware).FindProperty("validKitchenObjectSOList");
        Assert.IsNotNull(valid, "cup.prefab has no validKitchenObjectSOList.");

        foreach (string path in drinkKitchenObjectPaths)
        {
            KitchenObjectSO drink = Load<KitchenObjectSO>(path);
            Assert.IsTrue(CupAccepts(valid, drink),
                $"cup.prefab does not accept {drink.objectName} ({path}); it cannot be served.");
        }
    }

    [Test] public void EveryDrinkFoodSO_IsServableInACup()
    {
        ConfigFood cfg = Load<ConfigFood>("Assets/Configs/ConfigFood/ConfigFoodSO.asset");
        GameObject cup = Load<GameObject>("Assets/Prefab/KitchenObjects/cup.prefab");
        TablewareKitchenObject tableware = cup.GetComponent<TablewareKitchenObject>();
        Assert.IsNotNull(tableware, "cup.prefab has no TablewareKitchenObject component.");
        SerializedProperty valid = new SerializedObject(tableware).FindProperty("validKitchenObjectSOList");

        foreach (FoodSO food in cfg.FoodItems)
        {
            if (food == null || food.foodType != FoodType.Drink)
                continue;

            Assert.IsNotNull(food.kitchenObjectSOList, $"{food.name} has no kitchenObjectSOList.");
            Assert.AreEqual(1, food.kitchenObjectSOList.Count, $"{food.name} should list exactly one drink KitchenObject.");
            KitchenObjectSO drink = food.kitchenObjectSOList[0];
            Assert.IsNotNull(drink, $"{food.name} lists a null KitchenObject.");
            Assert.IsTrue(CupAccepts(valid, drink),
                $"Drink dish {food.name} uses {drink.objectName}, which cup.prefab rejects.");
        }
    }

    // -----------------------------------------------------------------------------------------
    // Dessert machines (Ice Cream Machine, Pancake Machine) and the dish-reachability invariant.
    // A dish is only servable if every ingredient in its list can be obtained: either bought
    // from the shop or produced by some recipe. An orphaned KitchenObjectSO (one no recipe
    // outputs and no shop item grants) makes its dish impossible to complete, silently.
    // -----------------------------------------------------------------------------------------

    private const string IceCreamMachinePath = "Assets/SO/BuildingSO/Counters/Tools/IceCreamMachine.asset";
    private const string IceCreamConfigPath = "Assets/SO/CookingToolConfigSO/IceCreamMachine.asset";
    private const string IceCreamPrefabPath = "Assets/Prefab/CookingTool/IceCreamMachineCookingTool.prefab";
    private const string PancakeMachinePath = "Assets/SO/BuildingSO/Counters/Tools/PancakeMachine.asset";
    private const string PancakeConfigPath = "Assets/SO/CookingToolConfigSO/PancakeMachine.asset";
    private const string PancakePrefabPath = "Assets/Prefab/CookingTool/PancakeMachineCookingTool.prefab";
    private const string IceCreamDrinkRecipePath = "Assets/SO/DrinkRecipeSO/Drink_MilkDough_IceCream.asset";
    private const string PancakeBakeRecipePath = "Assets/SO/BakingRecipeSO/Bake_Dough_Pancake.asset";

    [Test] public void DessertMachineConfigs_MatchExpectedSettings()
    {
        CookingToolConfigSO iceCream = Load<CookingToolConfigSO>(IceCreamConfigPath);
        Assert.IsTrue(iceCream.Supports(CookingToolConfigSO.CookingToolType.Beverage));
        Assert.IsFalse(iceCream.supportsOptionMenu);
        Assert.IsFalse(iceCream.supportsBurning, "Ice cream must not burn.");
        Assert.AreEqual(2, iceCream.maxFoodSlots, "Ice cream needs milk and dough.");
        Assert.IsTrue(iceCream.requiresManualStart, "The player presses start once both ingredients are in.");

        CookingToolConfigSO pancake = Load<CookingToolConfigSO>(PancakeConfigPath);
        Assert.IsTrue(pancake.Supports(CookingToolConfigSO.CookingToolType.Baking));
        Assert.IsFalse(pancake.supportsOptionMenu);
        Assert.IsTrue(pancake.supportsBurning, "A pancake left in the machine burns.");
        Assert.AreEqual(1, pancake.maxFoodSlots);
        Assert.IsFalse(pancake.requiresManualStart, "The pancake machine auto-starts.");
    }

    [Test] public void DessertMachines_GuidsMatchAssetGuids()
    {
        foreach (string path in new[] { IceCreamMachinePath, PancakeMachinePath })
        {
            PlacedObjectTypeSO placed = Load<PlacedObjectTypeSO>(path);
            Assert.IsFalse(string.IsNullOrEmpty(placed.Guid), path);
            Assert.AreEqual(AssetDatabase.AssetPathToGUID(path), placed.Guid, path);
        }
    }

    [Test] public void DessertMachines_AreToolsOnTheStove()
    {
        PlacedObjectTypeSO stove = Load<PlacedObjectTypeSO>("Assets/SO/BuildingSO/Counters/StoveCounter.asset");

        foreach (string path in new[] { IceCreamMachinePath, PancakeMachinePath })
        {
            PlacedObjectTypeSO tool = Load<PlacedObjectTypeSO>(path);
            Assert.IsTrue(tool.isTool, path);
            Assert.AreEqual(1, tool.width, path);
            Assert.AreEqual(1, tool.height, path);
            Assert.AreEqual(InventoryTabType.Tool, tool.itemType.TabType, path);
            Assert.AreEqual(1, tool.allowedUnderlyingCounters.Count, path);
            Assert.AreSame(stove, tool.allowedUnderlyingCounters[0], $"{path} sits on StoveCounter.");
        }
    }

    [Test] public void DessertMachinePrefabs_CarryCookingToolAndConfig()
    {
        var cases = new[]
        {
            new { Prefab = IceCreamPrefabPath, Config = IceCreamConfigPath, Name = "IceCreamMachineCookingTool" },
            new { Prefab = PancakePrefabPath, Config = PancakeConfigPath, Name = "PancakeMachineCookingTool" }
        };

        foreach (var c in cases)
        {
            GameObject prefab = Load<GameObject>(c.Prefab);
            Assert.AreEqual(c.Name, prefab.name, c.Prefab);

            CookingTool tool = prefab.GetComponent<CookingTool>();
            Assert.IsNotNull(tool, $"{c.Prefab} has no CookingTool.");
            Assert.IsNotNull(prefab.GetComponent<Unity.Netcode.NetworkObject>(), $"{c.Prefab} needs a NetworkObject.");

            SerializedProperty cfg = new SerializedObject(tool).FindProperty("cookingToolConfig");
            Assert.IsNotNull(cfg, $"{c.Prefab} has no cookingToolConfig field.");
            Assert.AreSame(Load<CookingToolConfigSO>(c.Config), cfg.objectReferenceValue,
                $"{c.Prefab} must point at {c.Config}.");
        }
    }

    [Test] public void DessertRecipes_ProduceTheDessertKitchenObjects()
    {
        KitchenObjectSO iceCream = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/IceCream.asset");
        KitchenObjectSO pancake = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Pancake.asset");
        KitchenObjectSO milk = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Milk.asset");
        KitchenObjectSO dough = Load<KitchenObjectSO>("Assets/SO/KitchenObjectSO/Dough.asset");

        DrinkRecipeSO iceCreamRecipe = Load<DrinkRecipeSO>(IceCreamDrinkRecipePath);
        CollectionAssert.Contains(iceCreamRecipe.input, milk, "Ice cream needs milk.");
        CollectionAssert.Contains(iceCreamRecipe.input, dough, "Ice cream needs dough.");
        Assert.AreSame(iceCream, iceCreamRecipe.output);
        Assert.Greater(iceCreamRecipe.drinkTimerMax, 0f);

        BakingRecipeSO pancakeRecipe = Load<BakingRecipeSO>(PancakeBakeRecipePath);
        Assert.AreSame(dough, pancakeRecipe.input);
        Assert.AreSame(pancake, pancakeRecipe.output);
        Assert.Greater(pancakeRecipe.bakingTimerMax, 0f);
    }

    [Test] public void DessertDishes_AreServableOnAPlate()
    {
        ConfigFood cfg = Load<ConfigFood>("Assets/Configs/ConfigFood/ConfigFoodSO.asset");
        GameObject plate = Load<GameObject>("Assets/Prefab/KitchenObjects/plate.prefab");
        TablewareKitchenObject tableware = plate.GetComponent<TablewareKitchenObject>();
        Assert.IsNotNull(tableware, "plate.prefab has no TablewareKitchenObject.");
        SerializedProperty valid = new SerializedObject(tableware).FindProperty("validKitchenObjectSOList");

        foreach (string name in new[] { "Ice Cream Sundae", "Pancake Stack" })
        {
            FoodSO food = cfg.FoodItems.Find(f => f != null && f.recipeName == name);
            Assert.IsNotNull(food, $"ConfigFood missing dessert {name}.");
            Assert.AreEqual(FoodType.Dessert, food.foodType, name);
            Assert.AreEqual(1, food.kitchenObjectSOList.Count, name);
            KitchenObjectSO item = food.kitchenObjectSOList[0];
            Assert.IsNotNull(item, name);

            bool found = false;
            for (int i = 0; i < valid.arraySize; i++)
            {
                if (valid.GetArrayElementAtIndex(i).objectReferenceValue == item) { found = true; break; }
            }
            Assert.IsTrue(found, $"Dessert {name} uses {item.objectName}, which plate.prefab rejects.");
        }
    }

    // The invariant that catches the whole class of "dish exists but can never be made" bugs.
    [Test] public void EveryDishIngredient_IsObtainable()
    {
        RecipeDatabaseSO database = Load<RecipeDatabaseSO>(RecipeDatabasePath);
        database.Initialize();
        ConfigShop shop = Load<ConfigShop>(ShopPath);

        // Everything a recipe can output.
        var produced = new HashSet<KitchenObjectSO>();
        foreach (var r in LoadAll<CuttingRecipeSO>("Assets/SO/CuttingRecipeSO")) if (r != null && r.output != null) produced.Add(r.output);
        foreach (var r in LoadAll<FryingRecipeSO>("Assets/SO/FryingRecipeSO")) if (r != null && r.output != null) produced.Add(r.output);
        foreach (var r in LoadAll<BakingRecipeSO>("Assets/SO/BakingRecipeSO")) if (r != null && r.output != null) produced.Add(r.output);
        foreach (var r in LoadAll<DeepFryRecipeSO>("Assets/SO/DeepFryRecipeSO")) if (r != null && r.output != null) produced.Add(r.output);
        foreach (var r in LoadAll<DrinkRecipeSO>("Assets/SO/DrinkRecipeSO")) if (r != null && r.output != null) produced.Add(r.output);
        foreach (var r in LoadAll<CombineRecipeSO>("Assets/SO/CombineRecipeSO")) if (r != null && r.output != null) produced.Add(r.output);

        // Everything the shop grants by KitchenObject GUID.
        var shopGuids = new HashSet<string>();
        foreach (var item in shop.ShopItems)
        {
            if (item?.Rewards == null) continue;
            foreach (var reward in item.Rewards)
                if (!string.IsNullOrEmpty(reward.Guid)) shopGuids.Add(reward.Guid);
        }

        var failures = new List<string>();
        foreach (FoodSO food in Load<ConfigFood>("Assets/Configs/ConfigFood/ConfigFoodSO.asset").FoodItems)
        {
            if (food == null || food.kitchenObjectSOList == null) continue;
            foreach (KitchenObjectSO ingredient in food.kitchenObjectSOList)
            {
                if (ingredient == null) { failures.Add($"{food.recipeName}: null ingredient"); continue; }
                if (produced.Contains(ingredient)) continue;
                if (!string.IsNullOrEmpty(ingredient.Guid) && shopGuids.Contains(ingredient.Guid)) continue;
                failures.Add($"{food.recipeName} needs {ingredient.objectName}, which no recipe produces and no shop item sells");
            }
        }

        Assert.IsEmpty(failures, "Unobtainable dish ingredients:\n" + string.Join("\n", failures));
    }

    private static List<T> LoadAll<T>(string folder) where T : Object
    {
        var result = new List<T>();
        foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) result.Add(asset);
        }
        return result;
    }
}
