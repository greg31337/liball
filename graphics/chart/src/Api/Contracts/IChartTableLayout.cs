namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartTableLayout"/>.</summary>
public interface IChartTableLayout
{
    /// <summary>
    /// Adds the supplied cell to this object.
    /// </summary>
    /// <param name = "cell">The cell.</param>
    /// <returns>The resulting table cell layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartTableCellLayout AddCell(ChartTableCellLayout cell);
    /// <summary>
    /// Returns the requested cell.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting table cell layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartTableCellLayout CellAt(int index);
    /// <summary>Returns the current row count.</summary>
    /// <returns>The current field value.</returns>
    int GetRowCount();
    /// <summary>Replaces the current row count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowCount(int value);
    /// <summary>Returns the current column count.</summary>
    /// <returns>The current field value.</returns>
    int GetColumnCount();
    /// <summary>Replaces the current column count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumnCount(int value);
    /// <summary>Returns the current cells.</summary>
    /// <returns>The current field value.</returns>
    ChartTableCellLayout[] GetCells();
    /// <summary>Replaces the current cells.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCells(ChartTableCellLayout[] value);
    /// <summary>Returns the current cell count.</summary>
    /// <returns>The current field value.</returns>
    int GetCellCount();
    /// <summary>Replaces the current cell count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCellCount(int value);
}
