using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// A custom LayoutGroup that arranges children in a grid.
/// This version supports content-based sizing and optional content wrapping.
/// </summary>
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

    [Tooltip("How the grid determines the number of rows and columns if not wrapping.")]
    [SerializeField] private FitType fitType;

    [Tooltip("If TRUE, content will wrap to the next line when it exceeds the parent's width.")]
    [SerializeField] private bool wrapContent;

    [Tooltip("Number of rows (used if Fit Type is Fixed Rows and not wrapping).")]
    [SerializeField] private int rows;

    [Tooltip("Number of columns (used if Fit Type is Fixed Columns and not wrapping).")]
    [SerializeField] private int columns;

    [Tooltip("Spacing between cells.")]
    [SerializeField] private Vector2 spacing;

    [Tooltip("Size of each cell.")]
    [SerializeField] private Vector2 cellSize;

    [Tooltip("Whether to force the cell width to fit the container.")]
    public bool fitX;

    [Tooltip("Whether to force the cell height to fit the container.")]
    public bool fitY;

    private int actualRows;
    private int actualColumns;
    
    // For wrapping logic
    private List<Rect> cellRects = new List<Rect>();
    private float totalHeight;

    private void CalculateWrappingLayout()
    {
        cellRects.Clear();
        totalHeight = 0;

        if (rectChildren.Count == 0) return;

        float parentWidth = rectTransform.rect.width;
        float currentX = padding.left;
        float currentY = padding.top;
        float currentRowMaxHeight = 0;
        int firstIndexOfCurrentRow = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            float childWidth = LayoutUtility.GetPreferredSize(rectChildren[i], 0);
            float childHeight = LayoutUtility.GetPreferredSize(rectChildren[i], 1);

            // If the item doesn't fit and it's not the first item in the row
            if (currentX + childWidth > parentWidth - padding.right && i > firstIndexOfCurrentRow)
            {
                // Finalize heights for the completed row
                FinalizeRowHeight(currentRowMaxHeight, firstIndexOfCurrentRow, i);

                // Start new row
                currentX = padding.left;
                currentY += currentRowMaxHeight + spacing.y;
                currentRowMaxHeight = 0;
                firstIndexOfCurrentRow = i;
            }

            cellRects.Add(new Rect(currentX, currentY, childWidth, childHeight));
            currentX += childWidth + spacing.x;
            currentRowMaxHeight = Mathf.Max(currentRowMaxHeight, childHeight);
        }

        // Finalize heights for the last row
        FinalizeRowHeight(currentRowMaxHeight, firstIndexOfCurrentRow, rectChildren.Count);

        totalHeight = currentY + currentRowMaxHeight + padding.bottom;
    }

    private void FinalizeRowHeight(float currentRowMaxHeight, int firstIndexOfCurrentRow, int i)
    {
        for (int j = firstIndexOfCurrentRow; j < i; j++)
        {
            Rect rect = cellRects[j];
            rect.height = currentRowMaxHeight;
            cellRects[j] = rect;
        }
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        if (wrapContent)
        {
            CalculateWrappingLayout();
            float parentWidth = rectTransform.rect.width;
            SetLayoutInputForAxis(parentWidth, parentWidth, -1, 0);
        }
        else
        {
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
    }

    public override void CalculateLayoutInputVertical()
    {
        if (wrapContent)
        {
            SetLayoutInputForAxis(totalHeight, totalHeight, -1, 1);
        }
        else
        {
            if (rectChildren.Count == 0)
            {
                SetLayoutInputForAxis(padding.top + padding.bottom, padding.top + padding.bottom, -1, 1);
                return;
            }

            int totalRows = rows > 0 ? rows : 1;
            float totalPreferredHeight = (cellSize.y * totalRows)
                + (spacing.y * (totalRows - 1))
                + padding.top + padding.bottom;

            SetLayoutInputForAxis(totalPreferredHeight, totalPreferredHeight, -1, 1);
        }
    }

    public override void SetLayoutHorizontal()
    {
        if (wrapContent)
        {
            for(int i = 0; i < rectChildren.Count; i++)
            {
                SetChildAlongAxis(rectChildren[i], 0, cellRects[i].x, cellRects[i].width);
            }
        }
        else
        {
            //float currentX = padding.left;
            //int currentRow = 0;

            //for (int i = 0; i < rectChildren.Count; i++)
            //{
            //    int row = i / actualColumns;

            //    if (row != currentRow)
            //    {
            //        currentX = padding.left;
            //        currentRow = row;
            //    }

            //    float cellWidth = LayoutUtility.GetPreferredSize(rectChildren[i], 0);
            //    SetChildAlongAxis(rectChildren[i], 0, currentX, cellWidth);
            //    currentX += cellWidth + spacing.x;
            //}
        }
    }

    public override void SetLayoutVertical()
    {
        if (wrapContent)
        {
            for(int i = 0; i < rectChildren.Count; i++)
            {
                SetChildAlongAxis(rectChildren[i], 1, cellRects[i].y, cellRects[i].height);
            }
        }
        else
        {
            //if(rectChildren.Count == 0) return;
            
            //float[] rowHeights = new float[actualRows];
            //for (int r = 0; r < actualRows; r++)
            //{
            //    float maxCellHeightInRow = 0;
            //    for (int c = 0; c < actualColumns; c++)
            //    {
            //        int index = r * actualColumns + c;
            //        if (index < rectChildren.Count)
            //        {
            //            maxCellHeightInRow = Mathf.Max(maxCellHeightInRow, LayoutUtility.GetPreferredSize(rectChildren[index], 1));
            //        }
            //    }
            //    rowHeights[r] = maxCellHeightInRow;
            //}

            //float currentY = padding.top;
            //for (int i = 0; i < rectChildren.Count; i++)
            //{
            //    int row = i / actualColumns;
            //    if (i % actualColumns == 0 && i > 0)
            //    {
            //        currentY += rowHeights[row - 1] + spacing.y;
            //    }
            //    SetChildAlongAxis(rectChildren[i], 1, currentY, rowHeights[row]);
            //}
        }
    }
}