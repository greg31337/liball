namespace chart;
/// <summary>Specifies how public statistics operations handle positive and negative infinity.</summary>
public enum ChartInfinityPolicy
{
    /// <summary>Rejects input containing an infinite value.</summary>
    Reject = 0,
    /// <summary>Excludes infinite values from the calculation.</summary>
    Skip = 1,
    /// <summary>Includes infinity and follows ordinary IEEE floating-point results.</summary>
    Allow = 2
}
