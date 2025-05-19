using System;
using UnityEngine;

[Serializable]
public class ItemType
{
    [Tooltip("The Item's background color in the UI")]
    [SerializeField] private Color _typeColor = default;
    [Tooltip("The tab type under which the item will be added")]
    [SerializeField] private InventoryTabType _tabType = default;

    public InventoryTabType TabType { get => _tabType; set => _tabType = value; }
    public Color TypeColor { get => _typeColor; set => _typeColor = value; }
}
