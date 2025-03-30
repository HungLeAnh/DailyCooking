using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEditor.UI;

public class SceneEditorTool
{
    private static string scenesFolder = "Assets/Scenes"; // Specify your scenes folder here

    [MenuItem("Tools/Scene Editor Tool/Scene")]
    public static void OpenScene()
    {
        SceneEditorWindow window = EditorWindow.GetWindow<SceneEditorWindow>();
        window.titleContent = new GUIContent("Scene Editor");
        window.Show();
    }

    public class SceneEditorWindow : EditorWindow
    {
        private List<string> scenePaths;

        private void OnEnable()
        {
            scenePaths = new List<string>();
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { scenesFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                scenePaths.Add(path);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Scenes in Folder", EditorStyles.boldLabel);

            foreach (string scenePath in scenePaths)
            {
                if (GUILayout.Button(Path.GetFileNameWithoutExtension(scenePath)))
                {
                    OpenScene(scenePath);
                }
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
}
