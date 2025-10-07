using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;




public class IconGenEditorTool : EditorWindow
{
    private class Data
    {
        public string name;
        public GameObject prefab;
        public Vector3 cameraPosition;
        public Vector3 cameraRotation;
        public Vector3 objectRotation;
        public Color backgroundColor;
    }
    
    [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

    [SerializeField] private TextAsset m_TextAsset = default;

    [SerializeField] private Data m_selectedData;
    
    [SerializeField] private Texture2D m_previewTexture;

    private Vector3Field m_cameraPositionField;
    private Vector3Field m_cameraRotationField;
    private Vector3Field m_objectRotationField;
    private Button m_exportButton;
    private Button m_exportAllButton;
    private ColorField m_backgroundColorField;

    private ListView m_list;

    private List<Data> m_ListData;


    private int m_size = 512;
    private Scene m_previewScene;
    private GameObject m_cameraObject;
    private Camera m_sceneCamera;
    private GameObject m_instance;


    private float Timer = 0f;
    private const float SaveTimeThreshold = 3f; // Save every 3 seconds
    private bool isTimerStopped = false;

    private void Update()
    {
        Timer += Time.deltaTime;
    }

    [MenuItem("Tools/Icon Gen Editor Tool")]
    public static void ShowExample()
    {
        IconGenEditorTool wnd = GetWindow<IconGenEditorTool>();
        wnd.titleContent = new GUIContent("IconGenEditorTool");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Instantiate UXML
        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        m_list = rootVisualElement.Q<ListView>("List");

        LoadData();

        m_list.itemsSource = m_ListData;
        m_list.bindItem = BindItem;
        m_list.makeItem = () => new Label();
        m_list.selectionChanged += OnSelectItem;
        m_list.Rebuild();


        rootVisualElement.Q<VisualElement>("Content").dataSource = this;

        m_cameraPositionField = rootVisualElement.Q<Vector3Field>("CameraPosition");
        m_cameraRotationField = rootVisualElement.Q<Vector3Field>("CameraRotation");
        m_objectRotationField = rootVisualElement.Q<Vector3Field>("ObjectRotation");
        m_exportButton = rootVisualElement.Q<Button>("ExportButton");
        m_exportAllButton = rootVisualElement.Q<Button>("ExportAllButton");
        m_backgroundColorField = rootVisualElement.Q<ColorField>("BackgroundColor");


        m_cameraPositionField.RegisterValueChangedCallback(OnCameraPositionChanged);
        m_cameraRotationField.RegisterValueChangedCallback(OnCameraRotationChanged);
        m_objectRotationField.RegisterValueChangedCallback(OnObjectRotationChanged);
        m_exportButton.clicked += Export;
        m_exportAllButton.clicked += ExportAll;
        m_backgroundColorField.RegisterValueChangedCallback(OnBackgroundColorChanged);
    }

    private void OnDestroy()
    {
        if(m_sceneCamera != null)
        {
            m_sceneCamera.targetTexture = null;
            DestroyImmediate(m_sceneCamera);
            DestroyImmediate(m_previewTexture);
            m_previewTexture = null;
            m_sceneCamera = null;
        }

        if (m_previewScene.IsValid())
        {
            EditorSceneManager.ClosePreviewScene(m_previewScene);
        }
        if (m_cameraObject != null)
        {
            DestroyImmediate(m_cameraObject);
        }
        if (m_instance != null)
        {
            DestroyImmediate(m_instance);
        }
        SaveData();
    }
    private void OnBackgroundColorChanged(ChangeEvent<Color> evt)
    {
        if (m_sceneCamera != null)
        {
            m_sceneCamera.backgroundColor = evt.newValue;
            m_ListData[m_list.selectedIndex].backgroundColor = evt.newValue;
        }
        UpdateCamera();
    }

    private void Export()
    {
        m_sceneCamera.depthTextureMode = DepthTextureMode.Depth;
        m_sceneCamera.backgroundColor = new Color(0, 0, 0, 0);

        UpdateCamera();

        SaveTextureAsPNG(m_previewTexture, m_selectedData.name);
        
        m_sceneCamera.backgroundColor = Color.black;
        UpdateCamera();
    }
    private void ExportAll()
    {
        isTimerStopped = true;

        if (!m_previewScene.IsValid())
        {
            m_previewScene = EditorSceneManager.NewPreviewScene();
        }
        if (m_cameraObject == null)
        {
            m_cameraObject = new GameObject("Camera");
            m_cameraObject.transform.position = new Vector3(0, 0, -10);
            m_cameraObject.transform.eulerAngles = new Vector3(0, 0, 0);
            m_sceneCamera = m_cameraObject.AddComponent<Camera>();
            m_sceneCamera.backgroundColor = Color.white;
            m_sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            m_sceneCamera.targetTexture = new RenderTexture(m_size, m_size, 32, RenderTextureFormat.ARGBFloat);

            SceneManager.MoveGameObjectToScene(m_cameraObject, m_previewScene);
            m_sceneCamera.scene = m_previewScene;

            m_cameraObject.transform.position = m_cameraObject.transform.position;
            m_cameraObject.transform.eulerAngles = m_cameraObject.transform.eulerAngles;

            m_cameraPositionField.value = m_cameraObject.transform.position;
            m_cameraRotationField.value = m_cameraObject.transform.eulerAngles;
        }
        m_sceneCamera.depthTextureMode = DepthTextureMode.Depth;
        m_sceneCamera.backgroundColor = new Color(0, 0, 0, 0);

        string csvPath = AssetDatabase.GetAssetPath(m_TextAsset);
        csvPath.Remove(csvPath.LastIndexOf('/'));
        string folderPath = EditorUtility.SaveFolderPanel("Save Icon", csvPath, "Exported Icons");
        
        if (string.IsNullOrEmpty(folderPath))
        {
            Debug.LogWarning("Save operation cancelled.");
            return;
        }

        foreach (var data in m_ListData)
        {

            // Clear previous objects in the preview scene
            if (m_instance != null)
            {
                DestroyImmediate(m_instance);
            }

            // Instantiate the selected prefab in the preview scene
            m_instance = PrefabUtility.InstantiatePrefab(data.prefab, m_previewScene) as GameObject;
            m_instance.transform.position = Vector3.zero;
            m_instance.transform.rotation = Quaternion.identity;

            SetupSetting(data);
            m_sceneCamera.depthTextureMode = DepthTextureMode.Depth;
            m_sceneCamera.backgroundColor = new Color(0, 0, 0, 0);            
            UpdateCamera();

            string path = Path.Combine(folderPath, $"{data.name}.png");
            SaveTextureToPath(m_previewTexture, path);

        }
        isTimerStopped = false;
    }
    private void SaveTextureToPath(Texture2D texture, string path)
    {
        if (m_previewTexture == null)
        {
            Debug.LogError("Texture is null, cannot save as PNG.");
            return;
        }

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Save operation cancelled.");
            return;
        }

        byte[] pngData = m_previewTexture.EncodeToPNG();
        if (pngData != null)
        {
            File.WriteAllBytes(path, pngData);
            Debug.Log($"Icon saved to: {path}");
        }
        else
        {
            Debug.LogError("Failed to save texture to PNG.");
        }
    }

