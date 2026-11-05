namespace chart;
/// <summary>
/// Specifies the supported aggregation kind values.
/// </summary>
public enum ChartAggregationKind
{
    /// <summary>
    /// Uses the count option for the aggregation kind.
    /// </summary>
    Count = 0,
    /// <summary>
    /// Uses the sum option for the aggregation kind.
    /// </summary>
    Sum = 1,
    /// <summary>
    /// Uses the mean option for the aggregation kind.
    /// </summary>
    Mean = 2,
    /// <summary>
    /// Uses the median option for the aggregation kind.
    /// </summary>
    Median = 3,
    /// <summary>
    /// Uses the min option for the aggregation kind.
    /// </summary>
    Min = 4,
    /// <summary>
    /// Uses the max option for the aggregation kind.
    /// </summary>
    Max = 5,
    /// <summary>
    /// Uses the first option for the aggregation kind.
    /// </summary>
    First = 6,
    /// <summary>
    /// Uses the last option for the aggregation kind.
    /// </summary>
    Last = 7,
    /// <summary>
    /// Uses the variance option for the aggregation kind.
    /// </summary>
    Variance = 8,
    /// <summary>
    /// Uses the standard deviation option for the aggregation kind.
    /// </summary>
    StandardDeviation = 9,
    /// <summary>
    /// Uses the percentile option for the aggregation kind.
    /// </summary>
    Percentile = 10
}
