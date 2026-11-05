namespace chart;

/// <summary>Stores deterministic selected and highlighted row state.</summary>
public class ChartSelection : IChartSelection
{
    internal string SelectionId;
    internal string LinkGroup;
    internal int[] SelectedRows;
    internal int[] HighlightedRows;

    /// <summary>Initializes a selection in a linked-selection group.</summary>
    /// <param name="selectionId">The stable selection identifier.</param>
    /// <param name="linkGroup">The stable linked-selection group.</param>
    public ChartSelection(string selectionId, string linkGroup)
    {
        ChartPlot.ValidateId(selectionId);
        ChartPlot.ValidateId(linkGroup);
        SelectionId = selectionId;
        LinkGroup = linkGroup;
        SelectedRows = new int[0];
        HighlightedRows = new int[0];
    }

    /// <summary>Replaces the selected row indices.</summary>
    /// <param name="rows">The nonnegative row indices.</param>
    public void SetSelectedRows(int[] rows)
    {
        SelectedRows = NormalizeRows(rows);
    }

    /// <summary>Replaces the highlighted row indices.</summary>
    /// <param name="rows">The nonnegative row indices.</param>
    public void SetHighlightedRows(int[] rows)
    {
        HighlightedRows = NormalizeRows(rows);
    }

    /// <summary>Clears selected and highlighted rows.</summary>
    public void Clear()
    {
        SelectedRows = new int[0];
        HighlightedRows = new int[0];
    }

    /// <summary>Returns the state of one row.</summary>
    /// <param name="row">The nonnegative row index.</param>
    /// <returns>The row state.</returns>
    public ChartSelectionState StateForRow(int row)
    {
        if (row < 0)
        {
            throw new System.ArgumentOutOfRangeException("row");
        }

        bool selected = ContainsRow(SelectedRows, row);
        bool highlighted = ContainsRow(HighlightedRows, row);
        if (selected && highlighted)
        {
            return ChartSelectionState.SelectedHighlighted;
        }

        if (highlighted)
        {
            return ChartSelectionState.Highlighted;
        }

        if (selected)
        {
            return ChartSelectionState.Selected;
        }

        return ChartSelectionState.Normal;
    }

    /// <summary>Returns selected rows in ascending order.</summary>
    /// <returns>An independent row-index array.</returns>
    public int[] CopySelectedRows()
    {
        return CopyRows(SelectedRows);
    }

    /// <summary>Returns highlighted rows in ascending order.</summary>
    /// <returns>An independent row-index array.</returns>
    public int[] CopyHighlightedRows()
    {
        return CopyRows(HighlightedRows);
    }

    /// <summary>Creates an independent copy.</summary>
    /// <returns>The copied selection.</returns>
    public ChartSelection Copy()
    {
        ChartSelection copy = new ChartSelection(SelectionId, LinkGroup);
        copy.SelectedRows = CopyRows(SelectedRows);
        copy.HighlightedRows = CopyRows(HighlightedRows);
        return copy;
    }

    /// <summary>Returns the selection identifier.</summary>
    /// <returns>The selection identifier.</returns>
    public string GetSelectionId()
    {
        return SelectionId;
    }

    /// <summary>Returns the linked-selection group.</summary>
    /// <returns>The linked-selection group.</returns>
    public string GetLinkGroup()
    {
        return LinkGroup;
    }

    internal static int[] NormalizeRows(int[] rows)
    {
        if (rows == null)
        {
            throw new System.ArgumentNullException("rows");
        }

        int[] sorted = CopyRows(rows);
        System.Array.Sort(sorted);
        int count = 0;
        int index = 0;
        while (index < sorted.Length)
        {
            if (sorted[index] < 0)
            {
                throw new System.ArgumentOutOfRangeException("rows");
            }

            if (count == 0 || sorted[index] != sorted[count - 1])
            {
                sorted[count] = sorted[index];
                count = count + 1;
            }

            index = index + 1;
        }

        int[] result = new int[count];
        System.Array.Copy(sorted, result, count);
        return result;
    }

    internal static bool ContainsRow(int[] rows, int row)
    {
        int index = 0;
        while (index < rows.Length)
        {
            if (rows[index] == row)
            {
                return true;
            }

            if (rows[index] > row)
            {
                return false;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int[] CopyRows(int[] rows)
    {
        int[] copy = new int[rows.Length];
        System.Array.Copy(rows, copy, rows.Length);
        return copy;
    }
}
