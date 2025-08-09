[System.Serializable]
public class GameData
{
    public PlayerStats playerStats = new PlayerStats();
    public InventoryData inventoryData = new InventoryData();
    public GridData gridData = new GridData();
    public TutorialData tutorialData = new TutorialData();

    public void SaveGridData(GridXZ<GridObject> grid)
    {
        gridData.SaveGridData(grid);
    }
    public void AddInventoryData(InventoryItemData item)
    {
        inventoryData.Add(item);
    }    
    public void AddInventoryData(string guid)
    {
        inventoryData.Add(guid);
    }
    public void RemoveInventoryData(InventoryItemData item)
    {
        inventoryData.Remove(item);
    }    
    public void RemoveInventoryData(string id)
    {
        inventoryData.Remove(id);
    }
    public void UpdatePlayedDay(int playerDay)
    {
        playerStats.playerData.daysPlayed = playerDay;
    }
}

