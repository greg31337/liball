namespace chart;
/// <summary>
/// Represents a table layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartTableLayout : IChartTableLayout
{
    internal int RowCount;
    internal int ColumnCount;
    internal ChartTableCellLayout[] Cells;
    internal int CellCount;
    /// <summary>
    /// Initializes a table layout from the supplied row count and column count.
    /// </summary>
    /// <param name = "rowCount">The number of logical rows represented by the input.</param>
    /// <param name = "columnCount">The number of logical columns represented by the input.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartTableLayout(int rowCount, int columnCount)
    {
        if (rowCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("rowCount");
        }

        if (columnCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("columnCount");
        }

        long cellCount = ((long)rowCount + 1) * columnCount;
        if (cellCount > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("columnCount");
        }

        RowCount = rowCount;
        ColumnCount = columnCount;
        Cells = new ChartTableCellLayout[(int)cellCount];
        CellCount = 0;
    }

    /// <summary>
    /// Adds the supplied cell to this object.
    /// </summary>
    /// <param name = "cell">The cell.</param>
    /// <returns>The resulting table cell layout.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartTableCellLayout AddCell(ChartTableCellLayout cell)
    {
        if (cell == null)
        {
            throw new System.ArgumentNullException("cell");
        }

        Cells[CellCount] = cell.Copy();
        CellCount = CellCount + 1;
        return Cells[CellCount - 1];
    }

    /// <summary>
    /// Returns the requested cell.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting table cell layout.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartTableCellLayout CellAt(int index)
    {
        if (index < 0 || index >= CellCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Cells[index];
    }

    /// <summary>Returns the current row count.</summary>
    /// <returns>The current field value.</returns>
    public int GetRowCount()
    {
        return RowCount;
    }

    /// <summary>Replaces the current row count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRowCount(int value)
    {
        RowCount = value;
    }

    /// <summary>Returns the current column count.</summary>
    /// <returns>The current field value.</returns>
    public int GetColumnCount()
    {
        return ColumnCount;
    }

    /// <summary>Replaces the current column count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumnCount(int value)
    {
        ColumnCount = value;
    }

    /// <summary>Returns the current cells.</summary>
    /// <returns>The current field value.</returns>
    public ChartTableCellLayout[] GetCells()
    {
        return Cells;
    }

    /// <summary>Replaces the current cells.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCells(ChartTableCellLayout[] value)
    {
        Cells = value;
    }

    /// <summary>Returns the current cell count.</summary>
    /// <returns>The current field value.</returns>
    public int GetCellCount()
    {
        return CellCount;
    }

    /// <summary>Replaces the current cell count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetCellCount(int value)
    {
        CellCount = value;
    }
}
