using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InventoryTabDatabase", menuName = "Inventory/InventoryTabDatabase", order = 1)]
public class InventoryTabDatabase : ScriptableObject
{
    [SerializeField] public List<InventoryTab> tabTypesList = new List<InventoryTab>();
    public List<InventoryTab> TabTypesList => tabTypesList; 
    public InventoryTab GetTabByType(InventoryTabType tabType)
    {
        return tabTypesList.Find(x => x.TabType == tabType);
    }
}