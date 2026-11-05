namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfPositionedGlyph"/>.</summary>
public interface IPdfPositionedGlyph
{
    /// <summary>Gets the glyph's zero-based visual index.</summary>
    /// <returns>The glyph order used by the encoded byte string.</returns>
    int GetVisualIndex();

    /// <summary>Gets the fixed-width PDF character code.</summary>
    /// <returns>The allocated one-byte or two-byte code value.</returns>
    int GetCharacterCode();

    /// <summary>Gets the allocated document CID.</summary>
    /// <returns>The CID selected by the character code.</returns>
    int GetCid();

    /// <summary>Gets the source-font glyph identifier.</summary>
    /// <returns>The original glyph identifier.</returns>
    int GetSourceGlyphId();

    /// <summary>Gets the original cluster's UTF-16 start.</summary>
    /// <returns>The zero-based start relative to the shaped run.</returns>
    int GetUtf16Start();

    /// <summary>Gets the original cluster's UTF-16 length.</summary>
    /// <returns>The cluster length in UTF-16 code units.</returns>
    int GetUtf16Length();

    /// <summary>Gets the shaped horizontal advance.</summary>
    /// <returns>The x advance in PDF 1000-unit text space.</returns>
    double GetXAdvance();

    /// <summary>Gets the shaped vertical advance.</summary>
    /// <returns>The y advance in PDF 1000-unit text space.</returns>
    double GetYAdvance();

    /// <summary>Gets the shaped horizontal placement offset.</summary>
    /// <returns>The x offset in PDF 1000-unit text space.</returns>
    double GetXOffset();

    /// <summary>Gets the shaped vertical placement offset.</summary>
    /// <returns>The y offset in PDF 1000-unit text space.</returns>
    double GetYOffset();

    /// <summary>Gets the requested shift perpendicular to the baseline.</summary>
    /// <returns>The baseline shift in caller/user-space points.</returns>
    double GetBaselineShift();

    /// <summary>Reports whether the glyph needs explicit placement.</summary>
    /// <returns>True for a shaping offset or nonzero baseline shift.</returns>
    bool HasExplicitPlacement();
}
