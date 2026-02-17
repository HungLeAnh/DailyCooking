using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlacedObjectModel
{
    private PlacedObjectTypeSO placedObjectTypeSO;
    private Vector2Int origin;
    private Dir dir;
    public Vector2Int Origin => origin; 
    public Dir Dir => dir;
    public PlacedObjectTypeSO PlacedObjectTypeSO => placedObjectTypeSO;
    public InventoryTabType InventoryTabType => placedObjectTypeSO.itemType.TabType;
    public PlacedObjectModel(PlacedObjectTypeSO placedObjectTypeSO, Vector2Int origin, Dir dir)
    {
        this.placedObjectTypeSO = placedObjectTypeSO;
        this.origin = origin;
        this.dir = dir;
    }
    public void UpdateDirAndOrigin(Vector2Int origin, Dir dir)
    {
        this.origin = origin;
        this.dir = dir;
    }

    public List<Vector2Int> GetGridPositionList()
    {
        return placedObjectTypeSO.GetGridPositionList(origin, dir);
    }

    public override string ToString()
    {
        return placedObjectTypeSO.nameString;
    }

    public string GetPlacedObjectTypeSOGuid()
    {
        return placedObjectTypeSO.Guid;
    }
}