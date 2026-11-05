namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgCffOutlineDecoder"/>.</summary>
public interface ISvgCffOutlineDecoder
{
    /// <summary>Decodes one CFF or CFF2 glyph using bounded defaults.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <returns>The decoded outline, stable failure, or caller-fallback classification.</returns>
    SvgCffGlyphResult DecodeWithFontAndGlyphIndex(SvgFont font, int glyphIndex);
    /// <summary>Decodes one CFF or CFF2 glyph using explicit operand and subroutine limits.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="options">The bounded outline options.</param>
    /// <returns>The decoded outline, stable failure, or caller-fallback classification.</returns>
    SvgCffGlyphResult DecodeWithFontAndGlyphIndexAndOptions(SvgFont font, int glyphIndex, SvgCffOutlineOptions options);
}
