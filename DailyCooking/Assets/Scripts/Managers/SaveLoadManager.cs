using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    private GameData gameData;
    private FileDataHandler dataHandler;

    private void Start()
    {
        gameData = GameManager.Instance.GameData;
        dataHandler = GameManager.Instance.DataHandler;

        gameData.playerStats.OnResourceChange += SaveGame;
        gameData.playerStats.OnLevelChange += SaveGame;
    }

    private void OnDestroy()
    {
        gameData.playerStats.OnResourceChange -= SaveGame;
        gameData.playerStats.OnLevelChange -= SaveGame;
    }

    public void SaveGame()
    {
        dataHandler.Save(gameData);
    }
}
