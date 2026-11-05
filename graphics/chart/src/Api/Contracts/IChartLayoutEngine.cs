namespace chart;
/// <summary>Defines substitutable chart layout engine operations.</summary>
public interface IChartLayoutEngine
{
    /// <summary>
    /// Computes the layout for the configured value within the supplied bounds.
    /// </summary>
    /// <param name = "document">The chart document whose model, layout, or output is processed.</param>
    /// <returns>The resulting layout result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartLayoutResult Layout(ChartDocument document);
}
