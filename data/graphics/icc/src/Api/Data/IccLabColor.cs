namespace icc;

/// <summary>Represents a high-precision CIELAB colour.</summary>
public class IccLabColor
{
    /// <summary>Gets or sets CIE lightness L star.</summary>
    public double L;
    /// <summary>Gets or sets the a star opponent component.</summary>
    public double A;
    /// <summary>Gets or sets the b star opponent component.</summary>
    public double B;

    /// <summary>Initializes a zero CIELAB colour.</summary>
    public IccLabColor()
    {
        L = 0.0;
        A = 0.0;
        B = 0.0;
    }
}
