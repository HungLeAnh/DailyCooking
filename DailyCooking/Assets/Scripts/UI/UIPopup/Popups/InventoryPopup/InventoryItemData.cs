using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryItemData
{
    private string _placedObjectTypeSOGuid;
    private InventoryTabType _tabType;
    public InventoryTabType TabType { get => _tabType; set => _tabType = value; }

    public string PlacedObjectTypeSOGuid { get => _placedObjectTypeSOGuid; set => _placedObjectTypeSOGuid = value; }
    public InventoryItemData() { }
    public InventoryItemData(string placedObjectTypeSOGuid, InventoryTabType tabType)
    {
        _placedObjectTypeSOGuid = placedObjectTypeSOGuid;
        _tabType = tabType;
    }
    public static InventoryItemData CreateInventoryItem(string id)
    {     
        if (string.IsNullOrEmpty(id) || GridBuildingSystem.Instance == null)
        {
            Debug.LogError("CreateInventoryItem: invalid inventory id or GridBuildingSystem is not ready: " + id);
            return null;
        }

        var placeObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(id);
        if (placeObjectTypeSO == null)
        {
            Debug.LogError("CreateInventoryItem: placeObjectTypeSO is null for id: " + id);
            return null;
        }

        return new InventoryItemData(placeObjectTypeSO.Guid, placeObjectTypeSO.itemType.TabType);
    }
}
