namespace icc;

/// <summary>Represents a formula or sampled segment of an ICC curve.</summary>
public class IccCurveSegment
{
    /// <summary>Gets or sets the segment representation.</summary>
    public IccCurveSegmentKind Kind;
    /// <summary>Gets or sets the ICC formula selector when the segment is formula-based.</summary>
    public int FormulaType;
    /// <summary>Gets or sets the segment start breakpoint.</summary>
    public double BreakPoint;
    /// <summary>Gets or sets formula parameters.</summary>
    public double[] Parameters;
    /// <summary>Gets or sets sampled values.</summary>
    public double[] Samples;

    /// <summary>Initializes an empty formula segment.</summary>
    public IccCurveSegment()
    {
        Kind = IccCurveSegmentKind.Formula;
        FormulaType = 0;
        BreakPoint = 0.0;
        Parameters = new double[0];
        Samples = new double[0];
    }
}
