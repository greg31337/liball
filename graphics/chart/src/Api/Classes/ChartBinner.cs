namespace chart;
/// <summary>Provides the public chart binner operations.</summary>
public class ChartBinner : IChartBinner
{
    /// <summary>Initializes a stateless chart binner facade.</summary>
    public ChartBinner()
    {
    }

    /// <summary>
    /// Groups values into fixed count using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "binCount">The requested number of histogram or color bins, which must be positive.</param>
    /// <returns>The resulting histogram.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartHistogram BinFixedCount(double[] values, int binCount)
    {
        return ChartBinnerOperations.BinFixedCount(values, binCount);
    }

    /// <summary>
    /// Groups values into fixed width using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "binWidth">The bin width.</param>
    /// <returns>The resulting histogram.</returns>
    public ChartHistogram BinFixedWidth(double[] values, double binWidth)
    {
        return ChartBinnerOperations.BinFixedWidth(values, binWidth);
    }

    /// <summary>
    /// Groups values into sturges using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting histogram.</returns>
    public ChartHistogram BinSturges(double[] values)
    {
        return ChartBinnerOperations.BinSturges(values);
    }

    /// <summary>
    /// Groups values into scott using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting histogram.</returns>
    public ChartHistogram BinScott(double[] values)
    {
        return ChartBinnerOperations.BinScott(values);
    }

    /// <summary>
    /// Groups values into freedman diaconis using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting histogram.</returns>
    public ChartHistogram BinFreedmanDiaconis(double[] values)
    {
        return ChartBinnerOperations.BinFreedmanDiaconis(values);
    }

    /// <summary>
    /// Groups values into the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "strategy">The strategy.</param>
    /// <param name = "fixedCount">The requested number of fixed entries.</param>
    /// <param name = "fixedWidth">The fixed width.</param>
    /// <returns>The resulting histogram.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public ChartHistogram Bin(double[] values, ChartHistogramBinStrategy strategy, int fixedCount, double fixedWidth)
    {
        return ChartBinnerOperations.Bin(values, strategy, fixedCount, fixedWidth);
    }

    /// <summary>
    /// Calculates the Sturges histogram bin count from the number of observations.
    /// </summary>
    /// <param name = "valueCount">The requested number of value entries.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public int CalculateSturgesBinCount(int valueCount)
    {
        return ChartBinnerOperations.CalculateSturgesBinCount(valueCount);
    }

    /// <summary>
    /// Calculates Scott's histogram bin width from population standard deviation and sample size.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public double CalculateScottBinWidth(double[] values)
    {
        return ChartBinnerOperations.CalculateScottBinWidth(values);
    }

    /// <summary>
    /// Calculates the Freedman-Diaconis bin width from the interquartile range and sample size.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public double CalculateFreedmanDiaconisBinWidth(double[] values)
    {
        return ChartBinnerOperations.CalculateFreedmanDiaconisBinWidth(values);
    }

    /// <summary>
    /// Applies the percentile operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "probability">The probability.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public double Percentile(double[] values, double probability)
    {
        return ChartBinnerOperations.Percentile(values, probability);
    }
}
