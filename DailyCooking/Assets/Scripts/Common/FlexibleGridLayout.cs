using UnityEngine;
using UnityEngine.UI;

public class FlexibleGridLayout : LayoutGroup
{
    public enum FitType
    {
        Uniform,
        Width,
        Height,
        FixedRows,
        FixedColumns
    }

    [Tooltip("The type of fit to use for the grid layout.")]
    public FitType fitType;

    [Tooltip("Number of rows (used if Fit Type is Fixed Rows).")]
    public int rows;

    [Tooltip("Number of columns (used if Fit Type is Fixed Columns).")]
    public int columns;

    [Tooltip("Size of each cell.")]
    public Vector2 cellSize;

    [Tooltip("Spacing between cells.")]
    public Vector2 spacing;

    [Tooltip("Whether to force the cell width to fit the container.")]
    public bool fitX;

    [Tooltip("Whether to force the cell height to fit the container.")]
    public bool fitY;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        // Check if we are using an automatic fit type
        if (fitType == FitType.Uniform || fitType == FitType.Width || fitType == FitType.Height)
        {
            fitX = true;
            fitY = true;

            // Calculate number of rows/columns based on square root of child count
            float sqrRt = Mathf.Sqrt(transform.childCount);
            rows = Mathf.CeilToInt(sqrRt);
            columns = Mathf.CeilToInt(sqrRt);
        }

        // Adjust rows/columns if strictly Width (Fixed Columns) or Height (Fixed Rows) logic is needed
        if (fitType == FitType.Width || fitType == FitType.FixedColumns)
        {
            rows = Mathf.CeilToInt(transform.childCount / (float)columns);
        }
        if (fitType == FitType.Height || fitType == FitType.FixedRows)
        {
            columns = Mathf.CeilToInt(transform.childCount / (float)rows);
        }

        // Calculate available space
        float parentWidth = rectTransform.rect.width;
        float parentHeight = rectTransform.rect.height;

        // Calculate cell width and height based on the container size, padding, and spacing
        float cellWidth = (parentWidth / (float)columns) - ((spacing.x / (float)columns) * (columns - 1)) - (padding.left / (float)columns) - (padding.right / (float)columns);
        float cellHeight = (parentHeight / (float)rows) - ((spacing.y / (float)rows) * (rows - 1)) - (padding.top / (float)rows) - (padding.bottom / (float)rows);

        // Apply calculated sizes if fit is enabled, otherwise use manually set cellSize
        cellSize.x = fitX ? cellWidth : cellSize.x;
        cellSize.y = fitY ? cellHeight : cellSize.y;

        int columnCount = 0;
        int rowCount = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            rowCount = i / columns;
            columnCount = i % columns;

            var item = rectChildren[i];

            var xPos = (cellSize.x * columnCount) + (spacing.x * columnCount) + padding.left;
            var yPos = (cellSize.y * rowCount) + (spacing.y * rowCount) + padding.top;

            SetChildAlongAxis(item, 0, xPos, cellSize.x);
            SetChildAlongAxis(item, 1, yPos, cellSize.y);
        }
    }

    public override void CalculateLayoutInputVertical()
    {
        // The logic is handled in CalculateLayoutInputHorizontal per the video's implementation.
    }

    public override void SetLayoutHorizontal()
    {
        // The logic is handled in CalculateLayoutInputHorizontal per the video's implementation.
    }

    public override void SetLayoutVertical()
    {
        // The logic is handled in CalculateLayoutInputHorizontal per the video's implementation.
    }
}
