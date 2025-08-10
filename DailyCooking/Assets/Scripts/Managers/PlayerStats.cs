using System;

[System.Serializable]
public class PlayerStats
{
    public event Action OnResourceChange;
    public event Action OnLevelChange;
    public event Action OnExpChange;
    public event Action<int> OnLevelUp;

    public PlayerData playerData = new PlayerData();

    public void UpdatePlayerCoins(int addCoins)
    {
        playerData.Coins += addCoins;
        OnResourceChange?.Invoke();
    }

    public void UpdatePlayerExp(int addExp)
    {
        playerData.Exp += addExp;
        if (playerData.Exp >= playerData.Level * 100)
        {
            playerData.Exp = 0;
            playerData.Level++;
            OnLevelChange?.Invoke();
            OnLevelUp?.Invoke(playerData.Level);
        }        
        OnExpChange?.Invoke();
    }
    public void UpdatePlayedDay(int playerDay)
    {
        playerData.DaysPlayed = playerDay;
        OnResourceChange?.Invoke();

    }
}

