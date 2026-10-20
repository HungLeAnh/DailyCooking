using UnityEngine;
using System;
using System.Collections.Generic;

public class GridVisualizer : IGridVisualizer, IDisposable
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
        this.gridWallList = gridWallList;

        // The serialized material is the shared asset (Assets/Shaders/GridShaderMaterial.mat).
        // Material.SetFloat on it persists into the .mat file: every play session dirtied the
        // working tree and one scene's grid size leaked into the other scene using the material.
        // Work on a runtime copy instead and destroy it with the visualizer.
        this.gridGuideMaterial = gridGuideMaterial != null ? new Material(gridGuideMaterial) : null;
        ApplyGridGuideMaterial();
    }

    private void ApplyGridGuideMaterial()
    {
        if (gridGuideObject == null || gridGuideMaterial == null)
            return;

        foreach (Renderer renderer in gridGuideObject.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = gridGuideMaterial;
    }

    public void SetActiveGridGuide(bool isActive)
    {
        gridGuideObject.SetActive(isActive);
        if (gridGuideMaterial == null)
            return;

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

    public void Dispose()
    {
        if (gridGuideMaterial == null)
            return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(gridGuideMaterial);
        else
            UnityEngine.Object.DestroyImmediate(gridGuideMaterial);

        gridGuideMaterial = null;
    }
}
