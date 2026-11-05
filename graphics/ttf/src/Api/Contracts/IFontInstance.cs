namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontInstance"/>.</summary>
public interface IFontInstance
{
    /// <summary>Gets the source face.</summary>
    /// <returns>The immutable source face.</returns>
    FontFace GetFace();

    /// <summary>Gets the immutable resolved variation coordinates.</summary>
    /// <returns>The clamped and normalized coordinate set.</returns>
    NormalizedFontCoordinates GetCoordinates();

    /// <summary>Shapes a UTF-16 string with default options.</summary>
    /// <param name="text">The source text.</param>
    /// <returns>The immutable visual shaping runs and diagnostics.</returns>
    TextShapingResult ShapeText(string text);

    /// <summary>Shapes a UTF-16 string with explicit options.</summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The Unicode and OpenType shaping controls.</param>
    /// <returns>The immutable visual shaping runs and diagnostics.</returns>
    TextShapingResult ShapeTextWithOptions(
        string text,
        TextShapingOptions options);

    /// <summary>Shapes a UTF-16 character span with default options.</summary>
    /// <param name="text">The source text span.</param>
    /// <returns>The immutable visual shaping runs and diagnostics.</returns>
    TextShapingResult ShapeSpan(ReadOnlySpan<char> text);

    /// <summary>Shapes a UTF-16 character span with explicit options.</summary>
    /// <param name="text">The source text span.</param>
    /// <param name="options">The Unicode and OpenType shaping controls.</param>
    /// <returns>The immutable visual shaping runs and diagnostics.</returns>
    TextShapingResult ShapeSpanWithOptions(
        ReadOnlySpan<char> text,
        TextShapingOptions options);

    /// <summary>Gets the glyph count.</summary>
    /// <returns>The number of glyph identifiers in the instance.</returns>
    int GetGlyphCount();

    /// <summary>Gets preserved global TrueType hint data.</summary>
    /// <remarks>
    /// Programs and control values are returned for preservation and inspection.
    /// This library does not execute hints, grid-fit outlines, or rasterize glyphs.
    /// </remarks>
    /// <returns>The stable data view, or null for non-TrueType outlines.</returns>
    TrueTypeHintingData? GetTrueTypeHintingData();

    /// <summary>Gets one resolved normalized glyph outline.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The stable immutable glyph outline.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    /// <exception cref="FontFormatException">The face does not contain supported outlines.</exception>
    GlyphOutline GetGlyphOutline(int glyphId);

    /// <summary>Copies this instance as a standalone static SFNT font.</summary>
    /// <remarks>
    /// TrueType or CFF2 outlines and associated metrics are materialized.
    /// Variation-only tables and invalidated digital signatures are omitted
    /// from the returned font. CFF2 outlines are emitted as static CFF1.
    /// Repeated calls are byte-for-byte deterministic and return independent
    /// arrays.
    /// </remarks>
    /// <returns>A new array containing the static font bytes.</returns>
    /// <exception cref="FontFormatException">
    /// The face does not contain supported outlines or required variation data
    /// is unavailable or malformed.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The static font exceeds a configured resource limit.
    /// </exception>
    byte[] CopyStaticSfntBytes();

    /// <summary>Copies this instance as static SFNT using explicit operation controls.</summary>
    /// <param name="options">Policy, limits, and cancellation controls.</param>
    /// <returns>A new array containing the static font bytes.</returns>
    byte[] CopyStaticSfntBytesWithOptions(FontOperationOptions options);

    /// <summary>Gets one glyph's exact normalized path bounds.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The stable immutable exact bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    /// <exception cref="FontFormatException">The face does not contain supported outlines.</exception>
    GlyphBounds? GetGlyphBounds(int glyphId);

    /// <summary>Measures one horizontal glyph at a point size.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The scaled advance and exact outline bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>glyphId</c> is outside the font or <c>pointSize</c> is invalid.
    /// </exception>
    /// <exception cref="FontFormatException">
    /// Required outline, metric, or units-per-em data is unavailable.
    /// </exception>
    TextMeasurement MeasureGlyph(int glyphId, double pointSize);

    /// <summary>Measures one horizontal glyph with point-based style adjustments.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="style">The point size and geometric style adjustments.</param>
    /// <returns>The adjusted advance and exact transformed outline bounds.</returns>
    /// <remarks>
    /// Character and word spacing require shaped source clusters and therefore
    /// do not affect this single-glyph measurement.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><c>style</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>glyphId</c> is outside the font.
    /// </exception>
    /// <exception cref="FontFormatException">
    /// Required outline, metric, or units-per-em data is unavailable.
    /// </exception>
    TextMeasurement MeasureStyledGlyph(int glyphId, TextStyle style);

    /// <summary>Measures a shaped run at a point size with exact positioned ink bounds.</summary>
    /// <param name="run">The immutable shaped run.</param>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The scaled advance vector and exact run ink bounds.</returns>
    /// <exception cref="System.ArgumentNullException"><c>run</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>pointSize</c> is invalid.</exception>
    /// <exception cref="FontFormatException">Required outline or metric data is unavailable.</exception>
    TextMeasurement MeasureRun(ShapedRun run, double pointSize);

    /// <summary>Measures a shaped run with point-based style adjustments.</summary>
    /// <param name="run">The immutable shaped run.</param>
    /// <param name="style">The point size and geometric style adjustments.</param>
    /// <returns>The adjusted advance vector and exact transformed run ink bounds.</returns>
    /// <exception cref="System.ArgumentNullException"><c>run</c> or <c>style</c> is null.</exception>
    /// <exception cref="FontFormatException">Required outline or metric data is unavailable.</exception>
    TextMeasurement MeasureStyledRun(ShapedRun run, TextStyle style);

    /// <summary>Gets one glyph's PostScript name or a stable synthetic name.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The parsed or synthetic glyph name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    string GetGlyphName(int glyphId);

    /// <summary>Gets one glyph's resolved metrics in design units.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The stable design-unit glyph metrics.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    /// <exception cref="FontFormatException">Required outline or metric tables are unavailable.</exception>
    GlyphMetrics GetGlyphMetrics(int glyphId);

    /// <summary>Gets recommended global metrics in design units.</summary>
    /// <returns>The stable recommended global metric result.</returns>
    /// <exception cref="FontFormatException">Required global metric tables are unavailable.</exception>
    FontDesignMetrics GetGlobalMetrics();
}
