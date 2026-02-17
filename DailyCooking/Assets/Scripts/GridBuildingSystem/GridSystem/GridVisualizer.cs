using UnityEngine;
using System.Collections.Generic;

public class GridVisualizer : IGridVisualizer
{
    private IGridManager gridManager;
    private GameObject gridGuideObject;
    private Material gridGuideMaterial;
    private List<GridWall> gridWallList;

    private const float GRID_OFFSET = 0.01f;

    public GridVisualizer(IGridManager gridManager, GameObject gridGuideObject, Material gridGuideMaterial, List<GridWall> gridWallList)
    {
        this.gridManager = gridManager;
        this.gridGuideObject = gridGuideObject;
        this.gridGuideMaterial = gridGuideMaterial;
        this.gridWallList = gridWallList;
    }

    public void SetActiveGridGuide(bool isActive)
    {
        gridGuideObject.SetActive(isActive);
        gridGuideMaterial.SetFloat("_GridWidthMax", gridManager.GetWidthMax() + GRID_OFFSET);
        gridGuideMaterial.SetFloat("_GridHeightMax", gridManager.GetHeightMax() + GRID_OFFSET);
        gridGuideMaterial.SetFloat("_GridHeightMin", gridManager.GetHeightMin());
        gridGuideMaterial.SetFloat("_GridWidthMin", gridManager.GetWidthMin());
        gridGuideMaterial.SetVector("_CellSize", new Vector2(gridManager.GetCellSize(), gridManager.GetCellSize()));
    }

    public void ShowWallShadow(bool isShow)
    {
        foreach (var wall in gridWallList)
        {
            if (isShow)
            {
                wall.OnGridEdit();
            }
            else
            {
                wall.OnExitGridEdit();
            }
            
        }
    }
}
