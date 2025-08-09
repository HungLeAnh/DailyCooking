using UnityEngine;

public static class PlacedObjectFactory
{
    public static PlacedObjectView Create(Vector3 worldPosition, Vector2Int origin, Dir dir, PlacedObjectTypeSO placedObjectTypeSO)
    {
        Transform placedObjectTransform = UnityEngine.Object.Instantiate(placedObjectTypeSO.prefab, worldPosition, Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0),GridBuildingSystem.Instance.Container).transform;
        PlacedObjectView placedObjectView = placedObjectTransform.GetComponent<PlacedObjectView>();
        PlacedObjectModel model = new PlacedObjectModel(placedObjectTypeSO, origin, dir);
        PlacedObjectController controller = new PlacedObjectController(model, placedObjectView);
        placedObjectView.Initialize(controller);

        var counterView = placedObjectView.GetComponent<BaseCounterView>();
        if(CounterModules.Instance.IsInited)
            CounterModules.Instance.AddNewCounterController(counterView);

        return placedObjectView;
    }
}