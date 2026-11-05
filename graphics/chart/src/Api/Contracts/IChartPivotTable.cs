namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartPivotTable"/>.</summary>
public interface IChartPivotTable
{
    /// <summary>
    /// Returns the requested value.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    double GetValue(int row, int column);
    /// <summary>
    /// Creates an independent copy of the row keys.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    string? [] CopyRowKeys();
    /// <summary>
    /// Creates an independent copy of the column keys.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    string? [] CopyColumnKeys();
    /// <summary>Returns the current row keys.</summary>
    /// <returns>The current field value.</returns>
    string? [] GetRowKeys();
    /// <summary>Replaces the current row keys.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowKeys(string? [] value);
    /// <summary>Returns the current column keys.</summary>
    /// <returns>The current field value.</returns>
    string? [] GetColumnKeys();
    /// <summary>Replaces the current column keys.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumnKeys(string? [] value);
    /// <summary>Returns the current values.</summary>
    /// <returns>The current field value.</returns>
    ChartDataMatrix GetValues();
    /// <summary>Replaces the current values.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetValues(ChartDataMatrix value);
}
