[System.Serializable]
public class PlayerData
{
    private const int DEFAULT_STARTING_COINS = 200;

    public int Level { get; set; } = 1;
    public int Exp { get; set; } = 0;
    public int Gems { get; set; } = 0;
    public int Coins { get; set; } = DEFAULT_STARTING_COINS;
    public int DaysPlayed { get; set; } = 1;
    public PlayerData(){}
    public PlayerData(int level, int exp, int gems, int coins, int daysPlayed)
    {
        Level = level;
        Exp = exp;
        Gems = gems;
        Coins = coins;
        DaysPlayed = daysPlayed;
    }
}

