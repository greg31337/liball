namespace chart;

/// <summary>Defines deterministic selected and highlighted row state.</summary>
public interface IChartSelection
{
    /// <summary>Replaces the selected row indices.</summary>
    /// <param name="rows">The nonnegative row indices.</param>
    void SetSelectedRows(int[] rows);
    /// <summary>Replaces the highlighted row indices.</summary>
    /// <param name="rows">The nonnegative row indices.</param>
    void SetHighlightedRows(int[] rows);
    /// <summary>Clears selected and highlighted rows.</summary>
    void Clear();
    /// <summary>Returns the state of one row.</summary>
    /// <param name="row">The nonnegative row index.</param>
    /// <returns>The row state.</returns>
    ChartSelectionState StateForRow(int row);
    /// <summary>Returns selected rows in ascending order.</summary>
    /// <returns>An independent row-index array.</returns>
    int[] CopySelectedRows();
    /// <summary>Returns highlighted rows in ascending order.</summary>
    /// <returns>An independent row-index array.</returns>
    int[] CopyHighlightedRows();
    /// <summary>Creates an independent copy.</summary>
    /// <returns>The copied selection.</returns>
    ChartSelection Copy();
    /// <summary>Returns the selection identifier.</summary>
    /// <returns>The selection identifier.</returns>
    string GetSelectionId();
    /// <summary>Returns the linked-selection group.</summary>
    /// <returns>The linked-selection group.</returns>
    string GetLinkGroup();
}
