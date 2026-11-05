namespace fake;

/// <summary>Represents cylindrical CIE L*C*h color relative to D65.</summary>
public class LchColor
{
    /// <summary>The perceptual lightness from 0 through 100.</summary>
    public double Lightness;

    /// <summary>The nonnegative chroma.</summary>
    public double Chroma;

    /// <summary>The hue angle in degrees from 0 inclusive to 360 exclusive.</summary>
    public double Hue;

    internal LchColor()
    {
    }
}
