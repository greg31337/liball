namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartSparklineLayout"/>.</summary>
public interface IChartSparklineLayout
{
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartSparklineLayout Copy();
    /// <summary>Returns the current points.</summary>
    /// <returns>The current field value.</returns>
    ChartPoint[] GetPoints();
    /// <summary>Replaces the current points.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPoints(ChartPoint[] value);
    /// <summary>Returns the current minimum.</summary>
    /// <returns>The current field value.</returns>
    double GetMinimum();
    /// <summary>Replaces the current minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMinimum(double value);
    /// <summary>Returns the current maximum.</summary>
    /// <returns>The current field value.</returns>
    double GetMaximum();
    /// <summary>Replaces the current maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMaximum(double value);
}
