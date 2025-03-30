using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System;
using Newtonsoft.Json.Linq;
public class GameManager : PersistentSingleton<GameManager>
{
    private GameData gameData;
    private FileDataHandler dataHandler;

    [Header("Settings")]
    [SerializeField] private string fileName = "GameData";
    [SerializeField] private bool useEncryption = false;

    private List<IDataPersistence> dataPersistenceObjects = new List<IDataPersistence>();

    public GameData GameData => gameData;

    protected override void Awake()
    {
        base.Awake();
        dataHandler = new FileDataHandler(
            Application.persistentDataPath,
            fileName,
            useEncryption
        );
    }
    private void Start()
    {
        LoadGame();
    }
    public void NewGame()
    {
        gameData = new GameData();
        LoadAllObjects(); // Initialize with default values
    }

    public void LoadGame()
    {
        gameData = dataHandler.Load();
        if (gameData == null) NewGame();
        LoadAllObjects();
    }

    public void SaveGame()
    {
        SaveAllObjects();
        dataHandler.Save(gameData);
    }

    private void LoadAllObjects()
    {
        foreach (IDataPersistence obj in dataPersistenceObjects)
        {
            obj.LoadData(gameData);
        }
    }

    private void SaveAllObjects()
    {
        foreach (IDataPersistence obj in dataPersistenceObjects)
        {
            obj.SaveData(ref gameData);
        }
    }

    public void RegisterPersistenceObject(IDataPersistence obj)
    {
        if (!dataPersistenceObjects.Contains(obj))
        {
            dataPersistenceObjects.Add(obj);
        }
    }
}
[System.Serializable]
public class GameData
{
    // Add all saveable properties
    public int currentLevel;
    public InventoryData inventoryData;
    public GridData gridData;

    public void SaveGridData(GridXZ<GridObject> grid)
    {
        gridData = new GridData(grid);
    }
}

public interface IDataPersistence
{
    void LoadData(GameData data);
    void SaveData(ref GameData data);
}

public class FileDataHandler
{
    private string dataDirPath;
    private string dataFileName;
    private bool useEncryption;
    private JsonSerializerSettings settings;
    private readonly string encryptionKey = "your-secure-key";

    public FileDataHandler(string dirPath, string fileName, bool useEncryption)
    {
        this.dataDirPath = dirPath;
        this.dataFileName = fileName;
        this.useEncryption = useEncryption;
        settings = new JsonSerializerSettings
        {
            ContractResolver = new CustomContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        settings.Converters.Add(new UniversalUnityConverter());
    }

    public GameData Load()
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        if (!File.Exists(fullPath)) return null;

        string data = File.ReadAllText(fullPath);
        if (useEncryption) data = XOREncryption(data);
        GameData gameData = JsonConvert.DeserializeObject<GameData>(data,settings);
        return gameData;
    }

    public void Save(GameData data)
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        
        string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented, settings);
        
        if (useEncryption) jsonData = XOREncryption(jsonData);

        File.WriteAllText(fullPath, jsonData);
    }

    private string XOREncryption(string data)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < data.Length; i++)
        {
            sb.Append((char)(data[i] ^ encryptionKey[i % encryptionKey.Length]));
        }
        return sb.ToString();
    }
}
public class CustomContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);

        // Exclude Unity-specific properties like "rigidbody"
        if (member.DeclaringType.Namespace == "UnityEngine")
        {
            property.ShouldSerialize = instance => false;
        }
        // Exclude properties like magnitude and normalized
        if (member.Name == "magnitude" || member.Name == "normalized")
            property.ShouldSerialize = instance => false;

        return property;
    }
}
public class UniversalUnityConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Vector3) ||
               objectType == typeof(Vector3Int) ||
               objectType == typeof(Vector2) ||
               objectType == typeof(Vector2Int) ||
               objectType == typeof(Quaternion) ||
               objectType == typeof(Color);
        // Extend with other Unity types
    }

    public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
    {
        JObject obj = new JObject();

        switch (value)
        {
            case Vector3 vector3:
                obj["x"] = vector3.x;
                obj["y"] = vector3.y;
                obj["z"] = vector3.z;
                break;            

            case Vector3Int  vector3Int:
                obj["x"] = vector3Int.x;
                obj["y"] = vector3Int.y;
                obj["z"] = vector3Int.z;
                break;

            case Vector2 vector2:
                obj["x"] = vector2.x;
                obj["y"] = vector2.y;
                break;

            case Vector2Int vector2Int:
                obj["x"] = vector2Int.x;
                obj["y"] = vector2Int.y;
                break;

            case Quaternion quaternion:
                obj["x"] = quaternion.x;
                obj["y"] = quaternion.y;
                obj["z"] = quaternion.z;
                obj["w"] = quaternion.w;
                break;

            case Color color:
                obj["r"] = color.r;
                obj["g"] = color.g;
                obj["b"] = color.b;
                obj["a"] = color.a;
                break;

            // Add more Unity types here (e.g., Rect, Bounds)
            default:
                throw new JsonSerializationException($"Unsupported Unity type: {value.GetType()}");
        }

        obj.WriteTo(writer);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        if (objectType == typeof(Vector3))
            return new Vector3((float)obj["x"], (float)obj["y"], (float)obj["z"]);   
        
        if (objectType == typeof(Vector3Int))
            return new Vector3((int)obj["x"], (int)obj["y"], (int)obj["z"]);

        if (objectType == typeof(Vector2))
            return new Vector2((float)obj["x"], (float)obj["y"]);

        if(objectType == typeof(Vector2Int))
            return new Vector2Int((int)obj["x"], (int)obj["y"]);

        if (objectType == typeof(Quaternion))
            return new Quaternion((float)obj["x"], (float)obj["y"], (float)obj["z"], (float)obj["w"]);

        if (objectType == typeof(Color))
            return new Color((float)obj["r"], (float)obj["g"], (float)obj["b"], (float)obj["a"]);
        
        throw new JsonSerializationException($"Unsupported Unity type: {objectType}");
    }
}