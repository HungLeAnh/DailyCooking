using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public class AssetsToCSVTool
{
    [MenuItem("Tools/Create List of Assets for Icon Gen")]
    public static void ExportAssetNames()
    {
        // Open folder panel starting at Assets folder
        string folderPath = EditorUtility.OpenFolderPanel("Select Folder to Export Assets From", Application.dataPath, "");

        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.Log("Folder selection canceled.");
            return;
        }

        // Convert absolute path to relative Unity project path
        string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        if (!folderPath.StartsWith(projectPath))
        {
            Debug.LogError("Selected folder must be inside the Unity project Assets folder.");
            return;
        }

        string relativeFolderPath = folderPath.Substring(projectPath.Length).Replace('\\', '/');

        // Find all asset GUIDs in the selected folder (recursively)
        string[] assetGuids = AssetDatabase.FindAssets("t:Prefab", new[] { relativeFolderPath });

        if (assetGuids.Length == 0)
        {
            Debug.LogWarning($"No assets found in folder '{relativeFolderPath}'.");
            return;
        }

        // Prepare CSV content
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("AssetName,AssetPath,CameraPosition,CameraRotation,ObjectRotation,BackgroundColor");

        foreach (string guid in assetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            string assetName = Path.GetFileNameWithoutExtension(assetPath);
            string cameraPosition = "(0 0 0)";
            string cameraRotation = "(0 0 0)";
            string objectRotation = "(0 0 0)";
            string backgroundColor = "(1 1 1 1)";

            assetName = EscapeForCSV(assetName);
            assetPath = EscapeForCSV(assetPath);

            csv.AppendLine($"{assetName},{assetPath},{cameraPosition},{cameraRotation},{objectRotation},{backgroundColor}");
        }

        // Choose a path to save CSV file
        string savePath = EditorUtility.SaveFilePanel(
            "Save Asset List as CSV",
            Application.dataPath,
             relativeFolderPath.GetLastPartFromSeparator('/') +".csv",
            "csv");

        if (string.IsNullOrEmpty(savePath))
        {
            Debug.Log("CSV export canceled.");
            return;
        }

        File.WriteAllText(savePath, csv.ToString(), Encoding.UTF8);
        Debug.Log($"Exported {assetGuids.Length} assets to CSV:\n{savePath}");
    }

    private static string EscapeForCSV(string input)
    {
        if (input.Contains(",") || input.Contains("\"") || input.Contains("\n"))
        {
            input = input.Replace("\"", "\"\"");
            return $"\"{input}\"";
        }
        return input;
    }
}