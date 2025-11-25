using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class GameManager : PersistentSingleton<GameManager>, IGameManager
{
    public event EventHandler OnPlayerSpawned;

    [SerializeField] private GameObject playerPrefab;
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
        
        gameData.PlayerStats.OnResourceChange += SaveGame;
        gameData.PlayerStats.OnLevelChange += SaveGame;
        gameData.PlayerStats.OnExpChange += SaveGame;
        gameData.InventoryData.OnInventoryDataChanged += SaveGame;
        gameData.GridData.OnGridDataChanged += SaveGame;
        gameData.TutorialData.OnTutorialDataChanged += SaveGame;
        gameData.MenuData.OnMenuDataChanged += SaveGame;
        SwitchState(new MainMenuState(this));
    }

    private void Update()
    {
        currentState?.Update();
    }

    public void InitializePlayer()
    {
        Vector3 placePosition = Vector3.zero;

        playerGameObject = Instantiate(playerPrefab, placePosition, Quaternion.identity);
        OnPlayerSpawned?.Invoke(this, EventArgs.Empty);
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
