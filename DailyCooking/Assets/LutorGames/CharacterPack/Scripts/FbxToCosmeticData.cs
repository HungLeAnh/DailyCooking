#if UNITY_EDITOR
namespace PrivateLT.CharacterCustomization
{
    using System.Collections.Generic;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    public class FbxToCosmeticData : EditorWindow
    {
        private static bool _alwayOverride;
        private static GameObject fbxGameobject;
        private static Customization_Data customizationData;
        private static Dictionary<string, List<Mesh>> meshDictionary = new();

        private static readonly string _iconFolderPath = "Assets/LutorGames/CharacterPack/Art/Icons";
        private static readonly string _scriptableObjectPath = "Assets/LutorGames/CharacterPack/Custom_SO/Customization_Data.asset";

        [MenuItem("Assets/LutorGames/Analyze Character Customization", false, -10)]
        public static void AnalyzeCharacter()
        {
            fbxGameobject = Selection.activeGameObject;

            if (fbxGameobject == null)
            {
                Debug.LogError("Please select FBX Gameobject");
                return;
            }

            _alwayOverride = false;

            ProcessFbxAndAddCosmetics();
        }

        private static void ProcessFbxAndAddCosmetics()
        {
            customizationData = AssetDatabase.LoadAssetAtPath<Customization_Data>(_scriptableObjectPath);

            if (customizationData == null)
            {
                Debug.LogError($"Customization_Data ScriptableObject not found at path: {_scriptableObjectPath}");
                return;
            }

            InitialiseFBXMeshData();
            ApplyFBXMeshData();


            EditorUtility.SetDirty(customizationData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Completed",
                "Successfuly Analyzed Customization",
                "Thank you lutor <3"
            );
        }

        private static void InitialiseFBXMeshData()
        {
            meshDictionary.Clear();

            var meshFilters = fbxGameobject.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var meshFilter in meshFilters)
            {
                var mesh = meshFilter.sharedMesh;

                string meshName = mesh.name;

                string category = meshName.Split('_')[0];

                if (!meshDictionary.ContainsKey(category))
                {
                    meshDictionary[category] = new List<Mesh>();
                }

                meshDictionary[category].Add(mesh);
            }
        }

        private static void ApplyFBXMeshData()
        {
            foreach (var type in meshDictionary.Keys)
            {
                var meshes = meshDictionary[type];

                foreach (var mesh in meshes)
                {
                    string meshName = mesh.name;

                    string imagePath = Path.Combine(_iconFolderPath, meshName + ".png");
                    Sprite image = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);

                    if (image == null) Debug.LogError($"Icon Couldn't found: {imagePath}");

                    Cosmetic newCosmetic = new Cosmetic
                    {
                        Name = meshName,
                        Icon = image,
                        Mesh = mesh
                    };

                    if (customizationData.Contains(newCosmetic, out var isSame) && (isSame || !ShowOverrideDialog(meshName))) continue;

                    customizationData.AddCosmetic(type, newCosmetic);

                    Debug.Log($"Added Cosmetic for mesh '{type}''{meshName}'");
                }
            }
        }

        public static bool ShowOverrideDialog(string meshName)
        {
            if (_alwayOverride) return false;

            int result = EditorUtility.DisplayDialogComplex(
                $"Override {meshName}?",
                $"Override existing {meshName}?",
                "Yes",
                "No",
                "No Always (Recommended)"
            );

            if (result == 2)
            {
                _alwayOverride = true;
            }

            return result == 0;
        }
    }
}

#endif
