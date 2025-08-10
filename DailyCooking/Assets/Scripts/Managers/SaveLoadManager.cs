using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    private GameData gameData;
    private FileDataHandler dataHandler;

    private void Start()
    {
        gameData = GameManager.Instance.GameData;
        dataHandler = GameManager.Instance.DataHandler;

        gameData.PlayerStats.OnResourceChange += SaveGame;
        gameData.PlayerStats.OnLevelChange += SaveGame;
        gameData.PlayerStats.OnExpChange += SaveGame;
        gameData.InventoryData.OnInventoryDataChanged += SaveGame;
        gameData.GridData.OnGridDataChanged += SaveGame;
        gameData.TutorialData.OnTutorialDataChanged += SaveGame;
    }

    private void OnDestroy()
    {
        if (gameData != null && gameData.PlayerStats != null)
        {
            gameData.PlayerStats.OnResourceChange -= SaveGame;
            gameData.PlayerStats.OnLevelChange -= SaveGame;
            gameData.PlayerStats.OnExpChange -= SaveGame;
            gameData.InventoryData.OnInventoryDataChanged -= SaveGame;
            gameData.GridData.OnGridDataChanged -= SaveGame;
            gameData.TutorialData.OnTutorialDataChanged -= SaveGame;
        }
    }

    public void SaveGame()
    {
        dataHandler.Save(gameData);
    }
}

