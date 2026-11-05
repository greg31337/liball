namespace fake;

/// <summary>Represents a color using hue, whiteness, and blackness.</summary>
public class HwbColor
{
    /// <summary>The hue in degrees from 0 inclusive to 360 exclusive.</summary>
    public double Hue;

    /// <summary>The whiteness percentage from 0 through 100.</summary>
    public double Whiteness;

    /// <summary>The blackness percentage from 0 through 100.</summary>
    public double Blackness;

    internal HwbColor()
    {
    }
}
