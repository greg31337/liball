namespace chart;
/// <summary>Creates and converts chart render style values.</summary>
public class ChartRenderStyleFactory : IChartRenderStyleFactory
{
    /// <summary>Initializes a stateless chart render style factory facade.</summary>
    public ChartRenderStyleFactory()
    {
    }

    /// <summary>
    /// Creates the requested fill from the supplied data and configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <returns>The newly created render style.</returns>
    public ChartRenderStyle CreateFill(ChartColor color)
    {
        return ChartRenderStyle.CreateFill(color);
    }

    /// <summary>
    /// Creates the requested stroke from the supplied data and configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <returns>The newly created render style.</returns>
    public ChartRenderStyle CreateStroke(ChartColor color, double strokeWidth)
    {
        return ChartRenderStyle.CreateStroke(color, strokeWidth);
    }

    /// <summary>
    /// Creates the requested fill stroke from the supplied data and configuration.
    /// </summary>
    /// <param name = "fillColor">The fill color.</param>
    /// <param name = "strokeColor">The stroke color.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <returns>The newly created render style.</returns>
    public ChartRenderStyle CreateFillStroke(ChartColor fillColor, ChartColor strokeColor, double strokeWidth)
    {
        return ChartRenderStyle.CreateFillStroke(fillColor, strokeColor, strokeWidth);
    }
}
