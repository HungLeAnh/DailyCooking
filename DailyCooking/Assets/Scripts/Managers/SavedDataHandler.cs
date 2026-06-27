using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class SavedDataHandler
{
    private string dataDirPath;
    private string dataFileName;
    private JsonSerializerSettings settings;
    public JsonSerializerSettings Settings => settings;

    public SavedDataHandler(string dirPath, string fileName)
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

    public List<SavedData> Load()
    {
        string fullPath = Path.Combine(dataDirPath, dataFileName);
        if (!File.Exists(fullPath)) return null;

        string data = File.ReadAllText(fullPath);
        try
        {
            List<SavedData> savedDataList = JsonConvert.DeserializeObject<List<SavedData>>(data, settings);
            return savedDataList;

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading data from file: {fullPath} {e}");
            return null;
        }
    }
    public List<SavedData> LoadFromJson(string jsonData)
    {
        try
        {
            List<SavedData> savedDataList = JsonConvert.DeserializeObject<List<SavedData>>(jsonData, settings);
            return savedDataList;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading data from JSON: {e}");
            return null;
        }
    }
    public void Save(List<SavedData> data)
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
