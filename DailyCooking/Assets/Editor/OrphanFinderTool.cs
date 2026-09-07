using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class OrphanFinderTool : EditorWindow
{
    // Folders to EXCLUDE from scan - third party / imported packages under Assets/
    private static readonly string[] ExcludedPrefixes = new[]
    {
        "Assets/Cafe Pack",
        "Assets/CoffeeShopStarterPack",
        "Assets/ExternalDependencyManager",
        "Assets/GooglePlayGames",
        "Assets/LevelPlay",
        "Assets/LutorGames",
        "Assets/MobileDependencyResolver",
        "Assets/PlayerPrefsEditor",
        "Assets/Plugins",
        "Assets/Samples",
        "Assets/TextMesh Pro",
        "Assets/Ultimate Food Pack",
        "Assets/Stylized Terrain Textures",
        "Assets/Layer Lab",
        "Assets/IconGenerator",
        "Assets/ithappy",
        "Assets/_Assets",
        "Assets/_Packages",
        "Assets/Widgets",
        "Assets/URPDefaultResources",
        "Assets/Coffee",
        "Assets/CodeMonkey",
        "Assets/Scripts/CodeMonkey", // imported CodeMonkey utils under Scripts
        "Assets/Autodesk",
        "Assets/VisualScripting",
        "Assets/ProBuilder",
        "Assets/Tests", // test scripts not counted as orphan
        "Assets/Settings", // URP / InputSystem settings - system assets
        "Assets/Resources", // DOTweenSettings etc - system
        "Assets/ProjectSettings"
    };

    private static bool IsExcluded(string path)
    {
        if (string.IsNullOrEmpty(path)) return true;
        foreach (var p in ExcludedPrefixes)
            if (path.StartsWith(p, StringComparison.OrdinalIgnoreCase)) return true;
        // also exclude Packages cache if any path sneaks in
        if (path.StartsWith("Packages/")) return true;
        if (path.StartsWith("Library/")) return true;
        return false;
    }

    private static bool IsInEditorFolder(string path) => path.Contains("/Editor/");

    [MenuItem("Tools/Orphan Finder/Find Orphans (Project-Owned)")]
    public static void FindOrphansMenu()
    {
        var result = FindOrphans();
        WriteReport(result);
        EditorUtility.DisplayDialog("Orphan Finder", $"Found: {result.orphanScripts.Count} orphan scripts, {result.orphanSOs.Count} orphan SOs, {result.orphanPrefabs.Count} orphan prefabs.\nReport written to: Assets/../Temp/orphan_report.json and shown in console.", "OK");
    }

    [MenuItem("Tools/Orphan Finder/Find Orphans (Log to Console)")]
    public static void FindOrphansLogOnly()
    {
        var result = FindOrphans();
        LogResult(result);
    }

    public class OrphanResult
    {
        public List<string> orphanScripts = new List<string>();
        public List<string> orphanSOs = new List<string>();
        public List<string> orphanPrefabs = new List<string>();
        public List<string> scannedInfo = new List<string>();
        // detailed maps for debugging
        public Dictionary<string, string> scriptGuidToPath = new Dictionary<string, string>();
    }

    public static OrphanResult FindOrphans()
    {
        var result = new OrphanResult();

        // 1. Collect all candidate assets (project-owned only)
        string[] allMonoScriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
        string[] allPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        // SO: ScriptableObject assets are .asset files with t:ScriptableObject; also include all .asset under SO/Configs
        string[] allSOGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });

        // Fallback: also collect .asset files under known SO folders that may not be found via t:ScriptableObject if type is custom
        // We'll broaden to all assets and filter by extension later
        // Filter by exclusion
        var candidateScriptGuids = allMonoScriptGuids.Where(g => {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (IsExcluded(p)) return false;
            if (IsInEditorFolder(p)) return false; // editor scripts expected not to be referenced by prefabs
            if (p.EndsWith(".cs") == false) return false;
            // skip asmdef etc
            return true;
        }).ToArray();

        var candidatePrefabGuids = allPrefabGuids.Where(g => {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (IsExcluded(p)) return false;
            return true;
        }).ToArray();

        var candidateSOGuids = allSOGuids.Where(g => {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (IsExcluded(p)) return false;
            // Exclude system assets: InputSystem, RPAsset, etc. We only care about game SOs under SO/, Configs/, Prefab SO wrappers
            // Keep only SO folder, Configs, Prefab, Databases
            if (!(p.StartsWith("Assets/SO/") || p.StartsWith("Assets/Configs/") || p.StartsWith("Assets/Prefab/") || p.StartsWith("Assets/Scripts/"))) return false;
            return true;
        }).ToArray();

        result.scannedInfo.Add($"Candidate scripts (excl. Editor & third party): {candidateScriptGuids.Length} / total {allMonoScriptGuids.Length}");
        result.scannedInfo.Add($"Candidate prefabs: {candidatePrefabGuids.Length} / total {allPrefabGuids.Length}");
        result.scannedInfo.Add($"Candidate SOs: {candidateSOGuids.Length} / total {allSOGuids.Length}");

        // 2. Build referenced GUID set by scanning ALL assets that could hold references (including third party referencers? but we care if project-owned orphans are referenced anywhere, even by third party? For orphan definition we consider referenced anywhere including third party? Typically orphan means not referenced anywhere in project. We'll scan ALL non-excluded assets + scenes + also consider Build Settings scenes and Resources)
        // To be safe, scan all Assets (excluding third party candidates? but referencers could be in project-owned only). We scan project-owned referencers only - if an orphan is referenced only by third party, it's still referenced, but since we exclude third party referencers, we would false-positive. So we scan ALL referencers including third party? Let's scan ALL Assets referencers except scripts themselves.
        // --- Optimized: collect dependencies only for project-owned referencers to determine orphan status ---
        // For speed, only scan project-owned assets (excluding third party) as referencers. True orphan = not referenced by project-owned assets.
        var allAssetPaths = AssetDatabase.GetAllAssetPaths()
            .Where(p => p.StartsWith("Assets/"))
            .Where(p => !IsExcluded(p))
            .Where(p => !p.EndsWith(".cs") && !p.EndsWith(".js") && !p.EndsWith(".dll"))
            .ToArray();

        var referencedGuids = new HashSet<string>();
        int total = allAssetPaths.Length;
        for (int i = 0; i < total; i++)
        {
            var path = allAssetPaths[i];
            try
            {
                var deps = AssetDatabase.GetDependencies(path, false);
                foreach (var depPath in deps)
                {
                    var depGuid = AssetDatabase.AssetPathToGUID(depPath);
                    if (!string.IsNullOrEmpty(depGuid)) referencedGuids.Add(depGuid);
                }
            }
            catch { }
            if (i % 1000 == 0) EditorUtility.DisplayProgressBar("Orphan Finder", $"Scanning dependencies {i}/{total}", (float)i / total);
        }
        EditorUtility.ClearProgressBar();

        // --- Optimized code reference: cache all project code into one string ---
        var allCsPaths = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" })
            .Select(g => AssetDatabase.GUIDToAssetPath(g))
            .Where(p => p.EndsWith(".cs") && !IsExcluded(p))
            .ToArray();

        var scriptClassToGuid = new Dictionary<string, string>();
        foreach (var guid in candidateScriptGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (monoScript != null)
            {
                var clazz = monoScript.GetClass();
                if (clazz != null) { if (!scriptClassToGuid.ContainsKey(clazz.Name)) scriptClassToGuid[clazz.Name] = guid; }
                else { var name = Path.GetFileNameWithoutExtension(path); if (!scriptClassToGuid.ContainsKey(name)) scriptClassToGuid[name] = guid; }
            }
            else { var name = Path.GetFileNameWithoutExtension(path); if (!scriptClassToGuid.ContainsKey(name)) scriptClassToGuid[name] = guid; }
        }

        // Build combined code text (excluding self per class later). Read each file once.
        var codeContents = new Dictionary<string, string>();
        var combinedSb = new System.Text.StringBuilder(200000);
        foreach (var csPath in allCsPaths)
        {
            try { var txt = File.ReadAllText(csPath); codeContents[csPath] = txt; combinedSb.AppendLine(txt); } catch { codeContents[csPath] = ""; }
        }
        string combinedCode = combinedSb.ToString();

        var codeReferencedGuids = new HashSet<string>();
        foreach (var kv in scriptClassToGuid)
        {
            var className = kv.Key; var guid = kv.Value;
            var ownPath = AssetDatabase.GUIDToAssetPath(guid);
            // count occurrences in combined but subtract own file if it contains itself
            if (!IsWordInContent(combinedCode, className)) continue;
            // ensure not only self-reference: check if exists outside own file
            bool foundOutside = false;
            foreach (var csPath in allCsPaths)
            {
                if (csPath == ownPath) continue;
                if (IsWordInContent(codeContents[csPath], className)) { foundOutside = true; break; }
            }
            if (foundOutside) codeReferencedGuids.Add(guid);
        }

        // 3. Determine orphans
        // Script orphan = not in referencedGuids AND not in codeReferencedGuids
        // Also check if MonoScript is attached to any prefab/scene: referencedGuids should already capture that (prefab dependency includes MonoScript)
        // Additionally, some scripts are abstract/base classes not directly attached but used via inheritance - they'll be codeReferenced.
        foreach (var guid in candidateScriptGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            bool assetRef = referencedGuids.Contains(guid);
            bool codeRef = codeReferencedGuids.Contains(guid);
            // Also check if script is a MonoBehaviour that is never used: if not assetRef and not codeRef -> orphan
            // For Editor-only scripts we already filtered, so remaining should be runtime
            if (!assetRef && !codeRef)
            {
                // Extra filter: ignore scripts that are ScriptableObject definitions but SO instances exist? If SO instances exist, script is used.
                // We'll keep them as not orphan if any SO of that type exists
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var clazz = ms != null ? ms.GetClass() : null;
                if (clazz != null && typeof(ScriptableObject).IsAssignableFrom(clazz) && candidateSOGuids.Any(g => {
                    var soPath = AssetDatabase.GUIDToAssetPath(g);
                    var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(soPath);
                    return so != null && so.GetType() == clazz;
                })) {
                    // has instances, not orphan
                    continue;
                }
                // For UI etc, also skip if class is never meant to be attached? We'll report anyway.
                result.orphanScripts.Add($"{path} | GUID:{guid} | class:{(clazz!=null?clazz.Name:Path.GetFileNameWithoutExtension(path))}");
                result.scriptGuidToPath[guid] = path;
            }
        }

        // SO orphan = not in referencedGuids (no other asset depends on it)
        foreach (var guid in candidateSOGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!referencedGuids.Contains(guid))
            {
                var assetName = Path.GetFileNameWithoutExtension(path);
                // use combinedCode for string ref (fast)
                if (combinedCode.Contains(assetName) || combinedCode.Contains(guid)) continue;
                result.orphanSOs.Add($"{path} | GUID:{guid}");
            }
        }

        // Prefab orphan = not in referencedGuids
        foreach (var guid in candidatePrefabGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!referencedGuids.Contains(guid))
            {
                if (path.Contains("/Resources/")) continue;
                var assetName = Path.GetFileNameWithoutExtension(path);
                if (combinedCode.Contains(assetName) || combinedCode.Contains(guid)) continue;
                result.orphanPrefabs.Add($"{path} | GUID:{guid}");
            }
        }

        result.scannedInfo.Add($"Total referenced GUIDs collected: {referencedGuids.Count}");
        result.scannedInfo.Add($"Code-referenced scripts: {codeReferencedGuids.Count}");
        return result;
    }

    private static bool IsWordInContent(string content, string word)
    {
        // Check word boundary to avoid false positive like "Player" matching "PlayerAction"
        int idx = content.IndexOf(word, StringComparison.Ordinal);
        while (idx >= 0)
        {
            bool beforeOk = idx == 0 || !char.IsLetterOrDigit(content[idx - 1]) && content[idx - 1] != '_';
            bool afterOk = idx + word.Length >= content.Length || !char.IsLetterOrDigit(content[idx + word.Length]) && content[idx + word.Length] != '_';
            if (beforeOk && afterOk) return true;
            idx = content.IndexOf(word, idx + 1, StringComparison.Ordinal);
        }
        return false;
    }

    private static void LogResult(OrphanResult r)
    {
        Debug.Log($"=== Orphan Finder Report ===");
        foreach (var s in r.scannedInfo) Debug.Log(s);
        Debug.Log($"--- Orphan Scripts ({r.orphanScripts.Count}) ---");
        foreach (var s in r.orphanScripts) Debug.LogWarning($"[Orphan Script] {s}");
        Debug.Log($"--- Orphan SOs ({r.orphanSOs.Count}) ---");
        foreach (var s in r.orphanSOs) Debug.LogWarning($"[Orphan SO] {s}");
        Debug.Log($"--- Orphan Prefabs ({r.orphanPrefabs.Count}) ---");
        foreach (var s in r.orphanPrefabs) Debug.LogWarning($"[Orphan Prefab] {s}");
    }

    private static void WriteReport(OrphanResult r)
    {
        LogResult(r);
        try
        {
            string dir = Path.Combine(Application.dataPath, "../Temp");
            Directory.CreateDirectory(dir);
            string json = JsonUtility.ToJson(new Wrapper { scripts = r.orphanScripts.ToArray(), sos = r.orphanSOs.ToArray(), prefabs = r.orphanPrefabs.ToArray(), info = r.scannedInfo.ToArray() }, true);
            // Also write plain text
            string txtPath = Path.Combine(dir, "orphan_report.json");
            File.WriteAllText(txtPath, json);
            string txtPath2 = Path.Combine(dir, "orphan_report.txt");
            var lines = new List<string>();
            lines.Add("=== Orphan Finder Report ===");
            lines.AddRange(r.scannedInfo);
            lines.Add($"Orphan Scripts ({r.orphanScripts.Count}):");
            lines.AddRange(r.orphanScripts);
            lines.Add($"Orphan SOs ({r.orphanSOs.Count}):");
            lines.AddRange(r.orphanSOs);
            lines.Add($"Orphan Prefabs ({r.orphanPrefabs.Count}):");
            lines.AddRange(r.orphanPrefabs);
            File.WriteAllLines(txtPath2, lines);
            Debug.Log($"Orphan report written to {txtPath} and {txtPath2}");
        }
        catch (Exception e) { Debug.LogError($"Failed to write orphan report: {e}"); }
    }

    [Serializable]
    private class Wrapper
    {
        public string[] scripts;
        public string[] sos;
        public string[] prefabs;
        public string[] info;
    }

    private void OnGUI()
    {
        GUILayout.Label("Orphan Finder (Project-Owned Only)", EditorStyles.boldLabel);
        GUILayout.Label("Excludes: Cafe Pack, CoffeeShopStarterPack, GooglePlayGames, etc. (see ExcludedPrefixes)", EditorStyles.wordWrappedLabel);
        if (GUILayout.Button("Find Orphans (Show in Console + Temp/orphan_report.json)"))
        {
            FindOrphansMenu();
        }
    }
}
