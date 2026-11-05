namespace pdf;

/// <summary>
/// Creates page cursors for flowing layout elements through PDF documents.
/// </summary>
public interface IPdfPageCursorFactory
{
    /// <summary>
    /// Creates a cursor with letter-sized pages and no margins.
    /// </summary>
    /// <param name="document">The document that receives generated pages.</param>
    /// <returns>The initialized page cursor.</returns>
    PdfPageCursor CreateDefault(PdfDocument document);

    /// <summary>
    /// Creates a cursor with explicit page dimensions and margins.
    /// </summary>
    /// <param name="document">The document that receives generated pages.</param>
    /// <param name="pageSize">The dimensions of each generated page.</param>
    /// <param name="margins">The margins applied to each generated page.</param>
    /// <returns>The initialized page cursor.</returns>
    PdfPageCursor Create(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins);
}
