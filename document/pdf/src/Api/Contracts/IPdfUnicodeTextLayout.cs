using System;

namespace pdf;

/// <summary>
/// Defines Unicode measurement, line layout, bidirectional ordering, and drawing operations.
/// </summary>
public interface IPdfUnicodeTextLayout
{
    /// <summary>
    /// Lays out one normal Unicode text span.
    /// </summary>
    /// <param name="text">The logical Unicode text.</param>
    /// <param name="fonts">The ordered fallback-font chain.</param>
    /// <param name="options">The sizing and direction options.</param>
    /// <returns>The laid-out lines and aggregate size.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    PdfUnicodeTextResult Layout(string text, PdfFontFallbackChain fonts, PdfUnicodeTextOptions options);

    /// <summary>
    /// Lays out Unicode spans with script, footnote, and optional hyphenation metadata.
    /// </summary>
    /// <param name="spans">The logical spans to lay out in order.</param>
    /// <param name="fonts">The ordered fallback-font chain.</param>
    /// <param name="options">The sizing and direction options.</param>
    /// <returns>The laid-out lines and aggregate size.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    PdfUnicodeTextResult LayoutSpans(PdfUnicodeTextSpan[] spans, PdfFontFallbackChain fonts, PdfUnicodeTextOptions options);

    /// <summary>
    /// Draws a completed Unicode layout with its first line on the supplied baseline.
    /// </summary>
    /// <param name="canvas">The target PDF canvas.</param>
    /// <param name="result">The completed Unicode layout.</param>
    /// <param name="x">The horizontal coordinate of the visual line start.</param>
    /// <param name="baseline">The baseline of the first line.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    void Draw(PdfCanvas canvas, PdfUnicodeTextResult result, double x, double baseline);
}
