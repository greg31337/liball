namespace icc;

/// <summary>Represents adaptive gain curve metadata and interpolation nodes.</summary>
public class IccAdaptiveGainCurve
{
    /// <summary>Gets or sets the 16-byte curve identifier.</summary>
    public byte[] Identifier;
    /// <summary>Gets or sets target headroom.</summary>
    public double TargetHeadroom;
    /// <summary>Gets or sets alternate target headroom.</summary>
    public double AlternateHeadroom;
    /// <summary>Gets or sets retained function metadata from header offsets 36 through 87.</summary>
    public byte[] FunctionData;
    /// <summary>Gets or sets backward-compatible AToB0, AToB1, and AToB2 target headrooms.</summary>
    public double[] BackwardCompatibleTargetHeadrooms;
    /// <summary>Gets or sets red-channel nodes.</summary>
    public IccAdaptiveGainCurveNode[] RedNodes;
    /// <summary>Gets or sets green-channel nodes.</summary>
    public IccAdaptiveGainCurveNode[] GreenNodes;
    /// <summary>Gets or sets blue-channel nodes.</summary>
    public IccAdaptiveGainCurveNode[] BlueNodes;
    /// <summary>Gets or sets whether the curve is image-specific.</summary>
    public bool IsImageSpecific;

    /// <summary>Initializes an empty curve.</summary>
    public IccAdaptiveGainCurve()
    {
        Identifier = new byte[16];
        TargetHeadroom = 0.0;
        AlternateHeadroom = 0.0;
        FunctionData = new byte[52];
        BackwardCompatibleTargetHeadrooms = new double[3];
        RedNodes = new IccAdaptiveGainCurveNode[0];
        GreenNodes = new IccAdaptiveGainCurveNode[0];
        BlueNodes = new IccAdaptiveGainCurveNode[0];
        IsImageSpecific = false;
    }
}
