namespace chart;
/// <summary>Creates and converts chart visual map values.</summary>
public class ChartVisualMapFactory : IChartVisualMapFactory
{
    /// <summary>Initializes a stateless chart visual map factory facade.</summary>
    public ChartVisualMapFactory()
    {
    }

    /// <summary>
    /// Creates the requested continuous from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "colorMap">The continuous color map used to translate numeric values.</param>
    /// <returns>The newly created visual map.</returns>
    public ChartVisualMap CreateContinuous(ChartEncodingChannel channel, double minimum, double maximum, ChartColorMap colorMap)
    {
        return ChartVisualMap.CreateContinuous(channel, minimum, maximum, colorMap);
    }

    /// <summary>
    /// Creates the requested binned from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "binCount">The requested number of histogram or color bins, which must be positive.</param>
    /// <param name = "colorMap">The continuous color map used to translate numeric values.</param>
    /// <returns>The newly created visual map.</returns>
    public ChartVisualMap CreateBinned(ChartEncodingChannel channel, double minimum, double maximum, int binCount, ChartColorMap colorMap)
    {
        return ChartVisualMap.CreateBinned(channel, minimum, maximum, binCount, colorMap);
    }

    /// <summary>
    /// Creates the requested threshold from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "thresholds">The thresholds.</param>
    /// <param name = "colors">The colors.</param>
    /// <returns>The newly created visual map.</returns>
    public ChartVisualMap CreateThreshold(ChartEncodingChannel channel, double[] thresholds, ChartColor[] colors)
    {
        return ChartVisualMap.CreateThreshold(channel, thresholds, colors);
    }

    /// <summary>
    /// Creates the requested categorical from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "categories">The categories.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created visual map.</returns>
    public ChartVisualMap CreateCategorical(ChartEncodingChannel channel, string[] categories, ChartPalette palette)
    {
        return ChartVisualMap.CreateCategorical(channel, categories, palette);
    }
}
