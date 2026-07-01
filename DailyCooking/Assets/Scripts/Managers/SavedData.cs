using Newtonsoft.Json;
using System;

[Serializable]
public class SavedData
{
    private string gameDataName = string.Empty;
    private string password = string.Empty;
    private DateTime lastSavedTime = DateTime.MinValue;
    public string GameDataName { get=>gameDataName; set=>gameDataName = value; }
    public string Password { get=>password; set=>password = value; } 
    public DateTime LastSavedTime { get=>lastSavedTime; set=>lastSavedTime = value; }
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