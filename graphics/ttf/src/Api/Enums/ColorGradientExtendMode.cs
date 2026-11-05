namespace ttf;

/// <summary>Identifies how a COLR gradient continues beyond its color line.</summary>
public enum ColorGradientExtendMode
{
    /// <summary>Use the nearest end-stop color outside the color line.</summary>
    Pad = 0,

    /// <summary>Repeat the color line in the same direction.</summary>
    Repeat = 1,

    /// <summary>Repeat the color line with alternating direction.</summary>
    Reflect = 2
}
