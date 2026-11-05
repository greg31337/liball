namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartGrid"/>.</summary>
public interface IChartGrid
{
    /// <summary>
    /// Sets the gaps configuration.
    /// </summary>
    /// <param name = "rowGap">The row gap.</param>
    /// <param name = "columnGap">The column gap.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetGaps(double rowGap, double columnGap);
    /// <summary>
    /// Applies the cell rect operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "area">The area.</param>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting rect.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartRect CellRect(ChartRect area, int row, int column);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartGrid Copy();
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
    /// <summary>Returns the current row gap.</summary>
    /// <returns>The current field value.</returns>
    double GetRowGap();
    /// <summary>Replaces the current row gap.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetRowGap(double value);
    /// <summary>Returns the current column gap.</summary>
    /// <returns>The current field value.</returns>
    double GetColumnGap();
    /// <summary>Replaces the current column gap.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColumnGap(double value);
}
