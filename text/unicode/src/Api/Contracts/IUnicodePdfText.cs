namespace unicode;

/// <summary>
/// Provides Unicode text preparation, layout, and serialization helpers for PDF generation.
/// </summary>
public interface IUnicodePdfText
{
    /// <summary>
    /// Replaces malformed UTF-16 and normalizes text to NFC for stable PDF extraction.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string PrepareActualText(string text);

    /// <summary>
    /// Replaces malformed UTF-16 and normalizes text to NFC for a PDF accessibility context.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <param name="context">The PDF text context.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>context</c> is invalid.</exception>
    string PrepareAccessibilityText(string text, UnicodePdfTextContext context);

    /// <summary>Prepares alternative text for a PDF structure element.</summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string PrepareAltText(string text);

    /// <summary>Prepares text for a PDF bookmark.</summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string PrepareBookmarkText(string text);

    /// <summary>Prepares text for a PDF outline item.</summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string PrepareOutlineText(string text);

    /// <summary>Prepares text associated with a PDF structure element.</summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string PrepareStructureElementText(string text);

    /// <summary>Prepares PDF document metadata text.</summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The prepared .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    string PrepareMetadataText(string text);

    /// <summary>Reports whether text is accessibility-ready for the requested PDF context.</summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <param name="context">The PDF text context.</param>
    /// <returns><c>true</c> when the text is valid, permitted, and NFC-normalized.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>context</c> is invalid.</exception>
    bool IsAccessibilityTextPrepared(string text, UnicodePdfTextContext context);

    /// <summary>Reports whether text contains only accessibility-safe Unicode scalars.</summary>
    /// <param name="text">The UTF-16 text to inspect.</param>
    /// <param name="context">The PDF text context.</param>
    /// <returns><c>true</c> when all input is well formed and no scalar is prohibited or U+FFFD.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>context</c> is invalid.</exception>
    bool ContainsOnlyValidAccessibilityScalars(string text, UnicodePdfTextContext context);

    /// <summary>
    /// Reports whether text is already in the normalized form used for PDF ActualText.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    bool IsActualTextPrepared(string text);

    /// <summary>
    /// Reports whether text contains only valid Unicode scalars allowed in PDF ActualText.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    bool ContainsOnlyValidActualTextScalars(string text);

    /// <summary>
    /// Returns extended grapheme-cluster ranges that PDF layout should keep intact.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodeSegmentRecord[] GetLayoutClusters(string text);

    /// <summary>
    /// Groups adjacent grapheme-safe text into resolved script runs for PDF font selection.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodePdfScriptRun[] GetScriptRuns(string text);

    /// <summary>
    /// Groups adjacent scalars into simple left-to-right and right-to-left PDF layout runs.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing the resulting values.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodePdfBidiRun[] GetBidiRuns(string text);

    /// <summary>
    /// Returns consolidated Unicode metadata for PDF text layout.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>The grapheme, word, line, script, bidi, bracket, mirror, and width metadata.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodePdfLayoutInfo GetLayoutInfo(string text);

    /// <summary>
    /// Returns scalar-level script, bidi, bracket, mirror, and width metadata.
    /// </summary>
    /// <param name="text">The UTF-16 text to process.</param>
    /// <returns>A new array containing one record per decoded scalar.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>text</c> is <c>null</c>.</exception>
    UnicodePdfScalarLayout[] GetScalarLayout(string text);

    /// <summary>
    /// Classifies one Unicode scalar as zero-width, narrow, or wide for PDF layout heuristics.
    /// </summary>
    /// <param name="codePoint">The Unicode scalar to inspect.</param>
    /// <returns>The scalar width classification.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>codePoint</c> is not a Unicode scalar.</exception>
    UnicodePdfTextWidth GetTextWidthClass(int codePoint);

    /// <summary>
    /// Escapes bytes for a PDF literal string, including delimiters, control bytes, and non-printable bytes.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    string EscapeLiteralString(byte[] bytes);

    /// <summary>
    /// Escapes PDF literal-string content into a caller-provided ASCII byte destination without partial writes.
    /// </summary>
    /// <param name="bytes">The bytes to escape.</param>
    /// <param name="destination">The destination for escaped ASCII bytes.</param>
    /// <returns>The write result, including the required byte length.</returns>
    UnicodeBufferResult EscapeLiteralStringBuffer(
        System.ReadOnlySpan<byte> bytes,
        System.Span<byte> destination);

    /// <summary>
    /// Formats bytes as uppercase PDF hexadecimal-string content.
    /// </summary>
    /// <param name="bytes">The encoded bytes to process.</param>
    /// <returns>The resulting .NET UTF-16 string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>bytes</c> is <c>null</c>.</exception>
    string ToHexString(byte[] bytes);

    /// <summary>
    /// Formats PDF hexadecimal-string content into a caller-provided ASCII byte destination without partial writes.
    /// </summary>
    /// <param name="bytes">The bytes to format.</param>
    /// <param name="destination">The destination for uppercase hexadecimal ASCII bytes.</param>
    /// <returns>The write result, including the required byte length.</returns>
    UnicodeBufferResult ToHexStringBuffer(
        System.ReadOnlySpan<byte> bytes,
        System.Span<byte> destination);
}
