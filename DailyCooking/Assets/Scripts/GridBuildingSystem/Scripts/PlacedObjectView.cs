using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[Serializable]
public class PlacedObjectView : MonoBehaviour
{
    private PlacedObjectController placedObjectController;

    public void Initialize(PlacedObjectController controller)
    {
        placedObjectController = controller;
    }

    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    public List<Vector2Int> GetGridPositionList()
    {
        return placedObjectController.GetGridPositionList();
    }
    public PlacedObjectModel GetModel()
    {
        return placedObjectController.GetModel();
    }
    public void OnTouch()
    {
        // Handle the click event
        Debug.Log("PlacedObjectView clicked!");

        // You can call a method on the controller or perform other actions here
        placedObjectController.HandleTouch();
    }
}
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
[Serializable]
public class PlacedObjectModel
{
    private PlacedObjectTypeSO placedObjectTypeSO;
    private Vector2Int origin;
    private Dir dir;
    public Vector2Int Origin => origin; 
    public Dir Dir => dir;
    public PlacedObjectTypeSO PlacedObjectTypeSO => placedObjectTypeSO;
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

    internal string GetPlacedObjectTypeSOGuid()
    {
        return placedObjectTypeSO.Guid;
    }
}
