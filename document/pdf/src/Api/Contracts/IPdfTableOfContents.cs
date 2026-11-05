using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfTableOfContents"/>.
/// </summary>
public interface IPdfTableOfContents
{
    /// <summary>
    /// Inserts a page at the requested position and returns the new value.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="pageIndex">The page index to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    PdfPage InsertPage(PdfDocument document, int pageIndex);

    /// <summary>
    /// Inserts a page at the requested position and returns the new value.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="pageIndex">The page index to use for this operation.</param>
    /// <param name="pageSize">The page size to use for this operation.</param>
    /// <param name="margins">The margins to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfPage InsertPageWithLayout(PdfDocument document, int pageIndex, PdfPageDimensions pageSize, PdfPageMargins margins);

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="top">The top in PDF points.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    void Draw(PdfDocument document, PdfPage page, double x, double top, double width);
}
