using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

[Serializable]
public class PlacedObjectView : NetworkBehaviour
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
}