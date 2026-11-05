namespace chart;
/// <summary>Specifies how public statistics operations handle not-a-number values.</summary>
public enum ChartNaNPolicy
{
    /// <summary>Rejects input containing a not-a-number value.</summary>
    Reject = 0,
    /// <summary>Excludes not-a-number values from the calculation.</summary>
    Skip = 1,
    /// <summary>Returns not-a-number when any input value is not-a-number.</summary>
    Propagate = 2
}