    private void SaveTextureAsPNG(Texture2D texture, string name)
    {
        if (texture == null)
        {
            Debug.LogError("Texture is null, cannot save as PNG.");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Save Icon", "Assets/Icons", name + ".png", "png");

        if(string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Save operation cancelled.");
            return;
        }

        byte[] pngData = texture.EncodeToPNG();
        if(pngData != null)
        {
            File.WriteAllBytes(path, pngData);
            Debug.Log($"Icon saved to: {path}");
        }
        else
        {
            Debug.LogError("Failed to save texture to PNG.");
        }
    }

    private void OnObjectRotationChanged(ChangeEvent<Vector3> evt)
    {
        if (m_instance != null)
        {
            m_instance.transform.eulerAngles = evt.newValue;
            m_ListData[m_list.selectedIndex].objectRotation = evt.newValue;
        }
        UpdateCamera();
    }

    private void OnCameraRotationChanged(ChangeEvent<Vector3> evt)
    {
        m_cameraObject.transform.eulerAngles = evt.newValue;
        m_ListData[m_list.selectedIndex].cameraRotation = evt.newValue;
        UpdateCamera();
    }

    private void OnCameraPositionChanged(ChangeEvent<Vector3> evt)
    {
        m_cameraObject.transform.position = evt.newValue;
        m_ListData[m_list.selectedIndex].cameraPosition = evt.newValue;
        UpdateCamera();
    }

    private void BindItem(VisualElement element, int index)
    {
        var label = element as Label;
        label.text = m_ListData[index].name;

    }
    private void OnSelectItem(object item)
    {
        m_selectedData = m_ListData[m_list.selectedIndex];
        if (!m_previewScene.IsValid())
        {
            m_previewScene = EditorSceneManager.NewPreviewScene();
        }
        if(m_cameraObject == null)
        {
            m_cameraObject = new GameObject("Camera");
            m_cameraObject.transform.position = new Vector3(0, 0, -10);
            m_cameraObject.transform.eulerAngles = new Vector3(0, 0, 0);
            m_sceneCamera = m_cameraObject.AddComponent<Camera>();
            m_sceneCamera.backgroundColor = Color.white;
            m_sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            m_sceneCamera.targetTexture = new RenderTexture(m_size, m_size, 32,RenderTextureFormat.ARGBFloat);

            SceneManager.MoveGameObjectToScene(m_cameraObject, m_previewScene);
            m_sceneCamera.scene = m_previewScene;

            m_cameraObject.transform.position = m_cameraObject.transform.position;
            m_cameraObject.transform.eulerAngles = m_cameraObject.transform.eulerAngles;

            m_cameraPositionField.value = m_cameraObject.transform.position;
            m_cameraRotationField.value = m_cameraObject.transform.eulerAngles;
        }
        
        // Clear previous objects in the preview scene
        if (m_instance != null)
        {
            DestroyImmediate(m_instance);
        }

        // Instantiate the selected prefab in the preview scene
        m_instance = PrefabUtility.InstantiatePrefab(m_selectedData.prefab, m_previewScene) as GameObject;
        m_instance.transform.position = Vector3.zero;
        m_instance.transform.rotation = Quaternion.identity;
        SetupSetting(m_selectedData);
        UpdateCamera();
    }
    private void SetupSetting(Data data)
    {
        m_cameraPositionField.value = data.cameraPosition;
        m_cameraRotationField.value = data.cameraRotation;
        m_objectRotationField.value = data.objectRotation;
        m_backgroundColorField.value = data.backgroundColor;
        m_cameraObject.transform.position = data.cameraPosition;
        m_cameraObject.transform.eulerAngles = data.cameraRotation;
        m_sceneCamera.backgroundColor = data.backgroundColor;

        m_instance.transform.eulerAngles = data.objectRotation;
    }
    private void UpdateCamera()
    {
        if (m_selectedData == null || m_selectedData.prefab == null)
        {
            Debug.LogWarning("No prefab selected or prefab is null.");
            return;
        }

        
        // Render the scene to the texture
        m_sceneCamera.Render();
        // Create a texture from the camera's render target
        m_previewTexture = new Texture2D(m_size, m_size, TextureFormat.ARGB32, false, true);
        
        RenderTexture.active = m_sceneCamera.targetTexture;
        m_previewTexture.ReadPixels(new Rect(0, 0, m_size, m_size), 0, 0);
        m_previewTexture.Apply();

        RenderTexture.active = null;

        SaveData();
    }

    private class ParsedData
    {
        public string AssetName { get; set; }
        public string AssetPath { get; set; }
        public Vector3 CameraPosition { get; set; }
        public Vector3 CameraRotation { get; set; }
        public Vector3 ObjectRotation { get; set; }
        public Color BackgroundColor { get; set; }
    }
    private void LoadData()
    {
        if (m_TextAsset == null)
        {
            Debug.LogError("TextAsset is not assigned.");
            return;
        }
        if(m_ListData != null)
            m_ListData.Clear();
        m_ListData = new List<Data>();
        var parsedData = CSVParser<ParsedData>.ParseCSV(m_TextAsset);

        foreach (var item in parsedData)
        {
            if (item == null) continue;

            if (string.IsNullOrEmpty(item.AssetPath) || string.IsNullOrEmpty(item.AssetName))
            {
                Debug.LogWarning($"Invalid data in CSV: {item.AssetName}, {item.AssetPath}");
                continue;
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(item.AssetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found at path: {item.AssetPath}");
                continue;
            }

            m_ListData.Add(new Data 
            { 
                name = item.AssetName, 
                prefab = prefab,
                cameraPosition = item.CameraPosition,
                cameraRotation = item.CameraRotation,
                objectRotation = item.ObjectRotation,
                backgroundColor = item.BackgroundColor
            });
        }
    }
    private void SaveData()
    {
        if(Timer < SaveTimeThreshold || isTimerStopped)
        {
            return;
        }
        Timer = 0f;
        string path = AssetDatabase.GetAssetPath(m_TextAsset);

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("Save operation cancelled.");
            return;
        }

        StringBuilder csv = new StringBuilder();
        csv.AppendLine("AssetName,AssetPath,CameraPosition,CameraRotation,ObjectRotation,BackgroundColor");

        foreach (var item in m_ListData)
        {
            string assetPath = AssetDatabase.GetAssetPath(item.prefab);
            //Debug.Log($"Get Path: {assetPath}");
            string assetName = item.name;
            string cameraPosition = CSVParser<Data>.Vector3ToString(item.cameraPosition);
            string cameraRotation = CSVParser<Data>.Vector3ToString(item.cameraRotation);
            string objectRotation = CSVParser<Data>.Vector3ToString(item.objectRotation);
            string backgroundColor = CSVParser<Data>.ColorToString(item.backgroundColor);

            csv.AppendLine($"{assetName},{assetPath},{cameraPosition},{cameraRotation},{objectRotation},{backgroundColor}");
        }

        File.WriteAllText(path, csv.ToString());
        AssetDatabase.Refresh();
    }
}

// Create an editor window which can display a chosen GameObject.
// Use OnInteractivePreviewGUI to display the GameObject and
// allow it to be interactive.

public class ExampleClass : EditorWindow
{
    GameObject gameObject;
    Editor gameObjectEditor;

    [MenuItem("Example/GameObject Editor")]
    static void ShowWindow()
    {
        GetWindowWithRect<ExampleClass>(new Rect(0, 0, 256, 256));
    }

    void OnGUI()
    {
        gameObject = (GameObject)EditorGUILayout.ObjectField(gameObject, typeof(GameObject), true);

        GUIStyle bgColor = new GUIStyle();
        bgColor.normal.background = EditorGUIUtility.whiteTexture;

        if (gameObject != null)
        {
            if (gameObjectEditor == null)
                gameObjectEditor = Editor.CreateEditor(gameObject);

            gameObjectEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(256, 256), bgColor);
        }
    }
}