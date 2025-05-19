using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryItem 
{
    [Tooltip("The name of the item")]
    private string _name = default;

    [Tooltip("A preview image for the item")]
    private Sprite _previewImage = default;

    [Tooltip("A description of the item")]
    private string _description = default;

    private PlacedObjectTypeSO _placedObjectTypeSO = default;


    public string Name {get => _name; set => _name = value; }
    public Sprite PreviewImage { get => _previewImage; set => _previewImage = value; }
    public string Description { get => _description; set => _description = value; }

    public PlacedObjectTypeSO placedObjectTypeSO { get => _placedObjectTypeSO; set => _placedObjectTypeSO = value; }
    public virtual List<ItemStack> IngredientsList { get; }
    public ItemType ItemType => _placedObjectTypeSO.itemType;
}

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
    public static InventoryItemData CreateInventoryItem(string id,bool isGuid = false)
    {
        if (isGuid)
        {
            var placeObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(id);
            var inventoryItem = new InventoryItemData(placeObjectTypeSO.Guid, placeObjectTypeSO.itemType.TabType);
            return inventoryItem;
        }
        else
        {        
            var placeObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOById(id);
            var inventoryItem = new InventoryItemData(placeObjectTypeSO.Guid, placeObjectTypeSO.itemType.TabType);
            return inventoryItem;
        }

    }
}
