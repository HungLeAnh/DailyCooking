using System;
using System.Collections.Generic;
using UnityEngine;

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

    private GameObject playerGameObject;
    public FileDataHandler DataHandler => dataHandler;

    public GameData GameData => gameData;
    public GameState GameState => gameState;
    protected override void Awake()
    {
        base.Awake();
        dataHandler = new FileDataHandler(
            Application.persistentDataPath,
            fileName
        );
        SwitchState(GameState.MainMenu);
    }
    private void Start()
    {
        LoadGame();
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIMainMenuPopup);
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
