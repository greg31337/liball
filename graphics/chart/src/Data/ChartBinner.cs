namespace chart;

/// <summary>
/// Provides operations for binner.
/// </summary>
internal static class ChartBinnerOperations
{
    /// <summary>
    /// Groups values into fixed count using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <param name="binCount">The requested number of histogram or color bins, which must be positive.</param>
    /// <returns>The resulting histogram.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public static ChartHistogram BinFixedCount(double[] values, int binCount)
    {
        if (binCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("binCount");
        }

        ChartStatisticsOperations.ValidateFiniteValues(values);

        double minimum = ChartStatisticsOperations.Min(values);
        double maximum = ChartStatisticsOperations.Max(values);
        if (minimum == maximum)
        {
            minimum = minimum - 0.5;
            maximum = maximum + 0.5;
        }

        double binWidth = (maximum - minimum) / binCount;
        return CreateHistogram(values, binCount, minimum, maximum, binWidth, ChartHistogramBinStrategy.FixedCount);
    }

    /// <summary>
    /// Groups values into fixed width using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <param name="binWidth">The bin width.</param>
    /// <returns>The resulting histogram.</returns>
    public static ChartHistogram BinFixedWidth(double[] values, double binWidth)
    {
        ValidateBinWidth(binWidth);
        ChartStatisticsOperations.ValidateFiniteValues(values);

        double minimum = ChartStatisticsOperations.Min(values);
        double maximum = ChartStatisticsOperations.Max(values);
        int binCount = 1;
        if (maximum > minimum)
        {
            binCount = CalculateBinCount(maximum - minimum, binWidth);
        }

        maximum = minimum + binWidth * binCount;
        return CreateHistogram(values, binCount, minimum, maximum, binWidth, ChartHistogramBinStrategy.FixedWidth);
    }

    /// <summary>
    /// Groups values into sturges using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting histogram.</returns>
    public static ChartHistogram BinSturges(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        return CreateHistogramWithCount(values, CalculateSturgesBinCount(values.Length), ChartHistogramBinStrategy.Sturges);
    }

    /// <summary>
    /// Groups values into scott using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting histogram.</returns>
    public static ChartHistogram BinScott(double[] values)
    {
        double width = CalculateScottBinWidth(values);
        return CreateHistogramWithWidth(values, width, ChartHistogramBinStrategy.Scott);
    }

    /// <summary>
    /// Groups values into freedman diaconis using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting histogram.</returns>
    public static ChartHistogram BinFreedmanDiaconis(double[] values)
    {
        double width = CalculateFreedmanDiaconisBinWidth(values);
        return CreateHistogramWithWidth(values, width, ChartHistogramBinStrategy.FreedmanDiaconis);
    }

    /// <summary>
    /// Groups values into the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <param name="strategy">The strategy.</param>
    /// <param name="fixedCount">The requested number of fixed entries.</param>
    /// <param name="fixedWidth">The fixed width.</param>
    /// <returns>The resulting histogram.</returns>
    /// <exception cref="System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartHistogram Bin(double[] values, ChartHistogramBinStrategy strategy, int fixedCount, double fixedWidth)
    {
        if (strategy == ChartHistogramBinStrategy.FixedCount)
        {
            return BinFixedCount(values, fixedCount);
        }

        if (strategy == ChartHistogramBinStrategy.FixedWidth)
        {
            return BinFixedWidth(values, fixedWidth);
        }

        if (strategy == ChartHistogramBinStrategy.Sturges)
        {
            return BinSturges(values);
        }

        if (strategy == ChartHistogramBinStrategy.Scott)
        {
            return BinScott(values);
        }

        if (strategy == ChartHistogramBinStrategy.FreedmanDiaconis)
        {
            return BinFreedmanDiaconis(values);
        }

        throw new System.ArgumentException("Unsupported histogram bin strategy.");
    }

    /// <summary>
    /// Calculates the Sturges histogram bin count from the number of observations.
    /// </summary>
    /// <param name="valueCount">The requested number of value entries.</param>
    /// <returns>The resulting int.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public static int CalculateSturgesBinCount(int valueCount)
    {
        if (valueCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("valueCount");
        }

        return (int)System.Math.Ceiling(System.Math.Log(valueCount, 2.0) + 1.0);
    }

    /// <summary>
    /// Calculates Scott's histogram bin width from population standard deviation and sample size.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public static double CalculateScottBinWidth(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);

