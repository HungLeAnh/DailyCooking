[System.Serializable]
public class PlayerData
{  
    public int DaysPlayed { get; set; } = 1;
    public PlayerData(){}
    public PlayerData(int daysPlayed)
    {
        DaysPlayed = daysPlayed;
    }
}

