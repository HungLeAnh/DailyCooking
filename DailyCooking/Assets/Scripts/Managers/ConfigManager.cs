using UnityEngine;
public class ConfigManager : PersistentSingleton<ConfigManager>
{
    [SerializeField] private ConfigShop configShop;
    [SerializeField] private ConfigFood configFood;
    [SerializeField] private ConfigUpgrade configUpgrade;
    [SerializeField] private Customization_Data customizationData;
    public ConfigShop ConfigShop => configShop;
    public ConfigFood ConfigFood => configFood;
    public ConfigUpgrade ConfigUpgrade => configUpgrade;
    public Customization_Data CustomizationData => customizationData;

    protected override void Awake()
    {
        ConfigFood.Initialize();
    }
}
