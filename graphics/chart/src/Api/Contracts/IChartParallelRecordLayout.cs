namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartParallelRecordLayout"/>.</summary>
public interface IChartParallelRecordLayout
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartParallelRecordLayout Copy();
    /// <summary>Returns the current row index.</summary>
    /// <returns>The current field value.</returns>
    int GetRowIndex();
    /// <summary>Replaces the current row index.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowIndex(int value);
    /// <summary>Returns the current points.</summary>
    /// <returns>The current field value.</returns>
    ChartPoint[] GetPoints();
    /// <summary>Replaces the current points.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPoints(ChartPoint[] value);
}
