namespace chart;
/// <summary>Defines substitutable chart axis factory operations.</summary>
public interface IChartAxisFactory
{
    /// <summary>
    /// Creates the requested bottom from the supplied data and configuration.
    /// </summary>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    /// <returns>The newly created axis.</returns>
    ChartAxis CreateBottom(ChartTick[] ticks, ChartRange domain, ChartRect plotArea);
    /// <summary>
    /// Creates the requested top from the supplied data and configuration.
    /// </summary>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    /// <returns>The newly created axis.</returns>
    ChartAxis CreateTop(ChartTick[] ticks, ChartRange domain, ChartRect plotArea);
    /// <summary>
    /// Creates the requested left from the supplied data and configuration.
    /// </summary>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    /// <returns>The newly created axis.</returns>
    ChartAxis CreateLeft(ChartTick[] ticks, ChartRange domain, ChartRect plotArea);
    /// <summary>
    /// Creates the requested right from the supplied data and configuration.
    /// </summary>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <param name = "plotArea">The rectangle available for plotting data.</param>
    /// <returns>The newly created axis.</returns>
    ChartAxis CreateRight(ChartTick[] ticks, ChartRange domain, ChartRect plotArea);
    /// <summary>
    /// Creates the requested radial from the supplied data and configuration.
    /// </summary>
    /// <param name = "center">The center.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "ticks">The ticks.</param>
    /// <param name = "domain">The input data range mapped by the scale or axis.</param>
    /// <returns>The newly created axis.</returns>
    ChartAxis CreateRadial(ChartPoint center, double radius, ChartTick[] ticks, ChartRange domain);
    /// <summary>
    /// Creates the requested angular from the supplied data and configuration.
    /// </summary>
    /// <param name = "center">The center.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "ticks">The ticks.</param>
    /// <returns>The newly created axis.</returns>
    ChartAxis CreateAngular(ChartPoint center, double radius, ChartTick[] ticks);
    /// <summary>Creates a ternary axis along one edge of a triangle.</summary>
    /// <param name = "startVertex">The edge start vertex.</param>
    /// <param name = "endVertex">The edge end vertex.</param>
    /// <param name = "oppositeVertex">The opposite triangle vertex.</param>
    /// <param name = "ticks">The ticks, normally spanning zero through one.</param>
    /// <returns>The newly created ternary axis.</returns>
    ChartAxis CreateTernary(ChartPoint startVertex, ChartPoint endVertex, ChartPoint oppositeVertex, ChartTick[] ticks);
}
