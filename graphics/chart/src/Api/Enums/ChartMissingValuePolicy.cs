namespace chart;
/// <summary>
/// Specifies the supported missing value policy values.
/// </summary>
public enum ChartMissingValuePolicy
{
    /// <summary>
    /// Uses the skip option for the missing value policy.
    /// </summary>
    Skip = 0,
    /// <summary>
    /// Uses the gap option for the missing value policy.
    /// </summary>
    Gap = 1,
    /// <summary>
    /// Uses the zero option for the missing value policy.
    /// </summary>
    Zero = 2,
    /// <summary>
    /// Uses the interpolate option for the missing value policy.
    /// </summary>
    Interpolate = 3,
    /// <summary>
    /// Uses the carry forward option for the missing value policy.
    /// </summary>
    CarryForward = 4
}
