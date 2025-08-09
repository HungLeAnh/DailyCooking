using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
            }
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
        catch 
        {
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
}