        double range = ChartStatisticsOperations.Max(values) - ChartStatisticsOperations.Min(values);
        double width = 3.5 * ChartStatisticsOperations.StandardDeviation(values) / System.Math.Cbrt(values.Length);
        return NormalizeRuleWidth(width, range);
    }

    /// <summary>
    /// Calculates the Freedman-Diaconis bin width from the interquartile range and sample size.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <returns>The resulting double.</returns>
    public static double CalculateFreedmanDiaconisBinWidth(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);

        double range = ChartStatisticsOperations.Max(values) - ChartStatisticsOperations.Min(values);
        double firstQuartile = Percentile(values, 0.25);
        double thirdQuartile = Percentile(values, 0.75);
        double width = 2.0 * (thirdQuartile - firstQuartile) / System.Math.Cbrt(values.Length);
        return NormalizeRuleWidth(width, range);
    }

    /// <summary>
    /// Applies the percentile operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name="values">The input values to process.</param>
    /// <param name="probability">The probability.</param>
    /// <returns>The resulting double.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public static double Percentile(double[] values, double probability)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        if (probability < 0.0 || probability > 1.0 || double.IsNaN(probability) || double.IsInfinity(probability))
        {
            throw new System.ArgumentOutOfRangeException("probability");
        }

        double[] sorted = ChartDataMatrix.CopyDoubleArray(values);
        System.Array.Sort(sorted);
        double position = (sorted.Length - 1) * probability;
        int lower = (int)System.Math.Floor(position);
        int upper = (int)System.Math.Ceiling(position);
        if (lower == upper)
        {
            return sorted[lower];
        }

        double fraction = position - lower;
        return sorted[lower] * (1.0 - fraction) + sorted[upper] * fraction;
    }

    internal static ChartHistogram CreateHistogramWithCount(double[] values, int binCount, ChartHistogramBinStrategy strategy)
    {
        if (binCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("binCount");
        }

        ChartStatisticsOperations.ValidateFiniteValues(values);

        double minimum = ChartStatisticsOperations.Min(values);
        double maximum = ChartStatisticsOperations.Max(values);
        if (minimum == maximum)
        {
            minimum = minimum - 0.5;
            maximum = maximum + 0.5;
        }

        double binWidth = (maximum - minimum) / binCount;
        return CreateHistogram(values, binCount, minimum, maximum, binWidth, strategy);
    }

    internal static ChartHistogram CreateHistogramWithWidth(double[] values, double binWidth, ChartHistogramBinStrategy strategy)
    {
        ValidateBinWidth(binWidth);
        ChartStatisticsOperations.ValidateFiniteValues(values);

        double minimum = ChartStatisticsOperations.Min(values);
        double maximum = ChartStatisticsOperations.Max(values);
        int binCount = 1;
        if (maximum > minimum)
        {
            binCount = CalculateBinCount(maximum - minimum, binWidth);
        }

        maximum = minimum + binWidth * binCount;
        return CreateHistogram(values, binCount, minimum, maximum, binWidth, strategy);
    }

    internal static ChartHistogram CreateHistogram(double[] values, int binCount, double minimum, double maximum, double binWidth, ChartHistogramBinStrategy strategy)
    {
        ChartHistogramBin[] bins = new ChartHistogramBin[binCount];
        int index = 0;
        while (index < binCount)
        {
            double binMinimum = minimum + binWidth * index;
            double binMaximum = binMinimum + binWidth;
            if (index == binCount - 1)
            {
                binMaximum = maximum;
            }

            bins[index] = new ChartHistogramBin(binMinimum, binMaximum);
            index = index + 1;
        }

        index = 0;
        while (index < values.Length)
        {
            int binIndex = FindBinIndex(values[index], minimum, maximum, binWidth, binCount);
            bins[binIndex].Count = bins[binIndex].Count + 1;
            index = index + 1;
        }

        return new ChartHistogram(bins, minimum, maximum, binWidth, values.Length, strategy);
    }

    internal static int FindBinIndex(double value, double minimum, double maximum, double binWidth, int binCount)
    {
        if (value <= minimum)
        {
            return 0;
        }

        if (value >= maximum)
        {
            return binCount - 1;
        }

        double position = (value - minimum) / binWidth;
        if (position <= 0.0)
        {
            return 0;
        }

        if (double.IsInfinity(position) || position >= binCount)
        {
            return binCount - 1;
        }

        int index = (int)position;
        if (index < 0)
        {
            return 0;
        }

        if (index >= binCount)
        {
            return binCount - 1;
        }

        return index;
    }

    internal static double NormalizeRuleWidth(double width, double range)
    {
        if (width > 0.0 && !double.IsNaN(width) && !double.IsInfinity(width))
        {
            return width;
        }

        if (range > 0.0)
        {
            return range;
        }

        return 1.0;
    }

    internal static void ValidateBinWidth(double binWidth)
    {
        if (binWidth <= 0.0 || double.IsNaN(binWidth) || double.IsInfinity(binWidth))
        {
            throw new System.ArgumentOutOfRangeException("binWidth");
        }
    }

    internal static int CalculateBinCount(double range, double binWidth)
    {
        double count = System.Math.Ceiling(range / binWidth);
        if (double.IsNaN(count) || double.IsInfinity(count) || count <= 0.0 || count > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("binWidth");
        }

        return (int)count;
    }
}
