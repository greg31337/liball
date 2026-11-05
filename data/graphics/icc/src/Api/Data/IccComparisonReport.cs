namespace icc;

/// <summary>Reports exact and approximate profile comparison results.</summary>
public class IccComparisonReport
{
    /// <summary>Gets or sets whether semantic profile data is exactly equal.</summary>
    public bool IsExactMatch;
    /// <summary>Gets or sets whether transforms are approximately equivalent.</summary>
    public bool IsApproximatelyEquivalent;
    /// <summary>Gets or sets the first content fingerprint.</summary>
    public byte[] FirstFingerprint;
    /// <summary>Gets or sets the second content fingerprint.</summary>
    public byte[] SecondFingerprint;
    /// <summary>Gets or sets individual colour differences.</summary>
    public IccColorDifference[] Differences;
    /// <summary>Gets or sets aggregate quality statistics.</summary>
    public IccQualityStatistics Statistics;
    /// <summary>Gets or sets the comparison outcome.</summary>
    public IccOperationResult Result;
    /// <summary>Gets or sets an optional transform plan used for sampled comparison.</summary>
    public IccTransformPlan? TransformPlan;

    /// <summary>Initializes an unequal, unmeasured report.</summary>
    public IccComparisonReport()
    {
        IsExactMatch = false;
        IsApproximatelyEquivalent = false;
        FirstFingerprint = new byte[0];
        SecondFingerprint = new byte[0];
        Differences = new IccColorDifference[0];
        Statistics = new IccQualityStatistics();
        Result = new IccOperationResult();
        TransformPlan = null;
    }
}
