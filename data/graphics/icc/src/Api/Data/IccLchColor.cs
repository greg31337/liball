namespace icc;

/// <summary>Represents cylindrical CIELCH coordinates.</summary>
public class IccLchColor
{
    /// <summary>Gets or sets lightness L star.</summary>
    public double L;
    /// <summary>Gets or sets chroma C star.</summary>
    public double C;
    /// <summary>Gets or sets hue in degrees.</summary>
    public double HueDegrees;

    /// <summary>Initializes zero LCH coordinates.</summary>
    public IccLchColor()
    {
        L = 0.0;
        C = 0.0;
        HueDegrees = 0.0;
    }
}
