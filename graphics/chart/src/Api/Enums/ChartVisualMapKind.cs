namespace chart;
/// <summary>
/// Specifies the supported visual map kind values.
/// </summary>
public enum ChartVisualMapKind
{
    /// <summary>
    /// Uses the continuous option for the visual map kind.
    /// </summary>
    Continuous = 0,
    /// <summary>
    /// Uses the binned option for the visual map kind.
    /// </summary>
    Binned = 1,
    /// <summary>
    /// Uses the threshold option for the visual map kind.
    /// </summary>
    Threshold = 2,
    /// <summary>
    /// Uses the categorical option for the visual map kind.
    /// </summary>
    Categorical = 3
}
