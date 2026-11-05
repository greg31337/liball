namespace fake;

/// <summary>Represents a CIE L*a*b* color relative to the D65 reference white.</summary>
public class LabColor
{
    /// <summary>The perceptual lightness from 0 through 100.</summary>
    public double Lightness;

    /// <summary>The green-red opponent coordinate.</summary>
    public double A;

    /// <summary>The blue-yellow opponent coordinate.</summary>
    public double B;

    internal LabColor()
    {
    }
}
