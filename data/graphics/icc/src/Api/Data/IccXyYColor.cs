namespace icc;

/// <summary>Represents a CIE xyY colour.</summary>
public class IccXyYColor
{
    /// <summary>Gets or sets the x chromaticity coordinate.</summary>
    public double X;
    /// <summary>Gets or sets the y chromaticity coordinate.</summary>
    public double Y;
    /// <summary>Gets or sets the luminance Y value.</summary>
    public double Luminance;

    /// <summary>Initializes a zero xyY colour.</summary>
    public IccXyYColor()
    {
        X = 0.0;
        Y = 0.0;
        Luminance = 0.0;
    }
}
