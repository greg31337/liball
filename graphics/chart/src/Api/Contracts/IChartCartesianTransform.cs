namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartCartesianTransform"/>.</summary>
public interface IChartCartesianTransform
{
    /// <summary>
    /// Maps x through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    double MapX(double value);
    /// <summary>
    /// Maps y through the configured input and output domains.
    /// </summary>
    /// <param name = "value">The value.</param>
    /// <returns>The resulting double.</returns>
    double MapY(double value);
    /// <summary>
    /// Maps point through the configured input and output domains.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting point.</returns>
    ChartPoint MapPoint(double x, double y);
    /// <summary>
    /// Maps points through the configured input and output domains.
    /// </summary>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <returns>The resulting point array.</returns>
    ChartPoint[] MapPoints(double[] xValues, double[] yValues);
    /// <summary>
    /// Applies the invert x operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <returns>The resulting double.</returns>
    double InvertX(double x);
    /// <summary>
    /// Applies the invert y operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "y">The y.</param>
    /// <returns>The resulting double.</returns>
    double InvertY(double y);
    /// <summary>
    /// Determines whether this value contains data point.
    /// </summary>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool ContainsDataPoint(double x, double y);
    /// <summary>
    /// Determines whether this value contains plot point.
    /// </summary>
    /// <param name = "point">The point.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    bool ContainsPlotPoint(ChartPoint point);
    /// <summary>
    /// Applies the clip plot polygon operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "points">The points.</param>
    /// <returns>The resulting point array.</returns>
    ChartPoint[] ClipPlotPolygon(ChartPoint[] points);
    /// <summary>Constrains the plot area to a fixed ratio of horizontal to vertical data scale.</summary>
    /// <param name = "ratio">The positive horizontal-to-vertical data scale ratio.</param>
    void SetFixedAspectRatio(double ratio);
    /// <summary>Restores the unconstrained plot area.</summary>
    void ClearFixedAspectRatio();
    /// <summary>Returns the current plot area.</summary>
    /// <returns>The current field value.</returns>
    ChartRect GetPlotArea();
    /// <summary>Replaces the current plot area.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPlotArea(ChartRect value);
    /// <summary>Returns the current x domain.</summary>
    /// <returns>The current field value.</returns>
    ChartRange GetXDomain();
    /// <summary>Replaces the current x domain.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetXDomain(ChartRange value);
    /// <summary>Returns the current y domain.</summary>
    /// <returns>The current field value.</returns>
    ChartRange GetYDomain();
    /// <summary>Replaces the current y domain.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetYDomain(ChartRange value);
}
