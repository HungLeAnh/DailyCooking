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