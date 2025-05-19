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
    public void SetPlacedObject(PlacedObjectView placedObject)
    {
        this.placedObject = placedObject;
        grid.TriggerGridObjectChanged(x,z);
    }
    public PlacedObjectView GetPlacedObject()
    {
        return placedObject;
    }

    public bool CanBuild()
    {
        return placedObject == null;
    }
    public void ClearPlacedObject()
    {
        placedObject = null;
        grid.TriggerGridObjectChanged(x, z);
    }
    public override string ToString()
    {
        return x + ", " + z;
    }

}