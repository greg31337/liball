namespace chart;
/// <summary>Creates and converts chart layout request values.</summary>
public class ChartLayoutRequestFactory : IChartLayoutRequestFactory
{
    /// <summary>Initializes a stateless chart layout request factory facade.</summary>
    public ChartLayoutRequestFactory()
    {
    }

    /// <summary>
    /// Creates the requested legend from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <returns>The newly created layout request.</returns>
    public ChartLayoutRequest CreateLegend(string id, ChartSide side, double width, double height, double spacing)
    {
        return ChartLayoutRequest.CreateLegend(id, side, width, height, spacing);
    }

    /// <summary>
    /// Creates the requested colorbar from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "side">The side.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "spacing">The spacing.</param>
    /// <returns>The newly created layout request.</returns>
    public ChartLayoutRequest CreateColorbar(string id, ChartSide side, double width, double height, double spacing)
    {
        return ChartLayoutRequest.CreateColorbar(id, side, width, height, spacing);
    }

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
    public ChartLayoutRequest CreateAnnotation(string id, ChartSide side, double width, double height, double spacing, bool overlay)
    {
        return ChartLayoutRequest.CreateAnnotation(id, side, width, height, spacing, overlay);
    }
}
