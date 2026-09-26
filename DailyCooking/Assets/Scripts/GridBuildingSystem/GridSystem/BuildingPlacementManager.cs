using System;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using Unity.Netcode;

public class BuildingPlacementManager : IBuildingPlacementManager
{
    public event EventHandler OnBuildingStart;
    public event EventHandler OnBuildingEnd;
    public event EventHandler<GridBuildingSystem.OnSelectedChangedArgs> OnSelectedChanged;
    public event EventHandler OnObjectPlaced;
    public event EventHandler<PlacedObjectTypeSO> OnReturnPlaceObjectToInventory;

    private IGridManager gridManager;
    private IGridVisualizer gridVisualizer;
    private IGameManager gameManager;
    private IUIPopupManager uiPopupManager;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private Dir dir = Dir.Down;
    private bool isBuilding = false;
    public PlacedObjectTypeSO PlacedObjectTypeSO => placedObjectTypeSO;

    public bool IsBuilding => isBuilding;

    public BuildingPlacementManager(IGridManager gridManager, IGridVisualizer gridVisualizer, IGameManager gameManager, IUIPopupManager uiPopupManager)
    {
        this.gridManager = gridManager;
        this.gridVisualizer = gridVisualizer;
        this.gameManager = gameManager;
        this.uiPopupManager = uiPopupManager;
    }
    public void RotateBuildingObject()
    {
        dir = PlacedObjectTypeSO.GetNextDir(dir);
    }

    // Checks locally for instant feedback, then asks the server, which checks again against its
    // own grid and inventory and spawns the object for everyone.
    public bool TryPlaceBuildingObject(Vector3 interactPos)
    {
        if (placedObjectTypeSO == null) return false;

        Vector3 roundedPos = new Vector3(Mathf.RoundToInt(interactPos.x),
                                Mathf.RoundToInt(interactPos.y),
                                Mathf.RoundToInt(interactPos.z));
        if (!gridManager.Grid.TryGetXZ(roundedPos, out int x, out int z))
            return false;

        Vector2Int placedObjectOrigin = new Vector2Int(x, z);
        if (!GridBuildingSystem.CanPlace(gridManager.Grid, placedObjectTypeSO, placedObjectOrigin, dir, requireUnlockedCells: true))
            return false;

        GridBuildingSystem.Instance.PlaceObjectServerRpc(placedObjectTypeSO.Guid, placedObjectOrigin, dir);
        DeselectObjectType();
        return true;
    }

    public void SetPlacedObjectTypeSO(PlacedObjectTypeSO placedObjectTypeSO, Vector3 objectPosition)
    { 
        if(placedObjectTypeSO == null)
        {
            DeselectObjectType();
            return;
        }
        this.placedObjectTypeSO = placedObjectTypeSO;
        RefreshSelectedObjectType(objectPosition);
    }

    public Quaternion GetPlacedObjectRotation()
    {
        if (placedObjectTypeSO != null)
        {
            return Quaternion.Euler(0, placedObjectTypeSO.GetRotationAngle(dir), 0);
        }
        else
        {
            return Quaternion.identity;
        }
    }

    public Vector3 GetPlacedObjectRotationOffset(InventoryTabType type)
    {
        Vector2Int rotationOffset = Vector2Int.zero;
        Vector3 positionOffset = Vector3.zero;
        switch (type)
        {
            case InventoryTabType.Counter:
            case InventoryTabType.Table:
                rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                positionOffset = new Vector3(rotationOffset.x, 0, rotationOffset.y) * gridManager.GetCellSize();
                return positionOffset;
            case InventoryTabType.Wall:
                rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                //Debug.LogError($"rotate offset: {rotationOffset} - dir: {dir}");
                positionOffset = new Vector3(rotationOffset.x, 0, rotationOffset.y) * gridManager.GetCellSize();
                return positionOffset;
            default:
                return Vector3.zero;
        }

    }


    public Vector3 GetMouseWorldSnappedPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(GameInput.Instance.GetClickPosition());
        if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f))
        {     
            gridManager.GetXZ(new Vector3(Mathf.RoundToInt(raycastHit.point.x),
                                    Mathf.RoundToInt(raycastHit.point.y),
                                    Mathf.RoundToInt(raycastHit.point.z)), out int x, out int z);
            Vector2Int placedObjectOrigin = new Vector2Int(x, z);
            placedObjectOrigin = gridManager.ValidateGridPosition(placedObjectOrigin);
            if (placedObjectTypeSO != null)
            {
                Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
                Vector3 placedObjectWorldPosition = gridManager.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y);
                return placedObjectWorldPosition;
            }
            else
            {
                return -Vector3.one;
            }
        }
        else
        {
            return -Vector3.one;
        }
    }

    public void FireOnBuildingStartEvent()
    {
        OnBuildingStart?.Invoke(this, EventArgs.Empty);
        gridVisualizer.SetActiveGridGuide(true);
        gridVisualizer.ShowWallShadow(true);
        isBuilding = true;

        //PlayerStateMachine.Instance.DisableInput(true);
        GameManager.Instance.HidePlayer();
    }

    public void FireOnBuildingEndEvent()
    {
        OnBuildingEnd?.Invoke(this, EventArgs.Empty);
        gridVisualizer.SetActiveGridGuide(false);
        gridVisualizer.ShowWallShadow(false);
        isBuilding = false;

        //PlayerStateMachine.Instance.DisableInput(false);
        GameManager.Instance.ShowPlayer();  
    }

    // Moving an object: the server picks it up into the inventory, then it is placed like any
    // inventory item (the pickup RPC reaches the server before the placement RPC).
    public void HandleExistingObjectInteraction(PlacedObjectView targetPlaceObjectView,Vector3 objectPosition)
    {
        // Read before the RPC: on the host the pickup despawns the object immediately.
        PlacedObjectTypeSO pickedUpTypeSO = targetPlaceObjectView.PlacedObjectTypeSO;
        dir = targetPlaceObjectView.Dir;
        GridBuildingSystem.Instance.PickUpPlacedObjectServerRpc(targetPlaceObjectView.NetworkObject);
        SetPlacedObjectTypeSO(pickedUpTypeSO, objectPosition);

        uiPopupManager.HidePopup(UIPopupType.UIInventoryPopup,
            new UIInventoryPopup.Param { isPlacingObject = true });
    }
    // Cancelling a placement: the item never left the inventory, so there is nothing to refund.
    public void ReturnObjectToInventory()
    {
        if (this.placedObjectTypeSO != null)
        {
            OnReturnPlaceObjectToInventory?.Invoke(this, placedObjectTypeSO);
        }
    }

    private void DeselectObjectType()
    {
        placedObjectTypeSO = null;
        dir = Dir.Down;
        RefreshSelectedObjectType(-Vector3.one);
    }

    private void RefreshSelectedObjectType(Vector3 targetPosition)
    {
        //Debug.Log($"RefreshSelectedObjectType: {placedObjectTypeSO} - targetPosition: {targetPosition}");
        OnSelectedChanged?.Invoke(this, new GridBuildingSystem.OnSelectedChangedArgs 
        { 
            placedObjectTypeSO = this.placedObjectTypeSO,
            position = targetPosition
        });
    }

    public void FireOnObjectPlacedEvent()
    {
        OnObjectPlaced?.Invoke(this, EventArgs.Empty);
    }
}
