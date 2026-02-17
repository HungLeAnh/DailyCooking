using System;
using UnityEngine;

[Serializable]
public class GridObjectData
{
    private string _placedObjectTypeSOGuid;
    private Vector2Int origin;
    private Dir dir;
    private InventoryTabType type;
    public Dir Dir { get => dir; set => dir = value; }
    public Vector2Int Origin { get => origin; set => origin = value; }
    public string PlacedObjectTypeSOGuid { get => _placedObjectTypeSOGuid; set => _placedObjectTypeSOGuid = value; }
    public InventoryTabType Type { get => type; set => type = value; }

    public GridObjectData(string placedObjectTypeSOGuid, Vector2Int origin, Dir dir,InventoryTabType type)
    {
        _placedObjectTypeSOGuid = placedObjectTypeSOGuid;
        this.origin = origin;
        this.dir = dir;
        this.type = type;
    }

}