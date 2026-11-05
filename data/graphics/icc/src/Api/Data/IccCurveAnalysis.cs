namespace icc;

/// <summary>Reports numerical characteristics of an ICC curve.</summary>
public class IccCurveAnalysis
{
    /// <summary>Gets or sets whether the curve is monotonic.</summary>
    public bool IsMonotonic;
    /// <summary>Gets or sets whether sampled outputs are non-decreasing.</summary>
    public bool IsIncreasing;
    /// <summary>Gets or sets whether sampled outputs are non-increasing.</summary>
    public bool IsDecreasing;
    /// <summary>Gets or sets whether formula and segment boundaries are continuous.</summary>
    public bool IsContinuous;
    /// <summary>Gets or sets whether the curve is invertible.</summary>
    public bool IsInvertible;
    /// <summary>Gets or sets the lower declared or operational input-domain bound.</summary>
    public double DomainMinimum;
    /// <summary>Gets or sets the upper declared or operational input-domain bound.</summary>
    public double DomainMaximum;
    /// <summary>Gets or sets the minimum sampled output.</summary>
    public double Minimum;
    /// <summary>Gets or sets the maximum sampled output.</summary>
    public double Maximum;
    /// <summary>Gets or sets the minimum sampled slope.</summary>
    public double MinimumSlope;
    /// <summary>Gets or sets the maximum sampled slope.</summary>
    public double MaximumSlope;
    /// <summary>Gets or sets the largest sampled input error after forward and inverse evaluation.</summary>
    public double MaximumRoundTripError;
    /// <summary>Gets or sets the number of analysis samples.</summary>
    public int SampleCount;
    /// <summary>Gets or sets the analysis outcome.</summary>
    public IccOperationResult Result;

    /// <summary>Initializes an unclassified curve analysis.</summary>
    public IccCurveAnalysis()
    {
        IsMonotonic = false;
        IsIncreasing = false;
        IsDecreasing = false;
        IsContinuous = false;
        IsInvertible = false;
        DomainMinimum = 0.0;
        DomainMaximum = 1.0;
        Minimum = 0.0;
        Maximum = 0.0;
        MinimumSlope = 0.0;
        MaximumSlope = 0.0;
        MaximumRoundTripError = 0.0;
        SampleCount = 0;
        Result = new IccOperationResult();
    }
}
