using System;
using UnityEngine;

public class WallPlacementManager : IWallPlacementManager
{
    private IGridManager gridManager;
    private IGridVisualizer gridVisualizer;
    private IGameManager gameManager;
    private PlacedObjectTypeSO placedObjectTypeSO;
    private bool isActive;

    public WallPlacementManager(IGridManager gridManager, IGridVisualizer gridVisualizer, IGameManager gameManager)
    {
        this.gridManager = gridManager;
        this.gridVisualizer = gridVisualizer;
        this.gameManager = gameManager;
    }

    public void SetActive(bool isActive)
    {
        this.isActive = isActive;
        if (isActive)
        {
            GameInput.Instance.OnMouseClickPerformed += GameInput_OnMouseClickPerformed;
        }
        else
        {
            GameInput.Instance.OnMouseClickPerformed -= GameInput_OnMouseClickPerformed;
        }
    }

    public void SetWallObjectType(PlacedObjectTypeSO placedObjectTypeSO)
    {
        this.placedObjectTypeSO = placedObjectTypeSO;
    }

    private void GameInput_OnMouseClickPerformed(object sender, Vector2 e)
    {
        if (!isActive) return;

        TryPlaceWall(GameInput.Instance.GetClickPosition());
    }

    public bool TryPlaceWall(Vector3 position)
    {
        if (placedObjectTypeSO == null) return false;

        gridManager.GetXZ(position, out int x, out int z);

        if (gridManager.Grid.GetGridObject(x, z) != null)
        {
            // For simplicity, we'll place a wall at the grid position.
            // More complex logic could be added here to check for existing objects, etc.
            Vector3 worldPosition = gridManager.GetWorldPosition(x, z);
            Transform wall = UnityEngine.Object.Instantiate(placedObjectTypeSO.prefab, worldPosition, Quaternion.identity);
            return true;
        }
        return false;
    }
}
