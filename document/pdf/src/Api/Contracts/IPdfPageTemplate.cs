using System;
using System.Globalization;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfPageTemplate"/>.
/// </summary>
public interface IPdfPageTemplate
{
    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    PdfLayoutResult Draw(PdfPage page, double x, double y, double width);

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="totalPages">The total number of pages in the document.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfLayoutResult DrawForPage(PdfPage page, double x, double y, double width, int pageNumber, int totalPages);

    /// <summary>
    /// Determines whether final template pass is required for this operation.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    bool NeedsFinalTemplatePass();
}
