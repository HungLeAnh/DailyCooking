[System.Serializable]
public class PlayerData
{
    public int level=1;
    public int exp=0;
    public int gems=0;
    public int coins = 1000;
    public int daysPlayed=1;
    public PlayerData(){}
    public PlayerData(int level, int exp, int currency, int gems, int coins, int daysPlayed)
    {
        this.level = level;
        this.exp = exp;
        this.gems = gems;
        this.coins = coins;
        this.daysPlayed = daysPlayed;
    }
}
