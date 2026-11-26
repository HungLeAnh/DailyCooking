using System;

[Serializable]
public class RestaurantData
{
    private const int DEFAULT_STARTING_COINS = 200;
    [System.NonSerialized]
    public Action OnResourceChange;
    [System.NonSerialized]
    public Action OnLevelChange;
    [System.NonSerialized]
    public Action OnExpChange;
    [System.NonSerialized]
    public Action<int> OnLevelUp;

    public int Level { get; set; } = 1;
    public int Exp { get; set; } = 0;
    public int Gems { get; set; } = 0;
    public int Coins { get; set; } = DEFAULT_STARTING_COINS;

    private string restaurantName = "";
    public string RestaurantName { get => restaurantName; set => restaurantName = value; }
    public void UpdateRestaurantName(string name)
    {
        restaurantName = name;
        OnResourceChange?.Invoke();
    }
    public void UpdateRestaurantCoins(int addCoins)
    {
        Coins += addCoins;
        OnResourceChange?.Invoke();
    }
    public void UpdateRestaurantGems(int addGem)
    {
        Gems += addGem;
        OnResourceChange?.Invoke();
    }
    // Clients mirror the host's totals (never deltas), so a lost or repeated message can't drift.
    public void SetCoins(int total)
    {
        Coins = total;
        OnResourceChange?.Invoke();
    }
    public void SetGems(int total)
    {
        Gems = total;
        OnResourceChange?.Invoke();
    }
    // Fires OnLevelUp once for every level reached above the current one, in order.
    public void SetProgress(int level, int exp)
    {
        int previous = Level;
        Exp = exp;
        if (level != previous)
        {
            Level = level;
            OnLevelChange?.Invoke();
            for (int reached = previous + 1; reached <= level; reached++)
                OnLevelUp?.Invoke(reached);
        }
        OnExpChange?.Invoke();
    }
    public void UpdateRestaurantExp(int addExp)
    {
        Exp += addExp;
        if (Exp >= Level * 100)
        {
            Exp = 0;
            Level++;
            OnLevelChange?.Invoke();
            OnLevelUp?.Invoke(Level);
        }
        OnExpChange?.Invoke();
    }
}