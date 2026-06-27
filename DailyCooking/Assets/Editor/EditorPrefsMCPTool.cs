using UnityEditor;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;
using System;
using MCPForUnity.Editor.Helpers;

namespace DailyCooking.Editor.Tools
{
    [McpForUnityTool("manage_editorprefs", Description = "Get, set, delete, or check existence of Unity EditorPrefs keys.")]
    public static class EditorPrefsMCPTool
    {
        public static object HandleCommand(JObject @params)
        {
            if (@params == null)
            {
                return new SuccessResponse("Error: Parameters cannot be null.", new { success = false });
            }

            string action = @params["action"]?.ToString()?.ToLower();
            string key = @params["key"]?.ToString();

            if (string.IsNullOrEmpty(action) || string.IsNullOrEmpty(key))
            {
                return new SuccessResponse("Error: Action and Key are required parameters.", new { success = false });
            }

            try
            {
                switch (action)
                {
                    case "get":
                        string type = @params["type"]?.ToString()?.ToLower() ?? "string";
                        object val = null;
                        if (!EditorPrefs.HasKey(key))
                        {
                            return new SuccessResponse($"Key '{key}' does not exist.", new { success = false, exists = false });
                        }

                        if (type == "int") val = EditorPrefs.GetInt(key);
                        else if (type == "float") val = EditorPrefs.GetFloat(key);
                        else if (type == "bool") val = EditorPrefs.GetBool(key);
                        else val = EditorPrefs.GetString(key);

                        return new SuccessResponse($"Retrieved key '{key}' of type '{type}'.", new { success = true, exists = true, value = val });

                    case "set":
                        string setType = @params["type"]?.ToString()?.ToLower() ?? "string";
                        JToken valueToken = @params["value"];

                        if (valueToken == null)
                        {
                            return new SuccessResponse("Error: Value parameter is required for set action.", new { success = false });
                        }

                        if (setType == "int") EditorPrefs.SetInt(key, valueToken.Value<int>());
                        else if (setType == "float") EditorPrefs.SetFloat(key, valueToken.Value<float>());
                        else if (setType == "bool") EditorPrefs.SetBool(key, valueToken.Value<bool>());
                        else EditorPrefs.SetString(key, valueToken.ToString());

                        return new SuccessResponse($"Successfully set key '{key}' of type '{setType}'.", new { success = true });

                    case "delete":
                        if (!EditorPrefs.HasKey(key))
                        {
                            return new SuccessResponse($"Key '{key}' does not exist, nothing to delete.", new { success = false, exists = false });
                        }
                        EditorPrefs.DeleteKey(key);
                        return new SuccessResponse($"Successfully deleted key '{key}'.", new { success = true });

                    case "has":
                        bool exists = EditorPrefs.HasKey(key);
                        return new SuccessResponse($"Key '{key}' existence check: {exists}.", new { success = true, exists = exists });

                    default:
                        return new SuccessResponse($"Error: Unknown action '{action}'. Use 'get', 'set', 'delete', or 'has'.", new { success = false });
                }
            }
            catch (Exception ex)
            {
                return new SuccessResponse($"Exception occurred: {ex.Message}", new { success = false, error = ex.ToString() });
            }
        }
    }
}
