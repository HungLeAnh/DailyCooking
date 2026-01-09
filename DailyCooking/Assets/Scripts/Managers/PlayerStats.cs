using System;

[System.Serializable]
public class PlayerStats
{
    public event Action OnResourceChange;
    public event Action OnLevelChange;
    public event Action OnExpChange;
    public event Action<int> OnLevelUp;

    public PlayerData playerData = new PlayerData();
    public StatsData statsData = new StatsData();
    public void UpdatePlayerCoins(int addCoins)
    {
        playerData.Coins += addCoins;
        OnResourceChange?.Invoke();
    }
    public void UpdatePlayerGems(int addGem)
    {
        playerData.Gems += addGem; 
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
    public void UpdatePlayerMoveSpeed(float amount)
    {
        statsData.MoveSpeed += statsData.MoveSpeed*amount;
        OnResourceChange?.Invoke();

    }
    public void UpdatePlayerCookingSpeed(float amount)
    {
        statsData.CookingSpeed += statsData.CookingSpeed * amount;
        OnResourceChange?.Invoke();

    }
    public void UpdatePlayerCarryingCapacity(float amount)
    {
        statsData.CarryingCapacity += (int)amount;
        OnResourceChange?.Invoke();

    }
    public void UpdatePlayerTipIncrease(float amount)
    {
        statsData.TipIncrease += amount;
        OnResourceChange?.Invoke();

    }
}

[Serializable]
public class StatsData
{
    private string restaurantName = "";
    private float moveSpeed = 5f;
    private float cookingSpeed = 1f;
    private int carryingCapacity = 1;
    private float tipIncrease = 0f;

    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float CookingSpeed { get => cookingSpeed; set => cookingSpeed = value; }
    public int CarryingCapacity { get => carryingCapacity; set => carryingCapacity = value; }
    public float TipIncrease { get => tipIncrease; set => tipIncrease = value; }
    public string RestaurantName { get => restaurantName; set => restaurantName = value; }
}