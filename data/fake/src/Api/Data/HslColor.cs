namespace fake;

/// <summary>Represents a color using hue, saturation, and lightness.</summary>
public class HslColor
{
    /// <summary>The hue in degrees from 0 inclusive to 360 exclusive.</summary>
    public double Hue;

    /// <summary>The saturation percentage from 0 through 100.</summary>
    public double Saturation;

    /// <summary>The lightness percentage from 0 through 100.</summary>
    public double Lightness;

    internal HslColor()
    {
    }
}
