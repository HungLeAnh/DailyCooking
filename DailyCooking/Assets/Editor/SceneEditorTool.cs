using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class SceneEditorWindow : EditorWindow
{
    private const string ScenesFolder = "Assets/Scenes";
    private List<string> scenePaths;
    private Vector2 _scroll;

    [MenuItem("Tools/Scene Editor Tool/Scene")]
    private static void Open() => GetWindow<SceneEditorWindow>("Scene Editor");

    private void OnEnable()
    {
        RefreshSceneList();
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        GUILayout.Label("Scenes in Folder", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh", EditorStyles.miniButton))
        {
            RefreshSceneList();
        }

        if (scenePaths == null || scenePaths.Count == 0)
        {
            EditorGUILayout.HelpBox($"No scenes found in {ScenesFolder}.", MessageType.Info);
        }
        else
        {
            foreach (string scenePath in scenePaths)
            {
                if (GUILayout.Button(Path.GetFileNameWithoutExtension(scenePath)))
                {
                    OpenScene(scenePath);
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshSceneList()
    {
        scenePaths = new List<string>();
        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { ScenesFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            scenePaths.Add(path);
        }
    }

    private void OpenScene(string scenePath)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath);
        }
    }
}
