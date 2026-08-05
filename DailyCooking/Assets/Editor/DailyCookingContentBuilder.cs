// DailyCookingContentBuilder.cs
// Editor-time bulk builder for the "Oven + Fast-Food" content batch.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using Object = UnityEngine.Object;

public static class DailyCookingContentBuilder
{
    private const string PACK_ROOT = "Assets/Ultimate Food Pack collection/Prefabs";
    private const string KITCHENOBJ_SO_DIR = "Assets/SO/KitchenObjectSO";
    private const string KITCHENOBJ_PREFAB_DIR = "Assets/Prefab/KitchenObjects";
    private const string NETWORK_PREFABS_ASSET = "Assets/DefaultNetworkPrefabs.asset";
    private const string MAIN_MENU_SCENE = "Assets/Scenes/MainMenuScene.unity";
    private const string CONFIG_FOOD_ASSET = "Assets/Configs/ConfigFood/ConfigFoodSO.asset";
    private const string CONFIG_SHOP_ASSET = "Assets/Configs/ConfigShop/ConfigShopSO.asset";
    private const string FOOD_PREFAB_DIR = "Assets/Prefab/Foods";
    private const string PLACEHOLDER_ICON = "Assets/_Assets/Icons/Ingredients/food_ingredient.png";
    private const string FOOD_SO_DIR = "Assets/SO/FoodSO";

    private static void Log(string msg) { Debug.Log("[ContentBuilder] " + msg); }

