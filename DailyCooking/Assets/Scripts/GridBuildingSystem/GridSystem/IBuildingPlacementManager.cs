using System;
using UnityEngine;

public interface IBuildingPlacementManager
{
    event EventHandler OnBuildingStart;
    event EventHandler OnBuildingEnd;
    event EventHandler<GridBuildingSystem.OnSelectedChangedArgs> OnSelectedChanged;
    event EventHandler OnObjectPlaced;
    event EventHandler<PlacedObjectTypeSO> OnReturnPlaceObjectToInventory;

    PlacedObjectTypeSO PlacedObjectTypeSO { get; }
    bool IsBuilding { get; }

    void RotateBuildingObject();
    bool TryPlaceBuildingObject(Vector3 interactPos);
    void DestroyPlaceObject(PlacedObjectView placedObjectView);
    void SetPlacedObjectTypeSO(PlacedObjectTypeSO placedObjectTypeSO, Vector3 objectPosition);
    Quaternion GetPlacedObjectRotation();
    Vector3 GetPlacedObjectRotationOffset(InventoryTabType type);
    Vector3 GetMouseWorldSnappedPosition();
    void FireOnBuildingStartEvent();
    void FireOnBuildingEndEvent();
    void HandleExistingObjectInteraction(PlacedObjectView targetPlaceObjectView,Vector3 objectPosition);
}
