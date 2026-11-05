namespace icc;

/// <summary>Defines a gray profile white point and tone curve.</summary>
public class IccGrayProfileDefinition
{
    /// <summary>Gets or sets the source white point.</summary>
    public IccXyYColor WhitePoint;
    /// <summary>Gets or sets the gray tone curve.</summary>
    public IccCurve GrayCurve;

    /// <summary>Initializes an empty gray definition with an identity curve.</summary>
    public IccGrayProfileDefinition()
    {
        WhitePoint = new IccXyYColor();
        GrayCurve = new IccCurve();
    }
}
