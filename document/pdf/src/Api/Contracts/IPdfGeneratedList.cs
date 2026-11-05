namespace pdf;

/// <summary>
/// Defines generation of a linked list of figures or tables.
/// </summary>
public interface IPdfGeneratedList
{
    /// <summary>Inserts a letter-sized generated publishing-list page.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="pageIndex">The zero-based insertion position.</param>
    /// <returns>The inserted page.</returns>
    PdfPage InsertPage(PdfDocument document, int pageIndex);

    /// <summary>Inserts a generated publishing-list page with explicit page settings.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="pageIndex">The zero-based insertion position.</param>
    /// <param name="pageSize">The inserted page size.</param>
    /// <param name="margins">The inserted page margins.</param>
    /// <returns>The inserted page.</returns>
    PdfPage InsertPageWithLayout(PdfDocument document, int pageIndex, PdfPageDimensions pageSize, PdfPageMargins margins);

    /// <summary>Draws linked entries for the selected publishing marker kind.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="page">The page receiving the generated list.</param>
    /// <param name="x">The left coordinate.</param>
    /// <param name="top">The top coordinate.</param>
    /// <param name="width">The available width.</param>
    void Draw(PdfDocument document, PdfPage page, double x, double top, double width);

    /// <summary>Returns the figure or table marker kind selected by this generator.</summary>
    /// <returns>The selected publishing marker kind.</returns>
    PdfPublishingMarkerKind GetMarkerKind();
}
