using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class GridObject
{
    [JsonIgnore]
    private GridXZ<GridObject> grid;
    [JsonIgnore]
    private PlacedObjectView placedObject;

    private int x;
    private int z;

    public GridObject(GridXZ<GridObject> grid, int x, int z)
    {
        this.grid = grid;
        this.x = x;
        this.z = z;
    }
    public GridObject(GridXZ<GridObject> grid, PlacedObjectView placedObject, int x, int z)
    {
        this.grid = grid;
        this.x = x;
        this.z = z;
        this.placedObject = placedObject;
    }
    public PlacedObjectView GetPlacedObject()
    {
        return placedObject;
    }

    public bool CanBuild()
    {
        if(placedObject != null)
        {
            if (placedObject.GetModel().InventoryTabType == InventoryTabType.Counter||
                placedObject.GetModel().InventoryTabType == InventoryTabType.Table)
                return false;
            if (placedObject.GetModel().InventoryTabType == InventoryTabType.Wall)
                return true;
        }
        return true;
    }
    public bool CanBuild(InventoryTabType type,Dir dir)
    {
        if (placedObject != null)
        {
            if(type == InventoryTabType.Wall &&
                placedObject.GetModel().InventoryTabType == InventoryTabType.Wall)
            {
                if(dir != placedObject.GetModel().Dir)
                    return true;
                else
                    return false;
            }
            else if(type == InventoryTabType.Wall && 
                placedObject.GetModel().InventoryTabType != InventoryTabType.Wall)
                return true;
            else if(placedObject.GetModel().InventoryTabType == InventoryTabType.Counter || 
                    placedObject.GetModel().InventoryTabType == InventoryTabType.Table)
                return false;
        }
        return true;
    }
    public override string ToString()
    {
        return x + ", " + z;
    }

}