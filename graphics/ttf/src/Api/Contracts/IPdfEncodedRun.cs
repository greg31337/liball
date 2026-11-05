namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfEncodedRun"/>.</summary>
public interface IPdfEncodedRun
{
    /// <summary>Copies the encoded PDF character bytes in visual glyph order.</summary>
    /// <returns>One or two bytes per glyph according to the code width.</returns>
    byte[] GetCodes();

    /// <summary>Gets the fixed number of bytes used by each character code.</summary>
    /// <returns>One for a simple font or two for a Type 0 font.</returns>
    int GetCodeByteCount();

    /// <summary>Gets the number of encoded glyphs.</summary>
    /// <returns>The glyph count.</returns>
    int GetGlyphCount();

    /// <summary>Gets one document CID.</summary>
    /// <param name="index">The zero-based visual glyph index.</param>
    /// <returns>The document CID represented by the character code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the run.
    /// </exception>
    int GetCid(int index);

    /// <summary>Gets one source glyph identifier.</summary>
    /// <param name="index">The zero-based visual glyph index.</param>
    /// <returns>The glyph identifier in the source font instance.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the run.
    /// </exception>
    int GetSourceGlyphId(int index);

    /// <summary>Copies the document CIDs in visual glyph order.</summary>
    /// <returns>A new array containing one CID per glyph.</returns>
    int[] GetCids();

    /// <summary>Copies the source glyph identifiers in visual order.</summary>
    /// <returns>A new array containing one source GID per glyph.</returns>
    int[] GetSourceGlyphIds();

    /// <summary>Gets the writing mode used by this encoded run.</summary>
    /// <returns>The horizontal or vertical writing mode.</returns>
    PdfWritingMode GetWritingMode();

    /// <summary>Gets one positioned glyph in encoded visual order.</summary>
    /// <param name="index">The zero-based visual glyph index.</param>
    /// <returns>The immutable code, source range, and position data.</returns>
    PdfPositionedGlyph GetGlyph(int index);

    /// <summary>Copies the positioned glyphs in encoded visual order.</summary>
    /// <returns>A new array containing immutable positioned glyphs.</returns>
    PdfPositionedGlyph[] GetGlyphs();

    /// <summary>Copies the positioned glyphs for text-matrix translation.</summary>
    /// <returns>A new array containing immutable positioned glyphs.</returns>
    PdfPositionedGlyph[] GetPositions();

    /// <summary>Gets the number of nonzero suggested <c>TJ</c> values.</summary>
    /// <returns>The text-adjustment count.</returns>
    int GetTextAdjustmentCount();

    /// <summary>Gets one suggested <c>TJ</c> adjustment.</summary>
    /// <param name="index">The zero-based adjustment index.</param>
    /// <returns>The immutable glyph index and numeric value.</returns>
    PdfTjAdjustment GetTextAdjustment(int index);

    /// <summary>Copies all suggested <c>TJ</c> adjustments.</summary>
    /// <returns>A new array in visual-glyph order.</returns>
    PdfTjAdjustment[] GetTextAdjustments();

    /// <summary>Gets the shaped run's total horizontal advance.</summary>
    /// <returns>The x advance in PDF 1000-unit text space.</returns>
    double GetXAdvance();

    /// <summary>Gets the shaped run's total vertical advance.</summary>
    /// <returns>The y advance in PDF 1000-unit text space.</returns>
    double GetYAdvance();

    /// <summary>Gets the run-wide shift perpendicular to the baseline.</summary>
    /// <returns>The baseline shift in caller/user-space points.</returns>
    double GetBaselineShift();

    /// <summary>Gets the source range start relative to the shaped run.</summary>
    /// <returns>Zero.</returns>
    int GetSourceUtf16Start();

    /// <summary>Gets the shaped source range length.</summary>
    /// <returns>The source length in UTF-16 code units.</returns>
    int GetSourceUtf16Length();
}
