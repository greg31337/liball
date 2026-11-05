namespace chart;
/// <summary>Defines substitutable chart visual map factory operations.</summary>
public interface IChartVisualMapFactory
{
    /// <summary>
    /// Creates the requested continuous from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "colorMap">The continuous color map used to translate numeric values.</param>
    /// <returns>The newly created visual map.</returns>
    ChartVisualMap CreateContinuous(ChartEncodingChannel channel, double minimum, double maximum, ChartColorMap colorMap);
    /// <summary>
    /// Creates the requested binned from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "binCount">The requested number of histogram or color bins, which must be positive.</param>
    /// <param name = "colorMap">The continuous color map used to translate numeric values.</param>
    /// <returns>The newly created visual map.</returns>
    ChartVisualMap CreateBinned(ChartEncodingChannel channel, double minimum, double maximum, int binCount, ChartColorMap colorMap);
    /// <summary>
    /// Creates the requested threshold from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "thresholds">The thresholds.</param>
    /// <param name = "colors">The colors.</param>
    /// <returns>The newly created visual map.</returns>
    ChartVisualMap CreateThreshold(ChartEncodingChannel channel, double[] thresholds, ChartColor[] colors);
    /// <summary>
    /// Creates the requested categorical from the supplied data and configuration.
    /// </summary>
    /// <param name = "channel">The channel.</param>
    /// <param name = "categories">The categories.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created visual map.</returns>
    ChartVisualMap CreateCategorical(ChartEncodingChannel channel, string[] categories, ChartPalette palette);
}
