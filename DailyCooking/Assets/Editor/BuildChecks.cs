using System.IO;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

public static class BuildChecks
{
    private const string OUTPUT_FOLDER = "Temp/BuildChecks/AndroidPlayerScripts";

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
