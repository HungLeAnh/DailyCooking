using System.Collections.Generic;
using UnityEngine;

public class TestGridShape : MonoBehaviour
{
    private GameObject gridGuideObject;
    [SerializeField] private Material gridGuideMaterial;

    private const float GRID_OFFSET = 0.01f;
    private float gridWidthMax = 5f;
    private float gridHeightMax = 5f;

    private float gridWidthMin = 5f;
    private float gridHeightMin = 5f;

    private float cellSize = 2f;

    private void Start()
    {
        SetUpGridGuide();
    }
    public void SetUpGridGuide()
    {
        gridGuideMaterial.SetFloat("_GridWidthMax", gridWidthMax);
        gridGuideMaterial.SetFloat("_GridHeightMax", gridHeightMax);
        gridGuideMaterial.SetFloat("_GridHeightMin", gridHeightMin);
        gridGuideMaterial.SetFloat("_GridWidthMin", gridWidthMin);
        gridGuideMaterial.SetVector("_CellSize", new Vector2(cellSize, cellSize));
    }
    public void OnClickExpandGrid()
    {
        if (gridHeightMax == gridWidthMax)
        {
            if(gridWidthMin < gridWidthMax)
            {
                gridWidthMin += 5f;
            }
            else
            {
                gridWidthMax += 5f;
                gridHeightMin = 5f;
            }
        }
        else if (gridHeightMax < gridWidthMax)
        {
            if(gridHeightMin < gridHeightMax)
            {
                gridHeightMin += 5f;
            }
            else
            {
                gridHeightMax += 5f;
                gridWidthMin = 5f;
            }
        }
        SetUpGridGuide();
    }
}
