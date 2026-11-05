namespace chart;
/// <summary>Creates and converts chart axis values.</summary>
public class ChartAxisFactory : IChartAxisFactory
{
    /// <summary>Initializes a stateless chart axis factory facade.</summary>
    public ChartAxisFactory()
    {
    }

    /// <summary>
    /// Creates the requested bottom from the supplied data and configuration.
    /// </summary>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    /// <returns>The newly created axis.</returns>
    public ChartAxis CreateBottom(ChartTick[] ticks, ChartRange domain, ChartRect plotArea)
    {
        return ChartAxis.CreateBottom(ticks, domain, plotArea);
    }

    /// <summary>
    /// Creates the requested top from the supplied data and configuration.
    /// </summary>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    /// <returns>The newly created axis.</returns>
    public ChartAxis CreateTop(ChartTick[] ticks, ChartRange domain, ChartRect plotArea)
    {
        return ChartAxis.CreateTop(ticks, domain, plotArea);
    }

    /// <summary>
    /// Creates the requested left from the supplied data and configuration.
    /// </summary>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    /// <returns>The newly created axis.</returns>
    public ChartAxis CreateLeft(ChartTick[] ticks, ChartRange domain, ChartRect plotArea)
    {
        return ChartAxis.CreateLeft(ticks, domain, plotArea);
    }

    /// <summary>
    /// Creates the requested right from the supplied data and configuration.
    /// </summary>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    /// <returns>The newly created axis.</returns>
    public ChartAxis CreateRight(ChartTick[] ticks, ChartRange domain, ChartRect plotArea)
    {
        return ChartAxis.CreateRight(ticks, domain, plotArea);
    }

    /// <summary>
    /// Creates the requested radial from the supplied data and configuration.
    /// </summary>
    /// <param name = "center">The center.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <returns>The newly created axis.</returns>
    public ChartAxis CreateRadial(ChartPoint center, double radius, ChartTick[] ticks, ChartRange domain)
    {
        return ChartAxis.CreateRadial(center, radius, ticks, domain);
    }

    /// <summary>
    /// Creates the requested angular from the supplied data and configuration.
    /// </summary>
    /// <param name = "center">The center.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "ticks">The ticks.</param>
    /// <returns>The newly created axis.</returns>
    public ChartAxis CreateAngular(ChartPoint center, double radius, ChartTick[] ticks)
    {
        return ChartAxis.CreateAngular(center, radius, ticks);
    }

    /// <summary>Creates a ternary axis along one edge of a triangle.</summary>
    /// <param name = "startVertex">The edge start vertex.</param>
    /// <param name = "endVertex">The edge end vertex.</param>
    /// <param name = "oppositeVertex">The opposite triangle vertex.</param>
    /// <param name = "ticks">The ticks, normally spanning zero through one.</param>
    /// <returns>The newly created ternary axis.</returns>
    public ChartAxis CreateTernary(ChartPoint startVertex, ChartPoint endVertex, ChartPoint oppositeVertex, ChartTick[] ticks)
    {
        return ChartAxis.CreateTernary(startVertex, endVertex, oppositeVertex, ticks);
    }
}
