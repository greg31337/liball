namespace chart;
/// <summary>
/// Represents a parallel record layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartParallelRecordLayout : IChartParallelRecordLayout
{
    internal int RowIndex;
    internal ChartPoint[] Points;
    /// <summary>
    /// Initializes a parallel record layout from the supplied row index and points.
    /// </summary>
    /// <param name = "rowIndex">The zero-based row index.</param>
    /// <param name = "points">The points.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartParallelRecordLayout(int rowIndex, ChartPoint[] points)
    {
        if (rowIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("rowIndex");
        }

        ChartRenderCommand.ValidatePoints(points, 2);
        RowIndex = rowIndex;
        Points = ChartRenderCommand.CopyPoints(points);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartParallelRecordLayout Copy()
    {
        return new ChartParallelRecordLayout(RowIndex, Points);
    }

    /// <summary>Returns the current row index.</summary>
    /// <returns>The current field value.</returns>
    public int GetRowIndex()
    {
        return RowIndex;
    }

    /// <summary>Replaces the current row index.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetRowIndex(int value)
    {
        RowIndex = value;
    }

    /// <summary>Returns the current points.</summary>
    /// <returns>The current field value.</returns>
    public ChartPoint[] GetPoints()
    {
        return Points;
    }

    /// <summary>Replaces the current points.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPoints(ChartPoint[] value)
    {
        Points = value;
    }
}
