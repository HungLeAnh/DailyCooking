using System;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

// Save slots: creating, loading and writing the restaurant save file.
public partial class GameManager
{
    private void SubscribeSaveEvents()
    {
        if (gameData == null) return;
        UnsubscribeSaveEvents();
        gameData.RestaurantData.OnLevelChange += SaveGame;
        gameData.RestaurantData.OnExpChange += SaveGame;
        gameData.RestaurantData.OnLevelUp += ShowLevelUpPopup;
        gameData.RestaurantData.OnResourceChange += SaveGame;
        if (gameData.PlayersStats != null)
        {
            foreach (var player in gameData.PlayersStats)
                player.OnResourceChange += SaveGame;
        }
        gameData.OnPlayerStatsAdded += GameData_OnPlayerStatsAdded;
        gameData.UpgradeData.OnMenuDataChanged += SaveGame;
        gameData.CosmeticData.OnCosmeticDataChanged += SaveGame;
        gameData.InventoryData.OnInventoryDataChanged += SaveGame;
        gameData.GridData.OnGridDataChanged += SaveGame;
        gameData.TutorialData.OnTutorialDataChanged += SaveGame;
        gameData.MenuData.OnMenuDataChanged += SaveGame;
        gameData.ShopData.OnResourceChange += SaveGame;
        gameData.PostBoxData.OnResourceChange += SaveGame;
    }

    private void UnsubscribeSaveEvents()
    {
        if (gameData == null) return;
        gameData.RestaurantData.OnLevelChange -= SaveGame;
        gameData.RestaurantData.OnExpChange -= SaveGame;
        gameData.RestaurantData.OnLevelUp -= ShowLevelUpPopup;
        gameData.RestaurantData.OnResourceChange -= SaveGame;
        if (gameData.PlayersStats != null)
        {
            foreach (var player in gameData.PlayersStats)
                player.OnResourceChange -= SaveGame;
        }
        gameData.OnPlayerStatsAdded -= GameData_OnPlayerStatsAdded;
        gameData.UpgradeData.OnMenuDataChanged -= SaveGame;
        gameData.CosmeticData.OnCosmeticDataChanged -= SaveGame;
        gameData.InventoryData.OnInventoryDataChanged -= SaveGame;
        gameData.GridData.OnGridDataChanged -= SaveGame;
        gameData.TutorialData.OnTutorialDataChanged -= SaveGame;
        gameData.MenuData.OnMenuDataChanged -= SaveGame;
        gameData.ShopData.OnResourceChange -= SaveGame;
        gameData.PostBoxData.OnResourceChange -= SaveGame;
    }

    private void GameData_OnPlayerStatsAdded(PlayerStats playerStats)
    {
        playerStats.OnResourceChange += SaveGame;
        SaveGame();
    }

    // Display only: the server already granted the reward (see ServerAddExp).
    private void ShowLevelUpPopup(int level)
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UILevelUpPopup,
        new UILevelUpPopup.Param
        {
            reward = new RewardData[]
        { new RewardData(RewardData.RewardType.Coin.ToString(), EconomyRules.GetLevelUpRewardCoins(level)) }
        });
    }

    // A joining client replaces its GameData with the host's snapshot.
    public void ReplaceGameDataFromHost(GameData hostGameData)
    {
        if (gameData != null)
            gameData.RestaurantData.OnLevelUp -= ShowLevelUpPopup;
        gameData = hostGameData;
        if (gameData != null)
        {
            gameData.MenuData.LoadMenuData();
            gameData.RestaurantData.OnLevelUp += ShowLevelUpPopup;
        }
    }

    public bool NewGame(string gameDataName, string password)
    {
        if (!IsValidGameDataName(gameDataName))
        {
            UIManager.Instance.ShowAlertMessage("Invalid restaurant name.");
            return false;
        }
        if (SavedDataList.Any(s => s.GameDataName == gameDataName))
        {
            // Creating it again would overwrite the existing save file.
            UIManager.Instance.ShowAlertMessage("A restaurant with this name already exists.");
            return false;
        }
        ClearActiveSave();
        gameData = GameData.CreateNew();
        dataHandler = new FileDataHandler(
            Application.persistentDataPath,
            fileName + "_" + gameDataName
        );
        SavedDataList.Add(new SavedData(gameDataName, PasswordHasher.Hash(password)));
        savedDataHandler.Save(SavedDataList);
        SubscribeSaveEvents();
        SaveGameImmediate();
        return true;
    }

    public bool LoadGame(string gameDataName, string password)
    {
        var savedData = SavedDataList.FirstOrDefault(s => s.GameDataName == gameDataName);
        if (savedData == null || !PasswordHasher.Verify(password, savedData.Password))
        {
            Debug.LogError("Invalid game data name or password.");
            UIManager.Instance.ShowAlertMessage("Invalid restaurant name or password.");
            return false;
        }
        if (!PasswordHasher.IsHashed(savedData.Password))
        {
            // Older saves stored the password in plain text; re-save it hashed now that it matched.
            savedData.Password = PasswordHasher.Hash(password);
            savedDataHandler.Save(SavedDataList);
        }
        ClearActiveSave();
        var handler = new FileDataHandler(
            Application.persistentDataPath,
            fileName + "_" + gameDataName
        );
        var loadedData = handler.Load();
        if (loadedData == null)
        {
            Debug.LogError($"Save file for '{gameDataName}' is missing or unreadable.");
            UIManager.Instance.ShowAlertMessage("This restaurant's save could not be loaded.");
            return false;
        }
        dataHandler = handler;
        gameData = loadedData;
        gameData.Migrate();
        gameData.MenuData.LoadMenuData();
        SubscribeSaveEvents();
        return true;
    }

    public void DeleteSavedData(SavedData savedData)
    {
        SavedDataList.Remove(savedData);
        savedDataHandler.Save(SavedDataList);
    }

    // Flushes the active save (host only) and forgets its file, so joining someone else's
    // restaurant can never write their data over this device's save file.
    // discardData=false keeps GameData readable while the game scene is still tearing down.
    public void ClearActiveSave(bool discardData = true)
    {
        SaveGameImmediate();
        UnsubscribeSaveEvents();
        dataHandler = null;
        isSaveDirty = false;
        if (discardData)
            gameData = null;
    }

    private static bool IsValidGameDataName(string gameDataName)
    {
        return !string.IsNullOrWhiteSpace(gameDataName) &&
            gameDataName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) < 0;
    }

    // Marks the save dirty; FlushPendingSave writes it at most once per SAVE_INTERVAL,
    // so bursts of data events cost one write and the last change is never dropped.
    public void SaveGame()
    {
        if (!CanSave()) return;
        isSaveDirty = true;
    }

    public void SaveGameImmediate()
    {
        if (!CanSave()) return;
        WriteSave();
    }

    private void FlushPendingSave()
    {
        if (!isSaveDirty || Time.unscaledTime - lastSaveTime < SAVE_INTERVAL) return;
        if (!CanSave())
        {
            isSaveDirty = false;
            return;
        }
        WriteSave();
    }

    private void WriteSave()
    {
        isSaveDirty = false;
        lastSaveTime = Time.unscaledTime;
        try
        {
            dataHandler.Save(gameData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game data: {e}");
        }
    }

    // Only the host owns the restaurant save.
    private bool CanSave()
    {
        if (dataHandler == null || gameData == null) return false;
        var networkManager = NetworkManager.Singleton;
        return networkManager == null || !networkManager.IsListening || networkManager.IsServer;
    }
}
