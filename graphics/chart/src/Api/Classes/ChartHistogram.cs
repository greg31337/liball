namespace chart;
/// <summary>
/// Represents a histogram used by the chart model or rendering pipeline.
/// </summary>
public class ChartHistogram : IChartHistogram
{
    internal ChartHistogramBin[] Bins;
    internal int BinCount;
    internal double Minimum;
    internal double Maximum;
    internal double BinWidth;
    internal int ValueCount;
    internal ChartHistogramBinStrategy Strategy;
    /// <summary>
    /// Initializes a histogram from the supplied bins, minimum, maximum, bin width, value count, and strategy.
    /// </summary>
    /// <param name = "bins">The bins.</param>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    /// <param name = "binWidth">The bin width.</param>
    /// <param name = "valueCount">The requested number of value entries.</param>
    /// <param name = "strategy">The strategy.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartHistogram(ChartHistogramBin[] bins, double minimum, double maximum, double binWidth, int valueCount, ChartHistogramBinStrategy strategy)
    {
        if (bins == null)
        {
            throw new System.ArgumentNullException("bins");
        }

        if (bins.Length == 0)
        {
            throw new System.ArgumentException("Histogram must contain at least one bin.");
        }

        Bins = CopyBins(bins);
        BinCount = bins.Length;
        Minimum = minimum;
        Maximum = maximum;
        BinWidth = binWidth;
        ValueCount = valueCount;
        Strategy = strategy;
    }

    /// <summary>
    /// Returns the requested bin.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting histogram bin.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartHistogramBin GetBin(int index)
    {
        if (index < 0 || index >= BinCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Bins[index];
    }

    /// <summary>
    /// Converts this value to tal count.
    /// </summary>
    /// <returns>The converted int.</returns>
    public int TotalCount()
    {
        int total = 0;
        int index = 0;
        while (index < BinCount)
        {
            total = total + Bins[index].Count;
            index = index + 1;
        }

        return total;
    }

    /// <summary>
    /// Creates an independent copy of the bins.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartHistogramBin[] CopyBins()
    {
        return CopyBins(Bins);
    }

    internal static ChartHistogramBin[] CopyBins(ChartHistogramBin[] bins)
    {
        ChartHistogramBin[] copy = new ChartHistogramBin[bins.Length];
        int index = 0;
        while (index < bins.Length)
        {
            copy[index] = new ChartHistogramBin(bins[index].Minimum, bins[index].Maximum);
            copy[index].Count = bins[index].Count;
            index = index + 1;
        }

        return copy;
    }

    /// <summary>Returns the current bins.</summary>
    /// <returns>The current field value.</returns>
    public ChartHistogramBin[] GetBins()
    {
        return Bins;
    }

    /// <summary>Replaces the current bins.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBins(ChartHistogramBin[] value)
    {
        Bins = value;
    }

    /// <summary>Returns the current bin count.</summary>
    /// <returns>The current field value.</returns>
    public int GetBinCount()
    {
        return BinCount;
    }

    /// <summary>Replaces the current bin count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBinCount(int value)
    {
        BinCount = value;
    }

    /// <summary>Returns the current minimum.</summary>
    /// <returns>The current field value.</returns>
    public double GetMinimum()
    {
        return Minimum;
    }

    /// <summary>Replaces the current minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMinimum(double value)
    {
        Minimum = value;
    }

    /// <summary>Returns the current maximum.</summary>
    /// <returns>The current field value.</returns>
    public double GetMaximum()
    {
        return Maximum;
    }

    /// <summary>Replaces the current maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetMaximum(double value)
    {
        Maximum = value;
    }

    /// <summary>Returns the current bin width.</summary>
    /// <returns>The current field value.</returns>
    public double GetBinWidth()
    {
        return BinWidth;
    }

    /// <summary>Replaces the current bin width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBinWidth(double value)
    {
        BinWidth = value;
    }

    /// <summary>Returns the current value count.</summary>
    /// <returns>The current field value.</returns>
    public int GetValueCount()
    {
        return ValueCount;
    }

    /// <summary>Replaces the current value count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetValueCount(int value)
    {
        ValueCount = value;
    }

    /// <summary>Returns the current strategy.</summary>
    /// <returns>The current field value.</returns>
    public ChartHistogramBinStrategy GetStrategy()
    {
        return Strategy;
    }

    /// <summary>Replaces the current strategy.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStrategy(ChartHistogramBinStrategy value)
    {
        Strategy = value;
    }
}
