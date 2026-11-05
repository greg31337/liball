namespace chart;
/// <summary>
/// Specifies the supported histogram bin strategy values.
/// </summary>
public enum ChartHistogramBinStrategy
{
    /// <summary>
    /// Uses the fixed count option for the histogram bin strategy.
    /// </summary>
    FixedCount = 0,
    /// <summary>
    /// Uses the fixed width option for the histogram bin strategy.
    /// </summary>
    FixedWidth = 1,
    /// <summary>
    /// Uses the sturges option for the histogram bin strategy.
    /// </summary>
    Sturges = 2,
    /// <summary>
    /// Uses the scott option for the histogram bin strategy.
    /// </summary>
    Scott = 3,
    /// <summary>
    /// Uses the freedman diaconis option for the histogram bin strategy.
    /// </summary>
    FreedmanDiaconis = 4
}
