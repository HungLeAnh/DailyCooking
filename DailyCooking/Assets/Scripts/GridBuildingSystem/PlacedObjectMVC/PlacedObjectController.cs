using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlacedObjectController
{
    private PlacedObjectModel model;
    private PlacedObjectView view;

    public PlacedObjectController(PlacedObjectModel model, PlacedObjectView view)
    {
        this.model = model;
        this.view = view;
    }

    public List<Vector2Int> GetGridPositionList()
    {
        return model.GetGridPositionList();
    }

    public PlacedObjectModel GetModel()
    {
        return model;
    }
}