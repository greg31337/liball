namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartStackResult"/>.</summary>
public interface IChartStackResult
{
    /// <summary>
    /// Returns the requested start.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    double GetStart(int row, int column);
    /// <summary>
    /// Returns the requested end.
    /// </summary>
    /// <param name = "row">The row.</param>
    /// <param name = "column">The column.</param>
    /// <returns>The resulting double.</returns>
    double GetEnd(int row, int column);
    /// <summary>Returns the current starts.</summary>
    /// <returns>The current field value.</returns>
    ChartDataMatrix GetStarts();
    /// <summary>Replaces the current starts.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStarts(ChartDataMatrix value);
    /// <summary>Returns the current ends.</summary>
    /// <returns>The current field value.</returns>
    ChartDataMatrix GetEnds();
    /// <summary>Replaces the current ends.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetEnds(ChartDataMatrix value);
    /// <summary>Returns the current mode.</summary>
    /// <returns>The current field value.</returns>
    ChartStackMode GetMode();
    /// <summary>Replaces the current mode.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMode(ChartStackMode value);
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
}
