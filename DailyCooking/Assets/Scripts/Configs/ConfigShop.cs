using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ConfigShopSO", menuName = "Configs/ConfigShop")]
[Serializable]
public class ConfigShop : ScriptableObject
{
    [SerializeField] private List<ConfigShopItem> shopItems = new List<ConfigShopItem>();
    public List<ConfigShopItem> ShopItems => shopItems;

    // Lazy id -> item cache so callers stop doing List.Find. The asset is loaded once, so the
    // dictionary is built on first use and only rebuilt if the list length changes (ShopItems
    // hands out the live list by reference, so a caller could in principle append to it).
    private Dictionary<int, ConfigShopItem> _byId;

    public ConfigShopItem GetById(int id)
    {
        if (_byId == null || _byId.Count != shopItems.Count)
        {
            _byId = new Dictionary<int, ConfigShopItem>(shopItems.Count);
            for (int i = 0; i < shopItems.Count; i++)
            {
                ConfigShopItem item = shopItems[i];
                if (item == null)
                    continue;
                // First one wins, matching the old List.Find semantics for a duplicated id.
                if (!_byId.ContainsKey(item.Id))
                    _byId[item.Id] = item;
            }
        }
        return _byId.GetValueOrDefault(id);
    }
}
