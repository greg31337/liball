namespace svg;

/// <summary>Defines bounded variable and color OpenType profile operations.</summary>
public interface ISvgOpenTypeFontProcessor
{
    /// <summary>Reads variation axes, palettes, and supported color glyph paints.</summary>
    /// <param name="font">A successfully parsed retained font.</param>
    /// <param name="options">The profile bounds and cancellation token.</param>
    /// <returns>The parsed profile or stable failure.</returns>
    SvgOpenTypeFontProfile ReadProfileWithFontAndOptions(SvgFont font, SvgOpenTypeProfileOptions options);
    /// <summary>Instantiates one glyph at explicit variation coordinates and palette.</summary>
    /// <param name="font">A successfully parsed retained font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="options">The coordinates, output size, palette, and bounds.</param>
    /// <returns>The instantiated glyph or precise callback requirement.</returns>
    SvgOpenTypeGlyphInstance InstantiateGlyphWithFontAndGlyphIndexAndOptions(SvgFont font, int glyphIndex, SvgOpenTypeInstanceOptions options);
}
