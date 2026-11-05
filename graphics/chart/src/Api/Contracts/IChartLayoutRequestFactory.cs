namespace chart;
/// <summary>Defines substitutable chart layout request factory operations.</summary>
public interface IChartLayoutRequestFactory
{
    /// <summary>
    /// Creates the requested legend from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <returns>The newly created layout request.</returns>
    ChartLayoutRequest CreateLegend(string id, ChartSide side, double width, double height, double spacing);
    /// <summary>
    /// Creates the requested colorbar from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <returns>The newly created layout request.</returns>
    ChartLayoutRequest CreateColorbar(string id, ChartSide side, double width, double height, double spacing);
    /// <summary>
    /// Creates the requested annotation from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <param name = "overlay">The overlay.</param>
    /// <returns>The newly created layout request.</returns>
    ChartLayoutRequest CreateAnnotation(string id, ChartSide side, double width, double height, double spacing, bool overlay);
}
