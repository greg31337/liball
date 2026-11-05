namespace chart;
/// <summary>Provides the public chart layout engine operations.</summary>
public class ChartLayoutEngine : IChartLayoutEngine
{
    /// <summary>Initializes a stateless chart layout engine facade.</summary>
    public ChartLayoutEngine()
    {
    }

    /// <summary>
    /// Computes the layout for the configured value within the supplied bounds.
    /// </summary>
    /// <param name = "document">The chart document whose model, layout, or output is processed.</param>
    /// <returns>The resulting layout result.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartLayoutResult Layout(ChartDocument document)
    {
        return ChartLayoutEngineOperations.Layout(document);
    }
}
