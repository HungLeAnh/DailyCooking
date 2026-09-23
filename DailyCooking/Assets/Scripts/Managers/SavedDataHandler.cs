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
        settings = SaveJson.CreateSettings();
    }

    public List<SavedData> Load()
    {
        return SaveJson.ReadWithBackup<List<SavedData>>(Path.Combine(dataDirPath, dataFileName), settings);
    }
    public List<SavedData> LoadFromJson(string jsonData)
    {
        try
        {
            return JsonConvert.DeserializeObject<List<SavedData>>(jsonData, settings);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading data from JSON: {e}");
            return null;
        }
    }
    public void Save(List<SavedData> data)
    {
        SaveJson.WriteAtomic(Path.Combine(dataDirPath, dataFileName), JsonConvert.SerializeObject(data, settings));
    }
}
