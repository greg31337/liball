namespace ttf;

/// <summary>Controls retention of color glyph tables.</summary>
public enum FontSubsetColorPolicy
{
    /// <summary>Remove color tables while retaining ordinary outlines.</summary>
    Drop = 0,

    /// <summary>Retain supported color tables and their glyph closure.</summary>
    Retain = 1
}
