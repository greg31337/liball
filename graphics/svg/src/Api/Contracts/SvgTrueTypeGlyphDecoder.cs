namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgTrueTypeGlyphDecoder"/>.</summary>
public interface ISvgTrueTypeGlyphDecoder
{
    /// <summary>Decodes one TrueType glyph using bounded defaults.</summary>
    /// <param name="font">A successfully parsed TrueType OpenType font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <returns>The expanded glyph outline and quadratic SVG path.</returns>
    SvgTrueTypeGlyphResult DecodeWithFontAndGlyphIndex(SvgFont font, int glyphIndex);
    /// <summary>Decodes one TrueType glyph using explicit point, component, and recursion limits.</summary>
    /// <param name="font">A successfully parsed TrueType OpenType font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="options">The bounded glyph expansion options.</param>
    /// <returns>The expanded glyph outline and quadratic SVG path.</returns>
    SvgTrueTypeGlyphResult DecodeWithFontAndGlyphIndexAndOptions(SvgFont font, int glyphIndex, SvgTrueTypeGlyphOptions options);
}
