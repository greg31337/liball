namespace icc;

/// <summary>Represents the curves and channel functions of an ICC.2 tone-map processing element.</summary>
public class IccToneMap
{
    /// <summary>Gets or sets the curve applied to the luminance input channel.</summary>
    public IccCurve LuminanceCurve;
    /// <summary>Gets or sets one tone-mapping function for each output channel.</summary>
    public IccToneMapFunction[] Functions;

    /// <summary>Initializes an empty tone map.</summary>
    public IccToneMap()
    {
        LuminanceCurve = new IccCurve();
        Functions = new IccToneMapFunction[0];
    }
}
