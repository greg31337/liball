namespace ttf;

/// <summary>Controls retention of TrueType hint programs and glyph instructions.</summary>
public enum FontSubsetHintPolicy
{
    /// <summary>Remove global hint tables and per-glyph instructions consistently.</summary>
    Drop = 0,

    /// <summary>Retain and rewrite supported hint data.</summary>
    Retain = 1
}
