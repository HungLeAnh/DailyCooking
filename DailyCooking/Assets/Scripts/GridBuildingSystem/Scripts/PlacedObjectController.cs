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
    public void HandleTouch()
    {
        // Handle the click event
        Debug.Log("PlacedObjectController.HandleClick() called!");

        // Perform actions such as selecting the object, showing a menu, etc.
    }

    internal PlacedObjectModel GetModel()
    {
        return model;
    }
}