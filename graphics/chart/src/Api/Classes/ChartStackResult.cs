namespace chart;
/// <summary>
/// Represents a stack result used by the chart model or rendering pipeline.
/// </summary>
public class ChartStackResult : IChartStackResult
{
    internal ChartDataMatrix Starts;
    internal ChartDataMatrix Ends;
    internal ChartStackMode Mode;
    internal int Rows;
    internal int Columns;
    /// <summary>
    /// Initializes a stack result from the supplied starts, ends, and mode.
    /// </summary>
    /// <param name = "starts">The starts.</param>
    /// <param name = "ends">The ends.</param>
    /// <param name = "mode">The mode.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartStackResult(ChartDataMatrix starts, ChartDataMatrix ends, ChartStackMode mode)
    {
        if (starts == null)
        {
            throw new System.ArgumentNullException("starts");
        }

        if (ends == null)
        {
            throw new System.ArgumentNullException("ends");
        }

        if (starts.Rows != ends.Rows || starts.Columns != ends.Columns)
        {
            throw new System.ArgumentException("Stack start and end matrices must have matching dimensions.");
        }

        Starts = starts.Copy();
        Ends = ends.Copy();
        Mode = mode;
        Rows = starts.Rows;
        Columns = starts.Columns;
    }

    /// <summary>
    /// Returns the requested start.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    public double GetStart(int row, int column)
    {
        return Starts.GetValue(row, column);
    }

    /// <summary>
    /// Returns the requested end.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    public double GetEnd(int row, int column)
    {
        return Ends.GetValue(row, column);
    }

    /// <summary>Returns the current starts.</summary>
    /// <returns>The current field value.</returns>
    public ChartDataMatrix GetStarts()
    {
        return Starts;
    }

    /// <summary>Replaces the current starts.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStarts(ChartDataMatrix value)
    {
        Starts = value;
    }

    /// <summary>Returns the current ends.</summary>
    /// <returns>The current field value.</returns>
    public ChartDataMatrix GetEnds()
    {
        return Ends;
    }

    /// <summary>Replaces the current ends.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetEnds(ChartDataMatrix value)
    {
        Ends = value;
    }

    /// <summary>Returns the current mode.</summary>
    /// <returns>The current field value.</returns>
    public ChartStackMode GetMode()
    {
        return Mode;
    }

    /// <summary>Replaces the current mode.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMode(ChartStackMode value)
    {
        Mode = value;
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
}
