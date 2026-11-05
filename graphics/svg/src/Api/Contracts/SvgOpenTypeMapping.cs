namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgOpenTypeMapping"/>.</summary>
public interface ISvgOpenTypeMapping
{
    /// <summary>Maps one Unicode scalar through a deterministically selected Unicode character map.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="codePoint">The Unicode scalar value to map.</param>
    /// <returns>The mapped glyph or a deterministic missing-glyph result.</returns>
    SvgOpenTypeGlyphMapping MapCodePoint(SvgFont font, int codePoint);
    /// <summary>Maps a Unicode variation sequence using a format-14 character map.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="codePoint">The base Unicode scalar value.</param>
    /// <param name="variationSelector">A Unicode variation-selector scalar value.</param>
    /// <returns>The mapped glyph or a deterministic missing-glyph result.</returns>
    SvgOpenTypeGlyphMapping MapVariationSequence(SvgFont font, int codePoint, int variationSelector);
    /// <summary>Gets and scales one glyph's horizontal and optional vertical metrics.</summary>
    /// <param name="font">A successfully parsed OpenType font.</param>
    /// <param name="glyphIndex">The zero-based glyph index.</param>
    /// <param name="emSize">The finite nonnegative output em size.</param>
    /// <returns>The design-unit and scaled glyph metrics.</returns>
    SvgOpenTypeGlyphMetrics GetGlyphMetrics(SvgFont font, int glyphIndex, double emSize);
}
