namespace chart;
/// <summary>
/// Specifies the supported sort kind values.
/// </summary>
public enum ChartSortKind
{
    /// <summary>
    /// Uses the none option for the sort kind.
    /// </summary>
    None = 0,
    /// <summary>
    /// Uses the x option for the sort kind.
    /// </summary>
    X = 1,
    /// <summary>
    /// Uses the y option for the sort kind.
    /// </summary>
    Y = 2,
    /// <summary>
    /// Uses the category option for the sort kind.
    /// </summary>
    Category = 3,
    /// <summary>
    /// Uses the value option for the sort kind.
    /// </summary>
    Value = 4,
    /// <summary>
    /// Uses the series name option for the sort kind.
    /// </summary>
    SeriesName = 5,
    /// <summary>
    /// Uses the custom option for the sort kind.
    /// </summary>
    Custom = 6
}
