using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

public class FileDataHandler
{
    private string dataDirPath;
    private string dataFileName;
    private JsonSerializerSettings settings;
    public JsonSerializerSettings Settings => settings;

    public FileDataHandler(string dirPath, string fileName)
    {
        this.dataDirPath = dirPath;
        this.dataFileName = fileName;
        settings = new JsonSerializerSettings
        {
            ContractResolver = new CustomContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Error = (sender, args) =>
            {
                args.ErrorContext.Handled = true;
            },
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented
        };
        settings.Converters.Add(new UniversalUnityConverter());
    }

    public GameData Load()
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        if (!File.Exists(fullPath)) return null;

        string data = File.ReadAllText(fullPath);
        try
        {
            GameData gameData = JsonConvert.DeserializeObject<GameData>(data,settings);
            return gameData;

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading data from file: {fullPath} {e}");
            return null;
        }
    }
    public GameData LoadFromJson(string jsonData)
    {
        try
        {
            GameData gameData = JsonConvert.DeserializeObject<GameData>(jsonData, settings);
            return gameData;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading data from JSON: {e}");
            return null;
        }
    }
    public void Save(GameData data)
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        
        string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented, settings);
        
        File.WriteAllText(fullPath, jsonData);
    }
    public string ConvertGameDataToJson(GameData data)
    {
        return JsonConvert.SerializeObject(data, Formatting.Indented, settings);
    }
}

