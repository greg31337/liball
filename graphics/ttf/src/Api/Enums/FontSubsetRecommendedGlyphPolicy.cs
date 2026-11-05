namespace ttf;

/// <summary>Controls retention of conventional interoperability glyphs.</summary>
public enum FontSubsetRecommendedGlyphPolicy
{
    /// <summary>Retain only requested glyphs in addition to required glyph zero.</summary>
    RequestedOnly = 0,

    /// <summary>Retain conventional control and space glyphs when available.</summary>
    IncludeRecommended = 1
}
