using System.IO;
using Newtonsoft.Json;

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
        settings = SaveJson.CreateSettings();
    }

    public GameData Load()
    {
        return SaveJson.ReadWithBackup<GameData>(Path.Combine(dataDirPath, dataFileName), settings);
    }
    public GameData LoadFromJson(string jsonData)
    {
        try
        {
            return JsonConvert.DeserializeObject<GameData>(jsonData, settings);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error loading data from JSON: {e}");
            return null;
        }
    }
    public void Save(GameData data)
    {
        SaveJson.WriteAtomic(Path.Combine(dataDirPath, dataFileName), ConvertGameDataToJson(data));
    }
    public string ConvertGameDataToJson(GameData data)
    {
        return JsonConvert.SerializeObject(data, settings);
    }
}
