namespace fake;

/// <summary>Represents an arithmetic CMYK conversion.</summary>
/// <remarks>The values are useful as synthetic color data and do not model a printer color profile.</remarks>
public class CmykColor
{
    /// <summary>The cyan percentage from 0 through 100.</summary>
    public double Cyan;

    /// <summary>The magenta percentage from 0 through 100.</summary>
    public double Magenta;

    /// <summary>The yellow percentage from 0 through 100.</summary>
    public double Yellow;

    /// <summary>The black key percentage from 0 through 100.</summary>
    public double Key;

    internal CmykColor()
    {
    }
}
