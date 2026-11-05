namespace pdf;

/// <summary>
/// Creates page cursors for flowing layout elements through PDF documents.
/// </summary>
public class PdfPageCursorFactory : IPdfPageCursorFactory
{
    /// <summary>
    /// Creates a page-cursor factory.
    /// </summary>
    public PdfPageCursorFactory()
    {
    }

    /// <summary>
    /// Creates a cursor with letter-sized pages and no margins.
    /// </summary>
    /// <param name="document">The document that receives generated pages.</param>
    /// <returns>The initialized page cursor.</returns>
    public PdfPageCursor CreateDefault(PdfDocument document)
    {
        return PdfPageCursor.Default(document);
    }

    /// <summary>
    /// Creates a cursor with explicit page dimensions and margins.
    /// </summary>
    /// <param name="document">The document that receives generated pages.</param>
    /// <param name="pageSize">The dimensions of each generated page.</param>
    /// <param name="margins">The margins applied to each generated page.</param>
    /// <returns>The initialized page cursor.</returns>
    public PdfPageCursor Create(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins)
    {
        return PdfPageCursor.Create(document, pageSize, margins);
    }
}
