using System;

[System.Serializable]
public class PlayerStats
{
    public event Action OnResourceChange;

    public PlayerData playerData = new PlayerData();
    public StatsData statsData = new StatsData();

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
    private float moveSpeed = 5f;
    private float cookingSpeed = 1f;
    private int carryingCapacity = 1;
    private float tipIncrease = 0f;

    public float MoveSpeed { get => moveSpeed; set => moveSpeed = value; }
    public float CookingSpeed { get => cookingSpeed; set => cookingSpeed = value; }
    public int CarryingCapacity { get => carryingCapacity; set => carryingCapacity = value; }
    public float TipIncrease { get => tipIncrease; set => tipIncrease = value; }
}