namespace icc;

/// <summary>Represents an identity, gamma, sampled, parametric, or segmented curve.</summary>
public class IccCurve
{
    /// <summary>Gets or sets the active curve representation.</summary>
    public IccCurveKind Kind;
    /// <summary>Gets or sets the gamma exponent.</summary>
    public double Gamma;
    /// <summary>Gets or sets ordered sampled values.</summary>
    public double[] Samples;
    /// <summary>Gets or sets parametric curve data when active.</summary>
    public IccParametricCurve? Parametric;
    /// <summary>Gets or sets segmented curve data.</summary>
    public IccCurveSegment[] Segments;
    /// <summary>Gets or sets optional curve analysis.</summary>
    public IccCurveAnalysis? Analysis;
    /// <summary>Gets or sets the first input value for an ICC.2 single sampled curve.</summary>
    public double InputMinimum;
    /// <summary>Gets or sets the last input value for an ICC.2 single sampled curve.</summary>
    public double InputMaximum;
    /// <summary>Gets or sets the ICC.2 lookup-extension selector.</summary>
    public int Extension;
    /// <summary>Gets or sets the ICC.2 value-encoding selector.</summary>
    public int Encoding;

    /// <summary>Initializes an identity curve.</summary>
    public IccCurve()
    {
        Kind = IccCurveKind.Identity;
        Gamma = 1.0;
        Samples = new double[0];
        Parametric = null;
        Segments = new IccCurveSegment[0];
        Analysis = null;
        InputMinimum = 0.0;
        InputMaximum = 1.0;
        Extension = 0;
        Encoding = 0;
    }
}
