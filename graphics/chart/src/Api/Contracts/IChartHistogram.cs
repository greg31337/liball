namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartHistogram"/>.</summary>
public interface IChartHistogram
{
    /// <summary>
    /// Returns the requested bin.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting histogram bin.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartHistogramBin GetBin(int index);
    /// <summary>
    /// Converts this value to tal count.
    /// </summary>
    /// <returns>The converted int.</returns>
    int TotalCount();
    /// <summary>
    /// Creates an independent copy of the bins.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartHistogramBin[] CopyBins();
    /// <summary>Returns the current bins.</summary>
    /// <returns>The current field value.</returns>
    ChartHistogramBin[] GetBins();
    /// <summary>Replaces the current bins.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBins(ChartHistogramBin[] value);
    /// <summary>Returns the current bin count.</summary>
    /// <returns>The current field value.</returns>
    int GetBinCount();
    /// <summary>Replaces the current bin count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBinCount(int value);
    /// <summary>Returns the current minimum.</summary>
    /// <returns>The current field value.</returns>
    double GetMinimum();
    /// <summary>Replaces the current minimum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMinimum(double value);
    /// <summary>Returns the current maximum.</summary>
    /// <returns>The current field value.</returns>
    double GetMaximum();
    /// <summary>Replaces the current maximum.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetMaximum(double value);
    /// <summary>Returns the current bin width.</summary>
    /// <returns>The current field value.</returns>
    double GetBinWidth();
    /// <summary>Replaces the current bin width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBinWidth(double value);
    /// <summary>Returns the current value count.</summary>
    /// <returns>The current field value.</returns>
    int GetValueCount();
    /// <summary>Replaces the current value count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetValueCount(int value);
    /// <summary>Returns the current strategy.</summary>
    /// <returns>The current field value.</returns>
    ChartHistogramBinStrategy GetStrategy();
    /// <summary>Replaces the current strategy.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStrategy(ChartHistogramBinStrategy value);
}
