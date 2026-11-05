namespace icc;

/// <summary>Represents a high-precision CIE XYZ colour.</summary>
public class IccXyzColor
{
    /// <summary>Gets or sets the X tristimulus value.</summary>
    public double X;
    /// <summary>Gets or sets the Y tristimulus value.</summary>
    public double Y;
    /// <summary>Gets or sets the Z tristimulus value.</summary>
    public double Z;
    /// <summary>Gets or sets whether the values use ICC-absolute colorimetry.</summary>
    public bool IsAbsolute;

    /// <summary>Initializes a zero media-relative XYZ colour.</summary>
    public IccXyzColor()
    {
        X = 0.0;
        Y = 0.0;
        Z = 0.0;
        IsAbsolute = false;
    }
}
