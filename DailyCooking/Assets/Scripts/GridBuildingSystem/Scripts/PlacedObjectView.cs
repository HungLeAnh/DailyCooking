using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[Serializable]
public class PlacedObjectView : MonoBehaviour,IPointerClickHandler
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
    public void OnPointerClick(PointerEventData eventData)
    {
        // Handle the click event
        Debug.Log("PlacedObjectView clicked!");

        // You can call a method on the controller or perform other actions here
        placedObjectController.HandleClick();
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
    public void HandleClick()
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

    public PlacedObjectModel(PlacedObjectTypeSO placedObjectTypeSO, Vector2Int origin, Dir dir)
    {
        this.placedObjectTypeSO = placedObjectTypeSO;
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
public static class PlacedObjectFactory
{
    public static PlacedObjectView Create(Vector3 worldPosition, Vector2Int origin, Dir dir, PlacedObjectTypeSO placedObjectTypeSO)
    {
        Transform placedObjectTransform = UnityEngine.Object.Instantiate(placedObjectTypeSO.prefab, worldPosition, Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0)).transform;
        PlacedObjectView placedObjectView = placedObjectTransform.GetComponent<PlacedObjectView>();

        PlacedObjectModel model = new PlacedObjectModel(placedObjectTypeSO, origin, dir);
        PlacedObjectController controller = new PlacedObjectController(model, placedObjectView);

        placedObjectView.Initialize(controller);

        return placedObjectView;
    }
}