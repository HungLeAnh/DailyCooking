using System;

[System.Serializable]
public class PlayerStats
{
    public event Action OnResourceChange;
    public event Action OnLevelChange;
    public event Action OnExpChange;
    public event Action<int> OnLevelUp;

    public PlayerData playerData = new PlayerData();

    public void UpdatePlayerResources(int addCoins)
    {
        playerData.coins += addCoins;
        OnResourceChange?.Invoke();
    }

    public void UpdatePlayerExp(int addExp)
    {
        playerData.exp += addExp;
        if (playerData.exp >= playerData.level * 100)
        {
            playerData.exp = 0;
            playerData.level++;
            OnLevelChange?.Invoke();
            OnLevelUp?.Invoke(playerData.level);
        }        
        OnExpChange?.Invoke();
        GameManager.Instance.SaveGame();

    }
}
