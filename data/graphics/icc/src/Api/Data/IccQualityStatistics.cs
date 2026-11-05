namespace icc;

/// <summary>Reports aggregate transform and round-trip quality statistics.</summary>
public class IccQualityStatistics
{
    /// <summary>Gets or sets the sample count.</summary>
    public int SampleCount;
    /// <summary>Gets or sets the mean colour difference.</summary>
    public double Mean;
    /// <summary>Gets or sets the maximum colour difference.</summary>
    public double Maximum;
    /// <summary>Gets or sets the 95th-percentile colour difference.</summary>
    public double Percentile95;
    /// <summary>Gets or sets the mean first-round-trip CIEDE2000 difference.</summary>
    public double FirstRoundTripMean;
    /// <summary>Gets or sets the maximum first-round-trip CIEDE2000 difference.</summary>
    public double FirstRoundTripMaximum;
    /// <summary>Gets or sets the 95th-percentile first-round-trip CIEDE2000 difference.</summary>
    public double FirstRoundTripPercentile95;
    /// <summary>Gets or sets the mean second-round-trip CIEDE2000 difference.</summary>
    public double SecondRoundTripMean;
    /// <summary>Gets or sets the maximum second-round-trip CIEDE2000 difference.</summary>
    public double SecondRoundTripMaximum;
    /// <summary>Gets or sets the 95th-percentile second-round-trip CIEDE2000 difference.</summary>
    public double SecondRoundTripPercentile95;
    /// <summary>Gets or sets the number of selected transform curves analyzed.</summary>
    public int CurveCount;
    /// <summary>Gets or sets whether every selected transform curve is continuous.</summary>
    public bool CurvesAreContinuous;
    /// <summary>Gets or sets whether every selected transform curve is invertible.</summary>
    public bool CurvesAreInvertible;
    /// <summary>Gets or sets the largest selected-curve inverse round-trip error.</summary>
    public double CurveMaximumRoundTripError;
    /// <summary>Gets or sets the largest CIEDE2000 change across a local transform interval.</summary>
    public double TransformContinuityMaximum;
    /// <summary>Gets or sets the mean local transform smoothness residual in CIEDE2000.</summary>
    public double TransformSmoothnessMean;
    /// <summary>Gets or sets the maximum local transform smoothness residual in CIEDE2000.</summary>
    public double TransformSmoothnessMaximum;
    /// <summary>Gets or sets the 95th-percentile local transform smoothness residual in CIEDE2000.</summary>
    public double TransformSmoothnessPercentile95;

    /// <summary>Initializes empty statistics.</summary>
    public IccQualityStatistics()
    {
        SampleCount = 0;
        Mean = 0.0;
        Maximum = 0.0;
        Percentile95 = 0.0;
        FirstRoundTripMean = 0.0;
        FirstRoundTripMaximum = 0.0;
        FirstRoundTripPercentile95 = 0.0;
        SecondRoundTripMean = 0.0;
        SecondRoundTripMaximum = 0.0;
        SecondRoundTripPercentile95 = 0.0;
        CurveCount = 0;
        CurvesAreContinuous = true;
        CurvesAreInvertible = true;
        CurveMaximumRoundTripError = 0.0;
        TransformContinuityMaximum = 0.0;
        TransformSmoothnessMean = 0.0;
        TransformSmoothnessMaximum = 0.0;
        TransformSmoothnessPercentile95 = 0.0;
    }
}
