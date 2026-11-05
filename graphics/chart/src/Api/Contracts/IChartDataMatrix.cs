namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartDataMatrix"/>.</summary>
public interface IChartDataMatrix
{
    /// <summary>
    /// Returns the requested value.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    double GetValue(int row, int column);
    /// <summary>
    /// Sets the value configuration.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <param name = "value">The value.</param>
    void SetValue(int row, int column, double value);
    /// <summary>
    /// Creates an independent copy of the values.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    double[] CopyValues();
    /// <summary>Returns a mutable zero-copy row-major view of all matrix values.</summary>
    /// <returns>A span backed by this matrix's values.</returns>
    System.Span<double> GetValueSpan();
    /// <summary>Returns a mutable zero-copy view of one matrix row.</summary>
    /// <param name = "row">The zero-based row index.</param>
    /// <returns>A span backed by the requested matrix row.</returns>
    System.Span<double> GetRowSpan(int row);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartDataMatrix Copy();
    /// <summary>Returns the current rows.</summary>
    /// <returns>The current field value.</returns>
    int GetRows();
    /// <summary>Replaces the current rows.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRows(int value);
    /// <summary>Returns the current columns.</summary>
    /// <returns>The current field value.</returns>
    int GetColumns();
    /// <summary>Replaces the current columns.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumns(int value);
    /// <summary>Returns the current values.</summary>
    /// <returns>The current field value.</returns>
    double[] GetValues();
    /// <summary>Replaces the current values.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetValues(double[] value);
}
