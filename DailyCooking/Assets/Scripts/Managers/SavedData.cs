using System;

[Serializable]
public class SavedData
{
    public string GameDataName { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public DateTime LastSavedTime { get; private set; } = DateTime.MinValue;
    public SavedData() { }
    public SavedData(string name, string password) 
    {
        GameDataName = name;
        Password = password;
        LastSavedTime = DateTime.Now;
    }
    public void UpdateSavedData()
    {
        LastSavedTime = DateTime.Now;
    }
}