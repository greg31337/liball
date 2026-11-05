namespace chart;
/// <summary>Defines substitutable chart render style factory operations.</summary>
public interface IChartRenderStyleFactory
{
    /// <summary>
    /// Creates the requested fill from the supplied data and configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <returns>The newly created render style.</returns>
    ChartRenderStyle CreateFill(ChartColor color);
    /// <summary>
    /// Creates the requested stroke from the supplied data and configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <returns>The newly created render style.</returns>
    ChartRenderStyle CreateStroke(ChartColor color, double strokeWidth);
    /// <summary>
    /// Creates the requested fill stroke from the supplied data and configuration.
    /// </summary>
    /// <param name = "fillColor">The fill color.</param>
    /// <param name = "strokeColor">The stroke color.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <returns>The newly created render style.</returns>
    ChartRenderStyle CreateFillStroke(ChartColor fillColor, ChartColor strokeColor, double strokeWidth);
}
