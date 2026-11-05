namespace chart;
/// <summary>
/// Represents a grid used by the chart model or rendering pipeline.
/// </summary>
public class ChartGrid : IChartGrid
{
    internal int Rows;
    internal int Columns;
    internal double RowGap;
    internal double ColumnGap;
    /// <summary>
    /// Initializes a grid from the supplied rows and columns.
    /// </summary>
    /// <param name = "rows">The rows.</param>
    /// <param name = "columns">The columns.</param>
    public ChartGrid(int rows, int columns)
    {
        ValidateDimensions(rows, columns);
        Rows = rows;
        Columns = columns;
        RowGap = 0.0;
        ColumnGap = 0.0;
    }

    /// <summary>
    /// Sets the gaps configuration.
    /// </summary>
    /// <param name = "rowGap">The row gap.</param>
    /// <param name = "columnGap">The column gap.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetGaps(double rowGap, double columnGap)
    {
        ChartLinearScale.ValidateFinite(rowGap, "rowGap");
        ChartLinearScale.ValidateFinite(columnGap, "columnGap");
        if (rowGap < 0.0 || columnGap < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("rowGap");
        }

        RowGap = rowGap;
        ColumnGap = columnGap;
    }

    /// <summary>
    /// Applies the cell rect operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "area">The area.</param>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting rect.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartRect CellRect(ChartRect area, int row, int column)
    {
        if (area == null)
        {
            throw new System.ArgumentNullException("area");
        }

        ChartAxis.ValidatePlotArea(area);
        if (row < 0 || row >= Rows)
        {
            throw new System.ArgumentOutOfRangeException("row");
        }

        if (column < 0 || column >= Columns)
        {
            throw new System.ArgumentOutOfRangeException("column");
        }

        double cellWidth = (area.Width - ColumnGap * (Columns - 1)) / Columns;
        double cellHeight = (area.Height - RowGap * (Rows - 1)) / Rows;
        double x = area.X + column * (cellWidth + ColumnGap);
        double y = area.Y + row * (cellHeight + RowGap);
        return new ChartRect(x, y, cellWidth, cellHeight);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartGrid Copy()
    {
        ChartGrid grid = new ChartGrid(Rows, Columns);
        grid.RowGap = RowGap;
        grid.ColumnGap = ColumnGap;
        return grid;
    }

    internal static void ValidateDimensions(int rows, int columns)
    {
        if (rows <= 0)
        {
            throw new System.ArgumentOutOfRangeException("rows");
        }

        if (columns <= 0)
        {
            throw new System.ArgumentOutOfRangeException("columns");
        }
    }

    /// <summary>Returns the current rows.</summary>
    /// <returns>The current field value.</returns>
    public int GetRows()
    {
        return Rows;
    }

    /// <summary>Replaces the current rows.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRows(int value)
    {
        Rows = value;
    }

    /// <summary>Returns the current columns.</summary>
    /// <returns>The current field value.</returns>
    public int GetColumns()
    {
        return Columns;
    }

    /// <summary>Replaces the current columns.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumns(int value)
    {
        Columns = value;
    }

    /// <summary>Returns the current row gap.</summary>
    /// <returns>The current field value.</returns>
    public double GetRowGap()
    {
        return RowGap;
    }

    /// <summary>Replaces the current row gap.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRowGap(double value)
    {
        RowGap = value;
    }

    /// <summary>Returns the current column gap.</summary>
    /// <returns>The current field value.</returns>
    public double GetColumnGap()
    {
        return ColumnGap;
    }

    /// <summary>Replaces the current column gap.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColumnGap(double value)
    {
        ColumnGap = value;
    }
}
