using UnityEngine;
public class ConfigManager : PersistentSingleton<ConfigManager>
{
    [SerializeField] private ConfigShop configShop;
    public ConfigShop ConfigShop => configShop;
    
    public void LoadConfig()
    {

    }
}
