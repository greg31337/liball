namespace icc;

/// <summary>Defines RGB primaries, white point, and tone curves for profile construction.</summary>
public class IccRgbProfileDefinition
{
    /// <summary>Gets or sets red-primary chromaticity.</summary>
    public IccXyYColor RedPrimary;
    /// <summary>Gets or sets green-primary chromaticity.</summary>
    public IccXyYColor GreenPrimary;
    /// <summary>Gets or sets blue-primary chromaticity.</summary>
    public IccXyYColor BluePrimary;
    /// <summary>Gets or sets the source white point.</summary>
    public IccXyYColor WhitePoint;
    /// <summary>Gets or sets the red tone curve.</summary>
    public IccCurve RedCurve;
    /// <summary>Gets or sets the green tone curve.</summary>
    public IccCurve GreenCurve;
    /// <summary>Gets or sets the blue tone curve.</summary>
    public IccCurve BlueCurve;
    /// <summary>Gets or sets optional version-4 coding-independent code-point metadata.</summary>
    public IccCicp? Cicp;

    /// <summary>Initializes an empty RGB definition with identity curves.</summary>
    public IccRgbProfileDefinition()
    {
        RedPrimary = new IccXyYColor();
        GreenPrimary = new IccXyYColor();
        BluePrimary = new IccXyYColor();
        WhitePoint = new IccXyYColor();
        RedCurve = new IccCurve();
        GreenCurve = new IccCurve();
        BlueCurve = new IccCurve();
        Cicp = null;
    }
}
