using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System;
using Newtonsoft.Json.Linq;

public enum GameState
{
    MainMenu,
    InGame,
    StartDay
}
[DefaultExecutionOrder(-1)]
public class GameManager : PersistentSingleton<GameManager>
{
    public event EventHandler OnStateChange;
    [SerializeField] private GameObject playerPrefab;
    private GameData gameData;
    private FileDataHandler dataHandler;
    private GameState gameState;

    [Header("Settings")]
    [SerializeField] private string fileName = "GameData";
    [SerializeField] private bool useEncryption = false;

    private List<IDataPersistence> dataPersistenceObjects = new List<IDataPersistence>();
    private GameObject playerGameObject;
    public FileDataHandler DataHandler => dataHandler;

    public GameData GameData => gameData;
    public GameState GameState => gameState;
    protected override void Awake()
    {
        base.Awake();
        dataHandler = new FileDataHandler(
            Application.persistentDataPath,
            fileName,
            useEncryption
        );
        SwitchState(GameState.MainMenu);
    }
    private void Start()
    {
        LoadGame();
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIMainMenuPopup.ToString());
    }

    public void InitializePlayer()
    {
        Vector3 placePosition = GridBuildingSystem.Instance.GetFirstEmptyGridPos();

        playerGameObject = Instantiate(playerPrefab, placePosition, Quaternion.identity);
    }

    public void DestroyPlayer()
    {
        Destroy(playerGameObject);
    }
    public void NewGame()
    {
        gameData = new GameData();
    }

    public void LoadGame()
    {
        gameData = dataHandler.Load();
        if (gameData == null) NewGame();
    }

    public void SaveGame()
    {
        dataHandler.Save(gameData);
    }

    public void SwitchState(GameState newState)
    {
        gameState = newState;
        OnStateChange?.Invoke(this,EventArgs.Empty);
    }
}
[System.Serializable]
public class GameData
{
    [JsonIgnore] public Action OnResourceChange;
    // Add all saveable properties
    public PlayerData playerData = new PlayerData();
    public InventoryData inventoryData = new InventoryData();
    public GridData gridData = new GridData();
    public TutorialData tutorialData = new TutorialData();

    public void SaveGridData(GridXZ<GridObject> grid)
    {
        gridData.SaveGridData(grid);
    }
    public void AddInventoryData(InventoryItemData item)
    {
        inventoryData.Add(item);
    }    
    public void AddInventoryData(string guid)
    {
        inventoryData.Add(guid);
    }
    public void RemoveInventoryData(InventoryItemData item)
    {
        inventoryData.Remove(item);
    }    
    public void RemoveInventoryData(string id)
    {
        inventoryData.Remove(id);
    }
    public void UpdatePlayedDay(int playerDay)
    {
        playerData.daysPlayed = playerDay;
    }
    public void UpdatePlayerResources(int addCoins)
    {
        playerData.coins += addCoins;
        OnResourceChange?.Invoke();
    }
}
[Serializable]
public class PlayerData
{
    public int level=1;
    public int experience=0;
    public int gems=0;
    public int coins = 1000;
    public int daysPlayed=1;
    public PlayerData(){}
    public PlayerData(int level, int experience, int currency, int gems, int coins, int daysPlayed)
    {
        this.level = level;
        this.experience = experience;
        this.gems = gems;
        this.coins = coins;
        this.daysPlayed = daysPlayed;
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
    public JsonSerializerSettings Settings => settings;

    public FileDataHandler(string dirPath, string fileName, bool useEncryption)
    {
        this.dataDirPath = dirPath;
        this.dataFileName = fileName;
        this.useEncryption = useEncryption;
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
        if (useEncryption) data = XOREncryption(data);
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
public enum ShopItemType
{
    Item,
    Currency,
        
    None
}
public enum ShopItemCategory
{
    Counters,

    None
}
[Serializable]
public class ConfigShopItem
{
    [SerializeField] private int id;
    [SerializeField] private string name;
    [SerializeField] private ShopItemType type;
    [SerializeField] private int price;
    [SerializeField] private string reward;
    [SerializeField] private ShopItemCategory category;
    [SerializeField] private int unlockLevel;

    public ShopItemCategory Category { get => category; set => category = value; }
    public string Reward { get => reward; set => reward = value; }
    public int Price { get => price; set => price = value; }
    public ShopItemType Type { get => type; set => type = value; }
    public string Name { get => name; set => name = value; }
    public int Id { get => id; set => id = value; }
    public int UnlockLevel { get => unlockLevel; set => unlockLevel = value; }

}