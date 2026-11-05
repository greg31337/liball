namespace chart;
/// <summary>
/// Represents a violin plan used by the chart model or rendering pipeline.
/// </summary>
public class ChartViolinPlan
{
    /// <summary>
    /// The center used by this violin plan.
    /// </summary>
    public double Center;
    /// <summary>
    /// The maximum width used by this violin plan.
    /// </summary>
    public double MaximumWidth;
    /// <summary>
    /// The density points used by this violin plan.
    /// </summary>
    public ChartPoint[] DensityPoints;
    /// <summary>
    /// The polygon points used by this violin plan.
    /// </summary>
    public ChartPoint[] PolygonPoints;
    /// <summary>
    /// Initializes a violin plan from the supplied center, maximum width, density points, and polygon points.
    /// </summary>
    /// <param name = "center">The center.</param>
    /// <param name = "maximumWidth">The maximum width.</param>
    /// <param name = "densityPoints">The density points.</param>
    /// <param name = "polygonPoints">The polygon points.</param>
    public ChartViolinPlan(double center, double maximumWidth, ChartPoint[] densityPoints, ChartPoint[] polygonPoints)
    {
        ChartLinearScale.ValidateFinite(center, "center");
        ChartRenderStyle.ValidatePositive(maximumWidth, "maximumWidth");
        ChartRenderCommand.ValidatePoints(densityPoints, 2);
        ChartRenderCommand.ValidatePoints(polygonPoints, 3);
        Center = center;
        MaximumWidth = maximumWidth;
        DensityPoints = ChartRenderCommand.CopyPoints(densityPoints);
        PolygonPoints = ChartRenderCommand.CopyPoints(polygonPoints);
    }
}
