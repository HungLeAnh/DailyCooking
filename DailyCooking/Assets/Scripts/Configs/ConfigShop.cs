using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ConfigShopSO", menuName = "Configs/ConfigShop")]
[Serializable]
public class ConfigShop : ScriptableObject
{
    [SerializeField] private List<ConfigShopItem> shopItems = new List<ConfigShopItem>();
    public List<ConfigShopItem> ShopItems => shopItems;
}
