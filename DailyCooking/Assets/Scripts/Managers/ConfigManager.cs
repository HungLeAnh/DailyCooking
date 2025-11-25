using UnityEngine;
public class ConfigManager : PersistentSingleton<ConfigManager>
{
    [SerializeField] private ConfigShop configShop;
    [SerializeField] private ConfigFood configFood;
    public ConfigShop ConfigShop => configShop;
    public ConfigFood ConfigFood => configFood;

    public void LoadConfig()
    {

    }
}
