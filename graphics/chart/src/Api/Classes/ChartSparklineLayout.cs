namespace chart;
/// <summary>
/// Represents a sparkline layout used by the chart model or rendering pipeline.
/// </summary>
public class ChartSparklineLayout : IChartSparklineLayout
{
    internal ChartPoint[] Points;
    internal double Minimum;
    internal double Maximum;
    /// <summary>
    /// Initializes a sparkline layout from the supplied points, minimum, and maximum.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    public ChartSparklineLayout(ChartPoint[] points, double minimum, double maximum)
    {
        ChartRenderCommand.ValidatePoints(points, 2);
        ChartLinearScale.ValidateFinite(minimum, "minimum");
        ChartLinearScale.ValidateFinite(maximum, "maximum");
        Points = ChartRenderCommand.CopyPoints(points);
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartSparklineLayout Copy()
    {
        return new ChartSparklineLayout(Points, Minimum, Maximum);
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

    /// <summary>Returns the current minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetMinimum()
    {
        return Minimum;
    }

    /// <summary>Replaces the current minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMinimum(double value)
    {
        Minimum = value;
    }

    /// <summary>Returns the current maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetMaximum()
    {
        return Maximum;
    }

    /// <summary>Replaces the current maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMaximum(double value)
    {
        Maximum = value;
    }
}