    private static void EnsureDir(string path)
    {
        string dir = Path.GetDirectoryName(path.Replace('\\', '/'));
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    private static KitchenObjectSO CreateKitchenObjectSO(string soName)
    {
        string assetPath = Path.Combine(KITCHENOBJ_SO_DIR, soName + ".asset").Replace('\\', '/');
        EnsureDir(assetPath);
        KitchenObjectSO existing = AssetDatabase.LoadAssetAtPath<KitchenObjectSO>(assetPath);
        if (existing != null) return existing;
        KitchenObjectSO so = ScriptableObject.CreateInstance<KitchenObjectSO>();
        so.name = soName;
        AssetDatabase.CreateAsset(so, assetPath);
        EditorUtility.SetDirty(so);
        Log("  created SO: " + so.name);
        return so;
    }

    private static void SetKitchenObjectSOField(GameObject prefabAsset, KitchenObjectSO so)
    {
        if (prefabAsset == null) return;
        var ko = prefabAsset.GetComponent<KitchenObject>();
        if (ko == null) ko = prefabAsset.AddComponent<KitchenObject>();
        var sObj = new SerializedObject(ko);
        var p = sObj.FindProperty("kitchenObjectSO");
        if (p != null) p.objectReferenceValue = so;
        sObj.ApplyModifiedProperties();
        EditorUtility.SetDirty(prefabAsset);
    }

    private static void SetKitchenObjectSOField(KitchenObjectSO so, Transform prefabTransform, Sprite sprite, string objectName)
    {
        var sObj = new SerializedObject(so);
        var pp = sObj.FindProperty("prefab");
        if (pp != null) pp.objectReferenceValue = prefabTransform;
        var sp = sObj.FindProperty("Sprite");
        if (sp != null) sp.objectReferenceValue = sprite;
        var op = sObj.FindProperty("objectName");
        if (op != null) op.stringValue = objectName;
        sObj.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
    }

    private static GameObject CreateKitchenObjectPrefab(KitchenObjectSO so, string packRelativePath, string prefFileName)
    {
        string outPath = Path.Combine(KITCHENOBJ_PREFAB_DIR, prefFileName).Replace('\\', '/');
        EnsureDir(outPath);
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(outPath);
        if (existing != null)
        {
            SetKitchenObjectSOField(existing, so);
            return existing;
        }
        string srcPath = Path.Combine(PACK_ROOT, packRelativePath).Replace('\\', '/');
        GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
        if (src == null) { Log("  MISSING pack prefab: " + srcPath); return null; }
        GameObject clone = (GameObject)PrefabUtility.InstantiatePrefab(src);
        if (clone == null) clone = (GameObject)Object.Instantiate(src);
        clone.name = prefFileName.Replace(".prefab", "");
        SetLayerRecursive(clone, 10);
        foreach (var col in clone.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(col);
        var netObj = clone.GetComponent<NetworkObject>() ?? clone.AddComponent<NetworkObject>();
        var ko = clone.GetComponent<KitchenObject>() ?? clone.AddComponent<KitchenObject>();
        if (clone.GetComponent<FollowTransform>() == null) clone.AddComponent<FollowTransform>();
        SetKitchenObjectSOField(clone, so);
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(clone, outPath);
        Object.DestroyImmediate(clone);
        return prefabAsset;
    }

    private static void RegisterNetworkPrefab(GameObject prefabAsset)
    {
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefabAsset));
        var obj = AssetDatabase.LoadAssetAtPath<Object>(NETWORK_PREFABS_ASSET);
        if (obj == null) { Log("  network prefabs asset NULL"); return; }
        var so = new SerializedObject(obj);
        var list = so.FindProperty("List");
        if (list == null) { Log("  could not find 'List' property on network prefabs asset"); return; }
        bool present = false;
        for (int i = 0; i < list.arraySize; i++)
        {
            var elem = list.GetArrayElementAtIndex(i);
            var p = elem.FindPropertyRelative("Prefab");
            if (p != null && p.objectReferenceValue == prefabAsset) { present = true; break; }
        }
        if (!present)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            var last = list.GetArrayElementAtIndex(list.arraySize - 1);
            var prefProp = last.FindPropertyRelative("Prefab");
            if (prefProp != null) prefProp.objectReferenceValue = prefabAsset;
            var ov = last.FindPropertyRelative("Override");
            if (ov != null) ov.intValue = 0;
        }
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Log("  network prefab registered: " + prefabAsset.name + " (" + guid + ")");
    }

    private static void AppendSceneList(KitchenObjectSO so)
    {
        if (!EditorSceneManager.GetSceneByPath(MAIN_MENU_SCENE).isLoaded)
            EditorSceneManager.OpenScene(MAIN_MENU_SCENE, OpenSceneMode.Single);
        var kgm = GameObject.FindObjectOfType<KitchenGameManager>();
        if (kgm == null) { Log("  ERROR: KitchenGameManager not found in scene"); return; }
        var soObj = new SerializedObject(kgm);
        var list = soObj.FindProperty("kitchenObjectSOList");
        if (list == null) { Log("  ERROR: could not find kitchenObjectSOList property"); return; }
        bool present = false;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == so) { present = true; break; }
        if (!present)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = so;
        }
        soObj.ApplyModifiedProperties();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Log("  scene KitchenGameManager list: added " + so.name);
    }

    private static GameObject GetTableware(string name) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(KITCHENOBJ_PREFAB_DIR, name + ".prefab").Replace('\\', '/'));

    private static GameObject CreateTablewarePrefab(KitchenObjectSO so, string displayName, string prefabName)
    {
        string outPath = Path.Combine(KITCHENOBJ_PREFAB_DIR, prefabName + ".prefab").Replace('\\', '/');
        EnsureDir(outPath);
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(outPath);
        if (existing != null)
        {
            SetKitchenObjectSOField(existing, so);
            return existing;
        }
        string srcPath = "Assets/Prefab/KitchenObjects/bowl.prefab";
        GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
        if (src == null) { Log("  ERROR: bowl source null"); return null; }
        GameObject clone = (GameObject)PrefabUtility.InstantiatePrefab(src);
        if (clone == null) clone = (GameObject)Object.Instantiate(src);
        clone.name = prefabName;
        SetLayerRecursive(clone, 10);
        var tko = clone.GetComponent<TablewareKitchenObject>();
        if (tko == null) tko = clone.AddComponent<TablewareKitchenObject>();
        SetKitchenObjectSOField(clone, so);
        EditorUtility.SetDirty(clone);
        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(clone, outPath);
        Object.DestroyImmediate(clone);
        return prefabAsset;
    }

    private static void AppendTablewareValidList(GameObject tablewarePrefab, KitchenObjectSO so)
    {
        if (tablewarePrefab == null || so == null) return;
        var components = tablewarePrefab.GetComponents<Component>();
        bool applied = false;
        foreach (var comp in components)
        {
            if (comp == null) continue;
            var sObj = new SerializedObject(comp);
            var validProp = sObj.FindProperty("validKitchenObjectSOList");
            if (validProp == null) { sObj.Dispose(); continue; }
            bool present = false;
            for (int i = 0; i < validProp.arraySize; i++)
                if (validProp.GetArrayElementAtIndex(i).objectReferenceValue == so) { present = true; break; }
            if (!present)
            {
                validProp.InsertArrayElementAtIndex(validProp.arraySize);
                validProp.GetArrayElementAtIndex(validProp.arraySize - 1).objectReferenceValue = so;
                sObj.ApplyModifiedProperties();
                EditorUtility.SetDirty(tablewarePrefab);
                applied = true;
            }
            sObj.Dispose();
        }
        if (applied)
        {
            AssetDatabase.SaveAssets();
            Log("  added " + so.name + " to " + tablewarePrefab.name + " valid list (via component)");
        }
    }

    public struct KitchenObjectDef
    {
        public string soName;
        public string displayName;
        public string packRelativePath;
        public bool isTableware;
        public bool addToPlateValidList;
        public bool addToCupValidList;
        public KitchenObjectDef(string s, string d, string p, bool tw, bool plate, bool cup)
        { soName = s; displayName = d; packRelativePath = p; isTableware = tw; addToPlateValidList = plate; addToCupValidList = cup; }
    }

    private static KitchenObjectDef[] DefaultDefs() => new KitchenObjectDef[]
    {
        new KitchenObjectDef("Dough","Dough","Pastry, Sweets & Desserts\\Bread loaf Half.prefab",false,false,false),
        new KitchenObjectDef("Croissant","Croissant","Pastry, Sweets & Desserts\\Croissant.prefab",false,true,false),
        new KitchenObjectDef("ChickenBreast","Chicken Breast","Meat & Seafood\\Chicken Breast.prefab",false,false,false),
        new KitchenObjectDef("RoastChicken","Roast Chicken","Meat & Seafood\\Chicken Roasted.prefab",false,true,false),
        new KitchenObjectDef("BeefSteak","Beef Steak","Meat & Seafood\\Beef Steak.prefab",false,false,false),
        new KitchenObjectDef("CookedSteak","Cooked Steak","Meat & Seafood\\Beef Steak cooked.prefab",false,true,false),
        new KitchenObjectDef("Salmon","Salmon","Meat & Seafood\\Salmon.prefab",false,false,false),
        new KitchenObjectDef("CookedSalmon","Grilled Salmon","Meat & Seafood\\Salmon fillet.prefab",false,true,false),
        new KitchenObjectDef("BakedPotato","Baked Potato","Fruits & Vegetables\\Potato half.prefab",false,true,false),
        new KitchenObjectDef("PotatoSlices","Potato Slices","Fruits & Vegetables\\Potato sliced.prefab",false,false,false),
        new KitchenObjectDef("FrenchFries","French Fries","Fast Food & Drinks\\French Fries A.prefab",false,true,false),
        new KitchenObjectDef("CorndogRaw","Corn Dog (Raw)","Fast Food & Drinks\\Corn dog A .prefab",false,false,false),
        new KitchenObjectDef("CorndogCooked","Corn Dog (Cooked)","Fast Food & Drinks\\Corn dog B.prefab",false,true,false),
        new KitchenObjectDef("ChickenLeg","Chicken Leg","Meat & Seafood\\Chicken Drumstick.prefab",false,false,false),
        new KitchenObjectDef("FriedChicken","Fried Chicken","Fast Food & Drinks\\Fried chicken C.prefab",false,true,false),
        new KitchenObjectDef("BaconRaw","Bacon (Raw)","Fast Food & Drinks\\Bacon A.prefab",false,false,false),
        new KitchenObjectDef("BaconCooked","Bacon (Cooked)","Fast Food & Drinks\\Bacon B.prefab",false,true,false),
        new KitchenObjectDef("Cucumber","Cucumber","Fruits & Vegetables\\Cucumber.prefab",false,false,false),
        new KitchenObjectDef("CucumberSlice","Cucumber Slice","Fruits & Vegetables\\Cucumber slice.prefab",false,true,false),
        new KitchenObjectDef("BellPepperRed","Bell Pepper (Red)","Fast Food & Drinks\\Paprika Red.prefab",false,false,false),
        new KitchenObjectDef("BellPepperSlice","Bell Pepper Slice","Fruits & Vegetables\\Bell pepper red slice.prefab",false,true,false),
        new KitchenObjectDef("Orange","Orange","Fruits & Vegetables\\Orange.prefab",false,false,false),
        new KitchenObjectDef("Milk","Milk","Fast Food & Drinks\\Milk A.prefab",false,false,false),
        new KitchenObjectDef("Peach","Peach","Fruits & Vegetables\\Peach.prefab",false,false,false),
        new KitchenObjectDef("OrangeJuice","Orange Juice","Fast Food & Drinks\\Orange juice.prefab",false,false,true),
        new KitchenObjectDef("Milkshake","Milkshake","Fast Food & Drinks\\Milk shake A.prefab",false,false,true),
        new KitchenObjectDef("PeachSmoothie","Peach Smoothie","Fast Food & Drinks\\Smoothie Peach .prefab",false,false,true),
        new KitchenObjectDef("HotDog","Hot Dog","Fast Food & Drinks\\Hot Dog Cooked.prefab",false,true,false),
        new KitchenObjectDef("Cup","Cup","bowl.prefab",true,false,true),
    };

    private static KitchenObjectSO BuildKitchenObject(KitchenObjectDef def)
    {
        Log(">> Building KitchenObject: " + def.soName);
        KitchenObjectSO so = CreateKitchenObjectSO(def.soName);
        string prefName = "food_ingredient_" + def.soName.ToLower();
        GameObject prefabAsset;
        if (def.isTableware)
            prefabAsset = CreateTablewarePrefab(so, def.displayName, "cup");
        else
            prefabAsset = CreateKitchenObjectPrefab(so, def.packRelativePath, prefName + ".prefab");
        if (prefabAsset == null) { Log("  SKIPPED prefab for " + def.soName + " (missing pack model)"); return so; }
        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(PLACEHOLDER_ICON);
        SetKitchenObjectSOField(so, prefabAsset.transform, icon, def.displayName);
        RegisterNetworkPrefab(prefabAsset);
        AppendSceneList(so);
        return so;
    }

    [UnityEditor.MenuItem("DailyCooking/Phase 2 - Create KitchenObjects")]
    public static void Phase2_CreateKitchenObjects_Menu()
    {
        Phase2_CreateKitchenObjects();
    }

    public static void Phase2_CreateKitchenObjects()
    {
        Log("=== Phase 2: Creating KitchenObjects ===");
        var defs = DefaultDefs();
        var createdSOs = new Dictionary<string, KitchenObjectSO>();
        foreach (var def in defs) createdSOs[def.soName] = BuildKitchenObject(def);
        var plate = GetTableware("plate");
        foreach (var def in defs)
        {
            if (def.isTableware) continue;
            KitchenObjectSO so = createdSOs[def.soName];
            if (so == null) continue;
            if (def.addToPlateValidList) AppendTablewareValidList(plate, so);
            if (def.addToCupValidList)
            {
                var cup = GetTableware("cup");
                if (cup == null) cup = CreateTablewarePrefab(createdSOs["Cup"], "Cup", "cup");
                AppendTablewareValidList(cup, so);
            }
        }
        var cupSO = createdSOs["Cup"];
        if (cupSO != null)
        {
            var cupGo = GetTableware("cup");
            if (cupGo != null)
            {
                Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(PLACEHOLDER_ICON);
                SetKitchenObjectSOField(cupSO, cupGo.transform, icon, "Cup");
                RegisterNetworkPrefab(cupGo);
                AppendSceneList(cupSO);
                SetKitchenObjectSOField(cupGo, cupSO);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Log("=== Phase 2 complete ===");
    }

    private static Transform FindChildByName(GameObject root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    private static List<string> GetExistingPlacedIds(SerializedObject pdb)
    {
        var list = pdb.FindProperty("placeObjects");
        var ids = new List<string>();
        if (list == null) return ids;
        for (int i = 0; i < list.arraySize; i++)
        {
            var elem = list.GetArrayElementAtIndex(i);
            var objRef = elem.objectReferenceValue as PlacedObjectTypeSO;
            if (objRef != null) ids.Add(objRef.id);
        }
        return ids;
    }

    private static void AddPlacedObjectToDatabase(PlacedObjectTypeSO poso)
    {
        var pdb = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/SO/PlacedObjectDatabase/PlacedObjectDatabase.asset");
        if (pdb == null) { Log("  ERROR: PlacedObjectDatabase null"); return; }
        var so = new SerializedObject(pdb);
        var list = so.FindProperty("placeObjects");
        if (list == null) { Debug.LogWarning("[ContentBuilder] no placeObjects prop"); so.Dispose(); return; }
        bool present = false;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == poso) { present = true; break; }
        if (!present)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = poso;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Log("  placed object registered in database: " + poso.nameString);
        }
        else so.Dispose();
    }

    private static void CreateCookingToolCounter(System.Type toolType, string toolChildName, string counterPrefabName, string placedSoName, string displayName)
    {
        string counterPath = Path.Combine("Assets/Prefab/Counters", counterPrefabName + ".prefab").Replace('\\', '/');
        EnsureDir(counterPath);
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(counterPath);
        if (existing != null) { Log("  tool prefab exists, re-wiring: " + counterPrefabName); return; }
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Counters/StovePanCounter.prefab");
        if (template == null) { Log("  ERROR: StovePanCounter template null"); return; }
        GameObject clone = (GameObject)Object.Instantiate(template);
        clone.name = counterPrefabName;

        Transform toolChild = FindChildByName(clone, "PanCookingTool");
        if (toolChild == null) { Log("  ERROR: PanCookingTool child not found in template"); Object.DestroyImmediate(clone); return; }
        toolChild.name = toolChildName;

        var oldTool = toolChild.GetComponent<PanCookingTool>();
        Transform savedPlacePoint = null;
        Component savedProgressBar = null, savedBurnWarning = null;
        float savedBurnShow = 0.5f;
        if (oldTool != null)
        {
            var oso = new SerializedObject(oldTool);
            var p = oso.FindProperty("placePoint"); if (p != null) savedPlacePoint = p.objectReferenceValue as Transform;
            var pb = oso.FindProperty("progressBarUI"); if (pb != null) savedProgressBar = pb.objectReferenceValue as Component;
            var bw = oso.FindProperty("burnWarningUI"); if (bw != null) savedBurnWarning = bw.objectReferenceValue as Component;
            var bsp = oso.FindProperty("burnShowProgressAmount"); if (bsp != null) savedBurnShow = bsp.floatValue;
            oso.Dispose();
            Object.DestroyImmediate(oldTool);
        }

        var newTool = (CookingTool)toolChild.gameObject.AddComponent(toolType);
        var nso = new SerializedObject(newTool);
        var pp = nso.FindProperty("placePoint"); if (pp != null) pp.objectReferenceValue = savedPlacePoint;
        var npb = nso.FindProperty("progressBarUI"); if (npb != null) npb.objectReferenceValue = savedProgressBar;
        var nbw = nso.FindProperty("burnWarningUI"); if (nbw != null) nbw.objectReferenceValue = savedBurnWarning;
        var nbsp = nso.FindProperty("burnShowProgressAmount"); if (nbsp != null) nbsp.floatValue = savedBurnShow;
        nso.ApplyModifiedProperties();
        nso.Dispose();

        var ctrl = clone.GetComponent<StoveCounterController>();
        if (ctrl != null)
        {
            var cso = new SerializedObject(ctrl);
            var ct = cso.FindProperty("_cookingTool");
            if (ct != null) ct.objectReferenceValue = newTool;
            cso.ApplyModifiedProperties();
            cso.Dispose();
        }

        GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(clone, counterPath);
        Object.DestroyImmediate(clone);
        RegisterNetworkPrefab(prefabAsset);

        var tplSo = AssetDatabase.LoadAssetAtPath<PlacedObjectTypeSO>("Assets/SO/BuildingSO/Counters/StovePanCounter.asset");
        if (tplSo == null) { Log("  ERROR: StovePanCounter SO template null"); return; }
        var copy = ScriptableObject.CreateInstance<PlacedObjectTypeSO>();
        EditorUtility.CopySerialized(tplSo, copy);
        string soPath = Path.Combine("Assets/SO/BuildingSO/Counters", placedSoName + ".asset").Replace('\\', '/');
        EnsureDir(soPath);
        AssetDatabase.CreateAsset(copy, soPath);
        var soso = new SerializedObject(copy);
        soso.FindProperty("nameString").stringValue = displayName;
        soso.FindProperty("prefab").objectReferenceValue = prefabAsset;
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Assets/Icons/Counters/StovePanCounter.png");
        soso.FindProperty("icon").objectReferenceValue = icon;
        // unique id
        var pdbObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/SO/PlacedObjectDatabase/PlacedObjectDatabase.asset");
        var ids = GetExistingPlacedIds(new SerializedObject(pdbObj));
        int max = 0; foreach (var id in ids) if (int.TryParse(id, out int v) && v > max) max = v;
        soso.FindProperty("id").stringValue = (max + 1).ToString();
        soso.ApplyModifiedProperties();
        EditorUtility.SetDirty(copy);
        AssetDatabase.ImportAsset(soPath, ImportAssetOptions.ForceUpdate);
        AddPlacedObjectToDatabase(copy);
        Log("  created cooking tool: " + counterPrefabName + " / " + placedSoName + " (" + displayName + ")");
    }

    [UnityEditor.MenuItem("DailyCooking/Phase 4 - Create Cooking Tools")]
    public static void Phase4_CreateCookingTools_Menu()
    {
        Phase4_CreateCookingTools();
    }

    public static void Phase4_CreateCookingTools()
    {
        Log("=== Phase 4: Creating Cooking Tools ===");
        CreateCookingToolCounter(typeof(OvenCookingTool), "OvenCookingTool", "OvenCookingToolCounter", "OvenCounter", "Oven");
        CreateCookingToolCounter(typeof(DeepFryCookingTool), "DeepFryCookingTool", "DeepFryerCounter", "DeepFryerCounter", "Deep Fryer");
        CreateCookingToolCounter(typeof(BeverageCookingTool), "BeverageCookingTool", "BeverageMachineCounter", "BeverageCounter", "Beverage Machine");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Log("=== Phase 4 complete ===");
    }

    private const string BAKING_DIR = "Assets/SO/BakingRecipeSO";
    private const string DEEPFRY_DIR = "Assets/SO/DeepFryRecipeSO";
    private const string DRINK_DIR = "Assets/SO/DrinkRecipeSO";
    private const string BURNING_DIR = "Assets/SO/BurningRecipeSO";
    private const string RECIPE_DB_PATH = "Assets/SO/RecipeDatabase/RecipeDatabase.asset";

    private static KitchenObjectSO ResolveSO(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return AssetDatabase.LoadAssetAtPath<KitchenObjectSO>(Path.Combine(KITCHENOBJ_SO_DIR, name + ".asset").Replace('\\', '/'));
    }

    private static T CreateRecipeAsset<T>(string assetName, string dir) where T : ScriptableObject
    {
        string path = Path.Combine(dir, assetName + ".asset").Replace('\\', '/');
        EnsureDir(path);
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        T r = ScriptableObject.CreateInstance<T>();
        r.name = assetName;
        AssetDatabase.CreateAsset(r, path);
        EditorUtility.SetDirty(r);
        return r;
    }

    private static void AppendRecipeToList(RecipeDatabaseSO db, string propName, ScriptableObject recipe)
    {
        var so = new SerializedObject(db);
        var list = so.FindProperty(propName);
        if (list == null) { Debug.LogWarning("[ContentBuilder] no serialized prop " + propName); so.Dispose(); return; }
        bool present = false;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == recipe) { present = true; break; }
        if (!present)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = recipe;
            so.ApplyModifiedProperties();
            Log("  recipe added to db." + propName);
        }
        else so.Dispose();
    }

    private static void AddBaking(RecipeDatabaseSO db, string assetName, string inputSo, string outputSo, float timer)
    {
        var r = CreateRecipeAsset<BakingRecipeSO>(assetName, BAKING_DIR);
        r.input = ResolveSO(inputSo); r.output = ResolveSO(outputSo); r.bakingTimerMax = timer;
        EditorUtility.SetDirty(r);
        AppendRecipeToList(db, "bakingRecipes", r);
    }

    private static void AddDeepFry(RecipeDatabaseSO db, string assetName, string inputSo, string outputSo, float timer)
    {
        var r = CreateRecipeAsset<DeepFryRecipeSO>(assetName, DEEPFRY_DIR);
        r.input = ResolveSO(inputSo); r.output = ResolveSO(outputSo); r.deepFryTimerMax = timer;
        EditorUtility.SetDirty(r);
        AppendRecipeToList(db, "deepFryRecipes", r);
    }

    private static void AddDrink(RecipeDatabaseSO db, string assetName, string[] inputs, string outputSo, float timer)
    {
        var r = CreateRecipeAsset<DrinkRecipeSO>(assetName, DRINK_DIR);
        r.input = new List<KitchenObjectSO>();
        foreach (var n in inputs) { var s = ResolveSO(n); if (s != null) r.input.Add(s); }
        r.output = ResolveSO(outputSo); r.drinkTimerMax = timer;
        EditorUtility.SetDirty(r);
        AppendRecipeToList(db, "drinkRecipes", r);
    }

    private static void AddBurning(RecipeDatabaseSO db, string assetName, string inputSo, string outputSo, float timer)
    {
        var r = CreateRecipeAsset<BurningRecipeSO>(assetName, BURNING_DIR);
        r.input = ResolveSO(inputSo); r.output = ResolveSO(outputSo); r.burningTimerMax = timer;
        EditorUtility.SetDirty(r);
        AppendRecipeToList(db, "burningRecipes", r);
    }

    [UnityEditor.MenuItem("DailyCooking/Phase 3 - Create Recipes")]
    public static void Phase3_CreateRecipes_Menu()
    {
        Phase3_CreateRecipes();
    }

    public static void Phase3_CreateRecipes()
    {
        Log("=== Phase 3: Creating Recipes ===");
        var db = AssetDatabase.LoadAssetAtPath<RecipeDatabaseSO>(RECIPE_DB_PATH);
        if (db == null) { Log("  ERROR: RecipeDatabase null"); return; }

        AddBaking(db, "Bake_Dough_Croissant", "Dough", "Croissant", 8f);
        AddBaking(db, "Bake_ChickenBreast_RoastChicken", "ChickenBreast", "RoastChicken", 12f);
        AddBaking(db, "Bake_BeefSteak_CookedSteak", "BeefSteak", "CookedSteak", 10f);
        AddBaking(db, "Bake_Salmon_CookedSalmon", "Salmon", "CookedSalmon", 10f);
        AddBaking(db, "Bake_CorndogRaw_CorndogCooked", "CorndogRaw", "CorndogCooked", 9f);
        AddBaking(db, "Bake_BaconRaw_BaconCooked", "BaconRaw", "BaconCooked", 7f);
        AddBaking(db, "Bake_PotatoSlices_BakedPotato", "PotatoSlices", "BakedPotato", 11f);

        AddDeepFry(db, "Fry_PotatoSlices_FrenchFries", "PotatoSlices", "FrenchFries", 6f);
        AddDeepFry(db, "Fry_ChickenLeg_FriedChicken", "ChickenLeg", "FriedChicken", 8f);
        AddDeepFry(db, "Fry_CorndogRaw_CorndogCooked", "CorndogRaw", "CorndogCooked", 7f);
        AddDeepFry(db, "Fry_BaconRaw_BaconCooked", "BaconRaw", "BaconCooked", 6f);

        AddDrink(db, "Drink_Milk_Milkshake", new[] { "Milk" }, "Milkshake", 4f);
        AddDrink(db, "Drink_Orange_OrangeJuice", new[] { "Orange" }, "OrangeJuice", 4f);
        AddDrink(db, "Drink_Peach_PeachSmoothie", new[] { "Peach" }, "PeachSmoothie", 4f);

        string[] burnOutputs = { "RoastChicken", "CookedSteak", "CookedSalmon", "BakedPotato",
                                 "CorndogCooked", "BaconCooked", "Croissant", "FrenchFries",
                                 "FriedChicken", "Milkshake", "OrangeJuice", "PeachSmoothie" };
        foreach (var o in burnOutputs)
            AddBurning(db, "Burn_" + o, o, "BurgerTrashSO", 5f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Log("=== Phase 3 complete ===");
    }

    private struct FoodDef
    {
        public string name;
        public string recipeName;
        public int foodType;
        public long price;
        public int exp;
        public int unlockLevel;
        public int unlockPrice;
        public string[] ingredients;
        public FoodDef(string n, string rn, int ft, long p, int e, int ul, int up, string[] ing)
        {
            name = n; recipeName = rn; foodType = ft; price = p; exp = e;
            unlockLevel = ul; unlockPrice = up; ingredients = ing;
        }
    }

    private struct ShopDef
    {
        public string itemName;
        public ShopItemCategory category;
        public int price;
        public int unlockLevel;
        public string koName;
        public ShopDef(string n, ShopItemCategory c, int p, int ul, string ko)
        {
            itemName = n; category = c; price = p; unlockLevel = ul; koName = ko;
        }
    }

    private static FoodSO CreateFoodSO(FoodDef d, Sprite icon)
    {
        string path = Path.Combine(FOOD_SO_DIR, d.name + ".asset").Replace('\\', '/');
        EnsureDir(path);
        var existing = AssetDatabase.LoadAssetAtPath<FoodSO>(path);
        if (existing != null) { Log("  FoodSO exists: " + d.name); return existing; }
        var fs = ScriptableObject.CreateInstance<FoodSO>();
        fs.name = d.name;
        fs.recipeName = d.recipeName;
        fs.kitchenObjectSOList = new List<KitchenObjectSO>();
        int missing = 0;
        foreach (var ing in d.ingredients)
        {
            var so = ResolveSO(ing);
            if (so == null) { Log("  missing ingredient SO " + ing + " for " + d.name); missing++; }
            fs.kitchenObjectSOList.Add(so);
        }
        fs.foodType = (FoodType)d.foodType;
        fs.price = d.price;
        fs.exp = d.exp;
        fs.unlockLevel = d.unlockLevel;
        fs.unlockPrice = d.unlockPrice;
        fs.Sprite = icon;
        AssetDatabase.CreateAsset(fs, path);
        var fso = new SerializedObject(fs);
        var gp = fso.FindProperty("_guid");
        if (gp != null && string.IsNullOrEmpty(gp.stringValue))
        {
            gp.stringValue = AssetDatabase.AssetPathToGUID(path);
            fso.ApplyModifiedProperties();
        }
        fso.Dispose();
        EditorUtility.SetDirty(fs);
        Log("  FoodSO created " + d.name + (missing > 0 ? " (missing=" + missing + ")" : ""));
        return fs;
    }

    private static void RegisterFoodInConfigFood(FoodSO food)
    {
        var cfg = AssetDatabase.LoadAssetAtPath<ConfigFood>(CONFIG_FOOD_ASSET);
        if (cfg == null) { Log("  ERROR ConfigFood null"); return; }
        var so = new SerializedObject(cfg);
        var list = so.FindProperty("foodItems");
        if (list == null) { Log("  ERROR no foodItems prop"); so.Dispose(); return; }
        bool present = false;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == food) { present = true; break; }
        if (!present)
        {
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = food;
            so.ApplyModifiedProperties();
            Log("  registered in ConfigFood: " + food.name);
        }
        so.Dispose();
    }

    private static void AddShopIngredientItem(string itemName, ShopItemCategory category, int price, int unlockLevel, KitchenObjectSO rewardKo, int amount)
    {
        var shop = AssetDatabase.LoadAssetAtPath<ConfigShop>(CONFIG_SHOP_ASSET);
        if (shop == null) { Log("  ERROR ConfigShop null"); return; }
        var so = new SerializedObject(shop);
        var list = so.FindProperty("shopItems");
        int maxId = 0;
        for (int i = 0; i < list.arraySize; i++)
        {
            int id = list.GetArrayElementAtIndex(i).FindPropertyRelative("id").intValue;
            if (id > maxId) maxId = id;
        }
        list.InsertArrayElementAtIndex(list.arraySize);
        var elem = list.GetArrayElementAtIndex(list.arraySize - 1);
        elem.FindPropertyRelative("id").intValue = maxId + 1;
        elem.FindPropertyRelative("name").stringValue = itemName;
        elem.FindPropertyRelative("type").enumValueFlag = (int)ShopItemType.Ingredient;
        elem.FindPropertyRelative("category").enumValueFlag = (int)category;
        elem.FindPropertyRelative("price").intValue = price;
        elem.FindPropertyRelative("unlockLevel").intValue = unlockLevel;
        var rewards = elem.FindPropertyRelative("rewards");
        rewards.InsertArrayElementAtIndex(rewards.arraySize);
        var rew = rewards.GetArrayElementAtIndex(0);
        rew.FindPropertyRelative("Guid").stringValue = rewardKo.Guid;
        rew.FindPropertyRelative("Amount").intValue = amount;
        so.ApplyModifiedProperties();
        so.Dispose();
        Log("  shop item added: " + itemName + " (id " + (maxId + 1) + ")");
    }

    private static void FixBurningOutputs()
    {
        var trash = ResolveSO("BurgerTrashSO");
        if (trash == null) { Log("  ERROR BurgerTrashSO null"); return; }
        var db = AssetDatabase.LoadAssetAtPath<RecipeDatabaseSO>(RECIPE_DB_PATH);
        if (db == null) { Log("  ERROR RecipeDatabase null"); return; }
        var so = new SerializedObject(db);
        var list = so.FindProperty("burningRecipes");
        int fixedN = 0;
        for (int i = 0; i < list.arraySize; i++)
        {
            var br = list.GetArrayElementAtIndex(i).objectReferenceValue as BurningRecipeSO;
            if (br == null) continue;
            var bso = new SerializedObject(br);
            var op = bso.FindProperty("output");
            if (op != null && op.objectReferenceValue == null)
            {
                op.objectReferenceValue = trash;
                bso.ApplyModifiedProperties();
                fixedN++;
            }
            bso.Dispose();
        }
        so.Dispose();
        AssetDatabase.SaveAssets();
        Log("  burning outputs fixed (null -> BurgerTrashSO): " + fixedN);
    }

    [UnityEditor.MenuItem("DailyCooking/Phase 5 - Create Dishes")]
    public static void Phase5_CreateDishes_Menu()
    {
        Phase5_CreateDishes();
    }

    public static void Phase5_CreateDishes()
    {
        Log("=== Phase 5: Creating Dishes ===");
        var icon = AssetDatabase.LoadAssetAtPath<Sprite>(PLACEHOLDER_ICON);
        var dishes = new FoodDef[]
        {
            new FoodDef("RoastChickenMeal","Roast Chicken Meal",(int)FoodType.MainDish,240,30,4,80,new[]{"RoastChicken","BakedPotato"}),
            new FoodDef("SteakFrites","Steak Frites",(int)FoodType.MainDish,280,32,5,100,new[]{"CookedSteak","FrenchFries"}),
            new FoodDef("GrilledSalmonPlate","Grilled Salmon Plate",(int)FoodType.MainDish,260,30,5,90,new[]{"CookedSalmon","BakedPotato"}),
            new FoodDef("BakedPotatoes","Baked Potatoes",(int)FoodType.MainDish,130,16,2,35,new[]{"BakedPotato"}),
            new FoodDef("CorndogSpecial","Corndog Special",(int)FoodType.MainDish,170,20,3,45,new[]{"CorndogCooked"}),
            new FoodDef("CroissantSnack","Croissant Snack",(int)FoodType.Dessert,100,14,2,30,new[]{"Croissant"}),
            new FoodDef("ChickenNuggetsMeal","Chicken Nuggets Meal",(int)FoodType.MainDish,190,22,4,50,new[]{"FriedChicken","FrenchFries"}),
            new FoodDef("BaconBurger","Bacon Burger",(int)FoodType.MainDish,300,34,5,110,new[]{"BunSO","BurgerCookedSO","BaconCooked","LettuceSliceSO","TomatoSlicesSO","CheeseSliceSO"}),
            new FoodDef("LoadedFries","Loaded Fries",(int)FoodType.MainDish,220,26,4,60,new[]{"FrenchFries","BaconCooked","CheeseSliceSO"}),
            new FoodDef("HotDogMeal","Hot Dog Meal",(int)FoodType.MainDish,150,18,3,40,new[]{"HotDog"}),
            new FoodDef("GardenSalad","Garden Salad",(int)FoodType.Appetizer,110,15,2,30,new[]{"LettuceSliceSO","TomatoSlicesSO","CucumberSlice","OnionSlicesSO"}),
            new FoodDef("OrangeJuiceDrink","Orange Juice",(int)FoodType.Drink,130,16,2,30,new[]{"OrangeJuice"}),
            new FoodDef("MilkshakeDrink","Milkshake",(int)FoodType.Drink,150,20,3,40,new[]{"Milkshake"}),
            new FoodDef("PeachSmoothieDrink","Peach Smoothie",(int)FoodType.Drink,160,22,3,45,new[]{"PeachSmoothie"}),
        };
        var created = new List<FoodSO>();
        foreach (var d in dishes)
        {
            var fs = CreateFoodSO(d, icon);
            if (fs != null) { created.Add(fs); RegisterFoodInConfigFood(fs); }
        }
        var plate = GetTableware("plate");
        var bk = ResolveSO("BurgerCookedSO");
        if (plate != null && bk != null) AppendTablewareValidList(plate, bk);

        var shopDefs = new ShopDef[]
        {
            new ShopDef("Chicken Breast", ShopItemCategory.Meats, 25, 2, "ChickenBreast"),
            new ShopDef("Beef Steak", ShopItemCategory.Meats, 30, 3, "BeefSteak"),
            new ShopDef("Salmon", ShopItemCategory.Meats, 35, 4, "Salmon"),
            new ShopDef("Potato Slices", ShopItemCategory.Vegetables, 18, 2, "PotatoSlices"),
            new ShopDef("Corndog (Raw)", ShopItemCategory.Patties, 40, 3, "CorndogRaw"),
            new ShopDef("Chicken Leg", ShopItemCategory.Meats, 22, 2, "ChickenLeg"),
            new ShopDef("Bacon (Raw)", ShopItemCategory.Patties, 24, 3, "BaconRaw"),
            new ShopDef("Cucumber", ShopItemCategory.Vegetables, 15, 2, "Cucumber"),
            new ShopDef("Bell Pepper", ShopItemCategory.Vegetables, 16, 2, "BellPepperRed"),
            new ShopDef("Orange", ShopItemCategory.Vegetables, 18, 2, "Orange"),
            new ShopDef("Milk", ShopItemCategory.Dairy, 16, 2, "Milk"),
            new ShopDef("Peach", ShopItemCategory.Vegetables, 20, 3, "Peach"),
            new ShopDef("Dough", ShopItemCategory.Bakery, 14, 2, "Dough"),
        };
        foreach (var s in shopDefs)
        {
            var ko = ResolveSO(s.koName);
            if (ko != null) AddShopIngredientItem(s.itemName, s.category, s.price, s.unlockLevel, ko, 1);
            else Log("  shop: missing KO " + s.koName);
        }
        FixBurningOutputs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Log("=== Phase 5 complete: " + created.Count + " dishes ===");
    }
}
