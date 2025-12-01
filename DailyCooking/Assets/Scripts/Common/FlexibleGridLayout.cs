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
                currentRowWidth += spacing.x * (Mathf.Min(actualColumns, childCount - r * actualColumns) - 1);
                maxRowWidth = Mathf.Max(maxRowWidth, currentRowWidth);
            }

            maxRowWidth += padding.left + padding.right;
            SetLayoutInputForAxis(maxRowWidth, maxRowWidth, -1, 0);
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
}