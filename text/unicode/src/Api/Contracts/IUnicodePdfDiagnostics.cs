namespace unicode;

/// <summary>
/// Provides diagnostics for Unicode text and mappings used in PDF files.
/// </summary>
public interface IUnicodePdfDiagnostics
{
    /// <summary>
    /// Checks whether text is valid, accessibility-ready, and encodable as a PDF text string.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodePdfDiagnosticResult CheckTextString(string text);

    /// <summary>
    /// Checks whether text is valid, prepared, and encodable for a PDF accessibility context.
    /// </summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <param name="context">The PDF text context.</param>
    /// <returns>The combined accessibility-text diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>context</c> is invalid.</exception>
    UnicodePdfDiagnosticResult CheckAccessibilityText(string text, UnicodePdfTextContext context);

    /// <summary>
    /// Checks PDF/A or PDF/UA text preparation, text-string encoding, and ToUnicode requirements.
    /// </summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <param name="context">The PDF text context.</param>
    /// <param name="conformance">The conformance profile.</param>
    /// <param name="unicodeMapRequired">Whether the font text requires a ToUnicode map.</param>
    /// <param name="glyphMap">The glyph-map snapshot, or <c>null</c> when none is present.</param>
    /// <returns>The aggregated conformance diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when an enum value is invalid.</exception>
    UnicodePdfDiagnosticResult CheckPdfConformance(
        string text,
        UnicodePdfTextContext context,
        UnicodePdfConformance conformance,
        bool unicodeMapRequired,
        IUnicodePdfGlyphMap? glyphMap);

    /// <summary>
    /// Checks a supplied font glyph-map snapshot for missing or invalid ToUnicode mappings.
    /// </summary>
    /// <param name="glyphMap">The PDF glyph-map source to inspect, or <c>null</c> when no map is present.</param>
    /// <returns>An object containing the result described in the summary.</returns>
    UnicodePdfDiagnosticResult CheckFontUnicodeMapping(IUnicodePdfGlyphMap? glyphMap);
}
