using UnityEngine;
public class ConfigManager : PersistentSingleton<ConfigManager>
{
    [SerializeField] private ConfigShop configShop;
    [SerializeField] private ConfigFood configFood;
    [SerializeField] private ConfigUpgrade configUpgrade;
    public ConfigShop ConfigShop => configShop;
    public ConfigFood ConfigFood => configFood;
    public ConfigUpgrade ConfigUpgrade => configUpgrade;

    public void LoadConfig()
    {

    }
}
