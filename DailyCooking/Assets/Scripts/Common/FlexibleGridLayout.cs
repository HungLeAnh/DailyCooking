using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// A custom LayoutGroup that arranges children in a grid.
/// This version supports content-based sizing where cell dimensions
/// are determined by the largest preferred size of the children in that row/column.
/// This modified version allows each row to have different column widths.
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

    [Tooltip("How the grid determines the number of rows and columns.")]
    public FitType fitType;

    [Tooltip("Number of rows (used if Fit Type is Fixed Rows).")]
    public int rows;

    [Tooltip("Number of columns (used if Fit Type is Fixed Columns).")]
    public int columns;

    [Tooltip("Spacing between cells.")]
    public Vector2 spacing;

    // These are not used in the new logic but kept for component compatibility.
    [Tooltip("If TRUE, stretches cell width to fill the container. (Not used for Auto Cell Size).")]
    public bool fitX;
    [Tooltip("If TRUE, stretches cell height to fill the container. (Not used for Auto Cell Size).")]
    public bool fitY;

    private int actualRows;
    private int actualColumns;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        int childCount = rectChildren.Count;
        if (childCount == 0)
        {
            SetLayoutInputForAxis(0, 0, 0, 0);
            return;
        }

        if (fitType == FitType.Uniform || fitType == FitType.Width || fitType == FitType.Height)
        {
            float sqrRt = Mathf.Sqrt(childCount);
            actualRows = Mathf.CeilToInt(sqrRt);
            actualColumns = Mathf.CeilToInt(sqrRt);
        }
        else if (fitType == FitType.FixedColumns)
        {
            actualColumns = columns > 0 ? columns : 1;
            actualRows = Mathf.CeilToInt(childCount / (float)actualColumns);
        }
        else // FitType.FixedRows
        {
            actualRows = rows > 0 ? rows : 1;
            actualColumns = Mathf.CeilToInt(childCount / (float)actualRows);
        }
        
        actualColumns = Mathf.Max(1, actualColumns);
        actualRows = Mathf.Max(1, actualRows);

        float maxRowWidth = 0;
        for (int r = 0; r < actualRows; r++)
        {
            float currentRowWidth = 0;
            for (int c = 0; c < actualColumns; c++)
            {
                int index = r * actualColumns + c;
                if (index < childCount)
                {
                    currentRowWidth += LayoutUtility.GetPreferredSize(rectChildren[index], 0);
                }
            }
            // Add spacing for the items in the current row
            currentRowWidth += spacing.x * (Mathf.Min(actualColumns, childCount - r * actualColumns) - 1);
            maxRowWidth = Mathf.Max(maxRowWidth, currentRowWidth);
        }

        maxRowWidth += padding.left + padding.right;
        SetLayoutInputForAxis(maxRowWidth, maxRowWidth, -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        if (rectChildren.Count == 0)
        {
            SetLayoutInputForAxis(0, 0, 0, 1);
            return;
        }
        
        float totalPreferredHeight = 0;
        for (int r = 0; r < actualRows; r++)
        {
            float maxCellHeightInRow = 0;
            for (int c = 0; c < actualColumns; c++)
            {
                int index = r * actualColumns + c;
                if (index < rectChildren.Count)
                {
                    maxCellHeightInRow = Mathf.Max(maxCellHeightInRow, LayoutUtility.GetPreferredSize(rectChildren[index], 1));
                }
            }
            totalPreferredHeight += maxCellHeightInRow;
        }

        totalPreferredHeight += spacing.y * (actualRows - 1);
        totalPreferredHeight += padding.top + padding.bottom;
        SetLayoutInputForAxis(totalPreferredHeight, totalPreferredHeight, -1, 1);
    }

    public override void SetLayoutHorizontal()
    {
        float currentX = padding.left;
        int currentRow = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            int row = i / actualColumns;

            if (row != currentRow)
            {
                currentX = padding.left;
                currentRow = row;
            }

            float cellWidth = LayoutUtility.GetPreferredSize(rectChildren[i], 0);
            SetChildAlongAxis(rectChildren[i], 0, currentX, cellWidth);
            currentX += cellWidth + spacing.x;
        }
    }

    public override void SetLayoutVertical()
    {
        if(rectChildren.Count == 0) return;
        
        float[] rowHeights = new float[actualRows];
        for (int r = 0; r < actualRows; r++)
        {
            float maxCellHeightInRow = 0;
            for (int c = 0; c < actualColumns; c++)
            {
                int index = r * actualColumns + c;
                if (index < rectChildren.Count)
                {
                    maxCellHeightInRow = Mathf.Max(maxCellHeightInRow, LayoutUtility.GetPreferredSize(rectChildren[index], 1));
                }
            }
            rowHeights[r] = maxCellHeightInRow;
        }

        float currentY = padding.top;
        for (int i = 0; i < rectChildren.Count; i++)
        {
            int row = i / actualColumns;
            if (i % actualColumns == 0 && i > 0)
            {
                currentY += rowHeights[row - 1] + spacing.y;
            }
            SetChildAlongAxis(rectChildren[i], 1, currentY, rowHeights[row]);
        }
    }
}
