namespace chart;

/// <summary>Defines deterministic metadata for linked selections.</summary>
public interface IChartSelectionSidecar
{
    /// <summary>Adds a snapshot of linked selection state.</summary>
    /// <param name="selection">The selection to snapshot.</param>
    void AddSelection(ChartSelection selection);
    /// <summary>Returns the requested selection snapshot.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The selection snapshot.</returns>
    ChartSelection SelectionAt(int index);
    /// <summary>Adds a deterministic snapshot of linked brush geometry and rows.</summary>
    /// <param name = "brush">The brush to snapshot.</param>
    void AddBrush(ChartBrush brush);
    /// <summary>Returns the requested linked brush snapshot.</summary>
    /// <param name = "index">The zero-based brush index.</param>
    /// <returns>An independent brush snapshot.</returns>
    ChartBrush BrushAt(int index);
    /// <summary>Returns the sorted union of rows brushed in one linked-selection group.</summary>
    /// <param name = "linkGroup">The linked-selection group.</param>
    /// <returns>An independent sorted row-index array.</returns>
    int[] CopyLinkedRows(string linkGroup);
    /// <summary>Creates a selected-row snapshot resolved from all brushes in one link group.</summary>
    /// <param name = "selectionId">The stable target selection identifier.</param>
    /// <param name = "linkGroup">The linked-selection group.</param>
    /// <returns>The resolved target selection.</returns>
    ChartSelection CreateLinkedSelection(string selectionId, string linkGroup);
    /// <summary>Returns deterministic linked-selection metadata text.</summary>
    /// <returns>The metadata text.</returns>
    string MetadataText();
    /// <summary>Creates an independent copy.</summary>
    /// <returns>The copied sidecar.</returns>
    ChartSelectionSidecar Copy();
    /// <summary>Returns the number of selection snapshots.</summary>
    /// <returns>The selection count.</returns>
    int GetSelectionCount();
    /// <summary>Returns the number of brush snapshots.</summary>
    /// <returns>The brush count.</returns>
    int GetBrushCount();
}
