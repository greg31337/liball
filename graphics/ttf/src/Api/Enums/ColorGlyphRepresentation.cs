namespace ttf;

/// <summary>Identifies a glyph representation available to a color-glyph consumer.</summary>
public enum ColorGlyphRepresentation
{
    /// <summary>No supported representation is available.</summary>
    None = 0,

    /// <summary>A normalized COLR paint graph.</summary>
    Colr = 1,

    /// <summary>An OpenType SVG document.</summary>
    Svg = 2,

    /// <summary>An Apple <c>sbix</c> bitmap image.</summary>
    Sbix = 3,

    /// <summary>A color bitmap located by <c>CBLC</c> and stored in <c>CBDT</c>.</summary>
    Cbdt = 4,

    /// <summary>A monochrome bitmap located by <c>EBLC</c> and stored in <c>EBDT</c>.</summary>
    Ebdt = 5,

    /// <summary>The font's ordinary monochrome outline.</summary>
    MonochromeOutline = 6
}
