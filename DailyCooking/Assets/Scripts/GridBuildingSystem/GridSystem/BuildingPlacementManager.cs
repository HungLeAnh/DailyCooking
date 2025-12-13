using System;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

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
    private ICounterModules counterModules;
    private IUIPopupManager uiPopupManager;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private Dir dir = Dir.Down;
    private bool isBuilding = false;
    private bool isPlacingWall = false;

    public enum WallDirection
    {
        Horizontal,
        Vertical
    }

    public PlacedObjectTypeSO PlacedObjectTypeSO => placedObjectTypeSO;

    public bool IsBuilding => isBuilding;

    public BuildingPlacementManager(IGridManager gridManager, IGridVisualizer gridVisualizer, IGameManager gameManager, ICounterModules counterModules, IUIPopupManager uiPopupManager)
    {
        this.gridManager = gridManager;
        this.gridVisualizer = gridVisualizer;
        this.gameManager = gameManager;
        this.counterModules = counterModules;
        this.uiPopupManager = uiPopupManager;
    }
    public void RotateBuildingObject()
    {
        dir = PlacedObjectTypeSO.GetNextDir(dir);
    }

    public bool TryPlaceBuildingObject(Vector3 interactPos)
    {
        if (placedObjectTypeSO == null) return false;

        gridManager.GetXZ(new Vector3(Mathf.RoundToInt(interactPos.x),
                                Mathf.RoundToInt(interactPos.y),
                                Mathf.RoundToInt(interactPos.z)), out int x, out int z);

        Vector2Int placedObjectOrigin = new Vector2Int(x, z);
        placedObjectOrigin = gridManager.ValidateGridPosition(placedObjectOrigin);
        
        if (placedObjectOrigin == Vector2Int.zero && (interactPos.x < 0 || interactPos.z < 0))
        {
            return false;
        }
        List<Vector2Int> gridPositionList = placedObjectTypeSO.GetGridPositionList(placedObjectOrigin, dir);

        bool canBuild = true;

        foreach (var gridPosition in gridPositionList)
        {
            var gridObject = gridManager.Grid.GetGridObject(gridPosition.x, gridPosition.y);
            if (gridObject == null || !gridObject.CanBuild())
            {
                canBuild = false;
                break;
            }
        }

        if (canBuild)
        {
            Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
            Vector3 placedObjectWorldPosition = gridManager.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) +
                new Vector3(rotationOffset.x, 0, rotationOffset.y) * gridManager.GetCellSize();
            PlacedObjectView placedObject = PlacedObjectFactory.Create(placedObjectWorldPosition, placedObjectOrigin, dir, placedObjectTypeSO);
            //register counter controller
            counterModules.AddCounterController(placedObject.GetComponent<BaseCounterController>());

            placedObject.GetComponent<IPlaceable>().IsPlaced = true;

            foreach (var gridPosition in gridPositionList)
            {
                gridManager.Grid.GetGridObject(gridPosition.x, gridPosition.y).SetPlacedObject(placedObject);

            }
            OnObjectPlaced?.Invoke(this, EventArgs.Empty);
            
            gameManager.GameData.UpdateGridData(gridManager.Grid);
            DeselectObjectType();
            return true;
        }
        else
        {
               return false;
        }
    }
    
    public bool TryPlaceWall(Vector3 position)
    {
        if (placedObjectTypeSO == null || placedObjectTypeSO.itemType.TabType != InventoryTabType.Wall) return false;

        gridManager.GetXZ(position, out int x, out int z);

        if (gridManager.Grid.GetGridObject(x, z) != null)
        {
            float cellSize = gridManager.GetCellSize();
            Vector3 cellOrigin = gridManager.GetWorldPosition(x, z);
            Vector3 clickOffset = position - cellOrigin; // Offset from bottom-left corner of the cell

            WallDirection wallDir;
            Quaternion wallRotation;
            Vector3 wallWorldPosition;

            // Determine if horizontal or vertical wall and its position/rotation
            if (Mathf.Abs(clickOffset.x) < cellSize * 0.2f) // Near left edge (vertical)
            {
                wallDir = WallDirection.Vertical;
                wallRotation = Quaternion.Euler(0, 90, 0); // Rotate 90 degrees for vertical
                wallWorldPosition = cellOrigin + new Vector3(0, 0, cellSize / 2f); // Snap to left edge
            }
            else if (Mathf.Abs(clickOffset.x - cellSize) < cellSize * 0.2f) // Near right edge (vertical)
            {
                wallDir = WallDirection.Vertical;
                wallRotation = Quaternion.Euler(0, 90, 0); // Rotate 90 degrees for vertical
                wallWorldPosition = cellOrigin + new Vector3(cellSize, 0, cellSize / 2f); // Snap to right edge
            }
            else if (Mathf.Abs(clickOffset.z) < cellSize * 0.2f) // Near bottom edge (horizontal)
            {
                wallDir = WallDirection.Horizontal;
                wallRotation = Quaternion.identity; // No rotation for horizontal
                wallWorldPosition = cellOrigin + new Vector3(cellSize / 2f, 0, 0); // Snap to bottom edge
            }
            else if (Mathf.Abs(clickOffset.z - cellSize) < cellSize * 0.2f) // Near top edge (horizontal)
            {
                wallDir = WallDirection.Horizontal;
                wallRotation = Quaternion.identity; // No rotation for horizontal
                wallWorldPosition = cellOrigin + new Vector3(cellSize / 2f, 0, cellSize); // Snap to top edge
            }
            else
            {
                // If not near an edge, place in the center (default behavior)
                wallDir = WallDirection.Horizontal; // Default to horizontal if not on edge
                wallRotation = Quaternion.identity;
                wallWorldPosition = cellOrigin + new Vector3(cellSize / 2f, 0, cellSize / 2f);
            }
            
            Transform wall = UnityEngine.Object.Instantiate(placedObjectTypeSO.prefab, wallWorldPosition, wallRotation).transform;
            return true;
        }
        return false;
    }

    public void DestroyPlaceObject(PlacedObjectView placedObjectView)
    {
        List<Vector2Int> gridPositionList = placedObjectView.GetGridPositionList();
        foreach (Vector2Int gridPosition in gridPositionList)
        {
            gridManager.Grid.GetGridObject(gridPosition.x, gridPosition.y).ClearPlacedObject();
        }
        placedObjectView.DestroySelf();
    }

    public void SetPlacedObjectTypeSO(PlacedObjectTypeSO placedObjectTypeSO, Vector3 objectPosition)
    { 
        if(placedObjectTypeSO == null)
        {
            DeselectObjectType();
            gameManager.GameData.UpdateGridData(gridManager.Grid);

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

    public Vector3 GetPlacedObjectRotationOffset()
    {
        Vector2Int rotationOffset = placedObjectTypeSO.GetRotationOffset(dir);
        Vector3 positionOffset = new Vector3(rotationOffset.x, 0, rotationOffset.y) * gridManager.GetCellSize();
        return positionOffset;
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

        PlayerStateMachine.Instance.DisableInput(true);
        GameManager.Instance.HidePlayer();
    }

    public void FireOnBuildingEndEvent()
    {
        OnBuildingEnd?.Invoke(this, EventArgs.Empty);
        gridVisualizer.SetActiveGridGuide(false);
        gridVisualizer.ShowWallShadow(false);
        isBuilding = false;

        PlayerStateMachine.Instance.DisableInput(false);
        GameManager.Instance.ShowPlayer();  
    }

    public void HandleExistingObjectInteraction(PlacedObjectView targetPlaceObjectView,Vector3 objectPosition)
    {
        // dir = targetPlaceObjectView.GetModel().Dir; 
        SetPlacedObjectTypeSO(targetPlaceObjectView.GetModel().PlacedObjectTypeSO, objectPosition);
        if (this.placedObjectTypeSO != null)
        {
            gameManager.GameData.AddInventoryData(this.placedObjectTypeSO.Guid);
            gameManager.GameData.UpdateGridData(gridManager.Grid);
            OnReturnPlaceObjectToInventory?.Invoke(this, placedObjectTypeSO);
        }
        var destroyableObject = targetPlaceObjectView.GetComponent<IDestroyable>();
        destroyableObject.DestroySelf();
        GridBuildingSystem.Instance.DestroyPlaceObject(targetPlaceObjectView);

        uiPopupManager.HidePopup(UIPopupType.UIInventoryPopup,
            new UIInventoryPopup.Param { isPlacingObject = true });
    }

    private void DeselectObjectType()
    {
        placedObjectTypeSO = null;
        dir = Dir.Down;
        RefreshSelectedObjectType(-Vector3.one);
    }

    private void RefreshSelectedObjectType(Vector3 targetPosition)
    {
        OnSelectedChanged?.Invoke(this, new GridBuildingSystem.OnSelectedChangedArgs { placedObjectTypeSO = placedObjectTypeSO,
                                            position = targetPosition});
    }
}
