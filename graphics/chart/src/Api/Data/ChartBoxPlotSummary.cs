namespace chart;
/// <summary>
/// Represents a box plot summary used by the chart model or rendering pipeline.
/// </summary>
public class ChartBoxPlotSummary
{
    /// <summary>
    /// The minimum used by this box plot summary.
    /// </summary>
    public double Minimum;
    /// <summary>
    /// The first quartile used by this box plot summary.
    /// </summary>
    public double FirstQuartile;
    /// <summary>
    /// The median used by this box plot summary.
    /// </summary>
    public double Median;
    /// <summary>
    /// The third quartile used by this box plot summary.
    /// </summary>
    public double ThirdQuartile;
    /// <summary>
    /// The maximum used by this box plot summary.
    /// </summary>
    public double Maximum;
    /// <summary>
    /// The interquartile range used by this box plot summary.
    /// </summary>
    public double InterquartileRange;
    /// <summary>
    /// The lower fence used by this box plot summary.
    /// </summary>
    public double LowerFence;
    /// <summary>
    /// The upper fence used by this box plot summary.
    /// </summary>
    public double UpperFence;
    /// <summary>
    /// The whisker minimum used by this box plot summary.
    /// </summary>
    public double WhiskerMinimum;
    /// <summary>
    /// The whisker maximum used by this box plot summary.
    /// </summary>
    public double WhiskerMaximum;
    /// <summary>
    /// Initializes a box plot summary from the supplied minimum, first quartile, median, third quartile, and maximum.
    /// </summary>
    /// <param name = "minimum">The inclusive lower end of the numeric domain.</param>
    /// <param name = "firstQuartile">The first quartile.</param>
    /// <param name = "median">The median.</param>
    /// <param name = "thirdQuartile">The third quartile.</param>
    /// <param name = "maximum">The inclusive upper end of the numeric domain.</param>
    public ChartBoxPlotSummary(double minimum, double firstQuartile, double median, double thirdQuartile, double maximum)
    {
        ChartLinearScale.ValidateFinite(minimum, "minimum");
        ChartLinearScale.ValidateFinite(firstQuartile, "firstQuartile");
        ChartLinearScale.ValidateFinite(median, "median");
        ChartLinearScale.ValidateFinite(thirdQuartile, "thirdQuartile");
        ChartLinearScale.ValidateFinite(maximum, "maximum");
        Minimum = minimum;
        FirstQuartile = firstQuartile;
        Median = median;
        ThirdQuartile = thirdQuartile;
        Maximum = maximum;
        InterquartileRange = thirdQuartile - firstQuartile;
        LowerFence = firstQuartile - 1.5 * InterquartileRange;
        UpperFence = thirdQuartile + 1.5 * InterquartileRange;
        WhiskerMinimum = minimum;
        WhiskerMaximum = maximum;
    }
}
