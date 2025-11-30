using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// A custom LayoutGroup that arranges children in a grid.
/// This version supports content-based sizing where cell dimensions
/// are determined by the largest preferred size of the children in that row/column.
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

    [Tooltip("If TRUE, stretches cell width to fill the container. (Not used for Auto Cell Size).")]
    public bool fitX; // Retained for compatibility but minimized use

    [Tooltip("If TRUE, stretches cell height to fill the container. (Not used for Auto Cell Size).")]
    public bool fitY; // Retained for compatibility but minimized use

    private List<float> columnWidths = new List<float>();
    private List<float> rowHeights = new List<float>();

    private int actualRows;
    private int actualColumns;

    /// <summary>
    /// Calculates the minimum and preferred width for the entire layout group.
    /// This also calculates and stores the individual column widths based on the max preferred width of children.
    /// </summary>
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();

        // --- 1. Determine Actual Rows and Columns ---
        int childCount = rectChildren.Count;
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

        // Ensure minimum 1x1 grid for empty group
        actualColumns = Mathf.Max(1, actualColumns);
        actualRows = Mathf.Max(1, actualRows);

        // --- 2. Calculate Max Preferred Width for Each Column ---
        columnWidths.Clear();
        for (int c = 0; c < actualColumns; c++)
        {
            columnWidths.Add(0f); // Initialize width for each column
        }

        float totalMinWidth = 0;
        float totalPreferredWidth = 0;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = rectChildren[i];
            int col = i % actualColumns;

            // Get the child's preferred width (based on its content/LayoutElement)
            float preferredWidth = LayoutUtility.GetPreferredSize(child, 0);

            // The column width is determined by the maximum preferred width found in that column
            columnWidths[col] = Mathf.Max(columnWidths[col], preferredWidth);
        }

        // --- 3. Calculate Parent's Required Size ---
        // Sum up the calculated column widths
        foreach (float width in columnWidths)
        {
            totalPreferredWidth += width;
        }

        // Add spacing and padding
        totalPreferredWidth += spacing.x * (actualColumns - 1);
        totalPreferredWidth += padding.left + padding.right;

        // For simplicity in this auto-sizing logic, Min and Preferred are often the same
        totalMinWidth = totalPreferredWidth;

        // Set the calculated size for the parent
        SetLayoutInputForAxis(totalMinWidth, totalPreferredWidth, -1, 0);
    }

    /// <summary>
    /// Calculates the minimum and preferred height for the entire layout group.
    /// This also calculates and stores the individual row heights based on the max preferred height of children.
    /// </summary>
    public override void CalculateLayoutInputVertical()
    {
        // Must call horizontal first to ensure actualRows and actualColumns are set
        if (columnWidths.Count == 0) CalculateLayoutInputHorizontal();

        // --- 1. Calculate Max Preferred Height for Each Row ---
        rowHeights.Clear();
        for (int r = 0; r < actualRows; r++)
        {
            rowHeights.Add(0f); // Initialize height for each row
        }

        float totalMinHeight = 0;
        float totalPreferredHeight = 0;

        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            int row = i / actualColumns;

            // Get the child's preferred height (based on its content/LayoutElement)
            float preferredHeight = LayoutUtility.GetPreferredSize(child, 1);

            // The row height is determined by the maximum preferred height found in that row
            rowHeights[row] = Mathf.Max(rowHeights[row], preferredHeight);
        }

        // --- 2. Calculate Parent's Required Size ---
        // Sum up the calculated row heights
        foreach (float height in rowHeights)
        {
            totalPreferredHeight += height;
        }

        // Add spacing and padding
        totalPreferredHeight += spacing.y * (actualRows - 1);
        totalPreferredHeight += padding.top + padding.bottom;

        // For simplicity in this auto-sizing logic, Min and Preferred are often the same
        totalMinHeight = totalPreferredHeight;

        // Set the calculated size for the parent
        SetLayoutInputForAxis(totalMinHeight, totalPreferredHeight, -1, 1);
    }

    /// <summary>
    /// Sets the positions and sizes of the children along the horizontal axis.
    /// </summary>
    public override void SetLayoutHorizontal()
    {
        // Starting X position
        float currentX = padding.left;

        // Set the size and position for each child
        for (int i = 0; i < rectChildren.Count; i++)
        {
            int col = i % actualColumns;

            // The width of the cell is the maximum preferred width for that column
            float cellWidth = columnWidths[col];

            // If we are starting a new column (i.e., we are not in the first column)
            if (col > 0)
            {
                // Advance X position by the width of the previous column and spacing
                currentX += columnWidths[col - 1] + spacing.x;
            }
            else
            {
                // Reset X position to the starting padding for the beginning of the row
                currentX = padding.left;
            }

            // Set the horizontal position and size of the child
            // Note: The child is sized to the cellWidth, regardless of its own preferred width.
            SetChildAlongAxis(rectChildren[i], 0, currentX, cellWidth);
        }
    }

    /// <summary>
    /// Sets the positions and sizes of the children along the vertical axis.
    /// </summary>
    public override void SetLayoutVertical()
    {
        // Starting Y position
        float currentY = padding.top;

        // Set the size and position for each child
        for (int i = 0; i < rectChildren.Count; i++)
        {
            int row = i / actualColumns;
            int col = i % actualColumns;

            // The height of the cell is the maximum preferred height for that row
            float cellHeight = rowHeights[row];

            // If we are starting a new column (i.e., we are not in the first column)
            if (col == 0 && row > 0)
            {
                // Advance Y position by the height of the previous row and spacing
                currentY += rowHeights[row - 1] + spacing.y;
            }
            else if (row == 0 && col == 0)
            {
                // Reset Y position to the starting padding for the beginning of the first item
                currentY = padding.top;
            }

            // Set the vertical position and size of the child
            // Note: The child is sized to the cellHeight, regardless of its own preferred height.
            // We use the negative Y-axis convention for UI (top-to-bottom)
            SetChildAlongAxis(rectChildren[i], 1, currentY, cellHeight);
        }
    }
}