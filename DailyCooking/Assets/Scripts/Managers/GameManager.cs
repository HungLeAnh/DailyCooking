using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class GameManager : PersistentSingleton<GameManager>, IGameManager
{
    public event EventHandler OnPlayerSpawned;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 playerSpawnPosition;
    private GameData gameData;
    private FileDataHandler dataHandler;
    private GameManagerBaseState currentState;

    [Header("Settings")]
    [SerializeField] private string fileName = "GameData";
    
    private GameObject playerGameObject;
    public FileDataHandler DataHandler => dataHandler;

    public GameData GameData => gameData;
    
    protected override void Awake()
    {
        base.Awake();
        dataHandler = new FileDataHandler(
            Application.persistentDataPath,
            fileName
        );
    }
    private void Start()
    {
        LoadGame();
        gameData.MenuData.LoadMenuData();

        gameData.PlayerStats.OnResourceChange += SaveGame;
        gameData.PlayerStats.OnLevelChange += SaveGame;
        gameData.PlayerStats.OnExpChange += SaveGame;
        gameData.InventoryData.OnInventoryDataChanged += SaveGame; 
        gameData.GridData.OnGridDataChanged += SaveGame;
        gameData.TutorialData.OnTutorialDataChanged += SaveGame;
        gameData.MenuData.OnMenuDataChanged += SaveGame;

        gameData.PlayerStats.OnLevelUp += ShowLevelUpPopup;
        SwitchState(new MainMenuState(this));
    }

    private void ShowLevelUpPopup(int level)
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UILevelUpPopup,
            new UILevelUpPopup.Param
            {
                reward = new RewardData[]
            { new RewardData(UILevelUpPopup.RewardType.Coin.ToString(), level * 100) }
            });
    }
    private void Update()
    {
        currentState?.Update();
    }

    public void InitializePlayer()
    {
        playerGameObject = Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
        OnPlayerSpawned?.Invoke(this, EventArgs.Empty);
    }
    public void HidePlayer()
    {
        playerGameObject.SetActive(false);
    }public void ShowPlayer()
    {
        playerGameObject.SetActive(true);
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

    public void SwitchState(GameManagerBaseState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
