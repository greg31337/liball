using System;

namespace pdf;

/// <summary>
/// Generates a linked list of figures or tables from publishing markers registered on a document.
/// </summary>
public class PdfGeneratedList : IPdfGeneratedList
{
    internal PdfPublishingMarkerKind Kind;
    internal PdfTextStyle TitleStyle = PdfTextStyle.Bold(18);
    internal PdfTextStyle EntryStyle = PdfTextStyle.Regular(11);
    internal double LineHeight = 16;
    internal double GapAfterTitle = 18;

    /// <summary>
    /// Creates a generator for one publishing marker kind.
    /// </summary>
    /// <param name="kind">The figure or table marker kind to include.</param>
    public PdfGeneratedList(PdfPublishingMarkerKind kind)
    {
        ValidateKind(kind);
        Kind = kind;
    }

    /// <summary>Inserts a letter-sized generated publishing-list page.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="pageIndex">The zero-based insertion position.</param>
    /// <returns>The inserted page.</returns>
    public PdfPage InsertPage(PdfDocument document, int pageIndex)
    {
        return InsertPageWithLayout(document, pageIndex, new PdfPageSizeFactory().Letter(), new PdfPageMargins(72, 72, 72, 72));
    }

    /// <summary>Inserts a generated publishing-list page with explicit page settings.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="pageIndex">The zero-based insertion position.</param>
    /// <param name="pageSize">The inserted page size.</param>
    /// <param name="margins">The inserted page margins.</param>
    /// <returns>The inserted page.</returns>
    public PdfPage InsertPageWithLayout(PdfDocument document, int pageIndex, PdfPageDimensions pageSize, PdfPageMargins margins)
    {
        PdfGeneratedIndex.ValidateInsert(document, pageSize, margins);
        PdfPage page = document.InsertPageWithDimensions(pageIndex, pageSize);
        page.SetMargins(margins.Left, margins.Top, margins.Right, margins.Bottom);
        Draw(document, page, page.GetContentLeft(), page.GetContentTop(), page.GetContentWidth());
        return page;
    }

    /// <summary>Draws linked entries for the selected publishing marker kind.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="page">The page receiving the generated list.</param>
    /// <param name="x">The left coordinate.</param>
    /// <param name="top">The top coordinate.</param>
    /// <param name="width">The available width.</param>
    public void Draw(PdfDocument document, PdfPage page, double x, double top, double width)
    {
        PdfTableOfContents validator = new PdfTableOfContents();
        validator.ValidateDraw(document, page, width);
        string title = "List of Figures";
        if (Kind == PdfPublishingMarkerKind.Table)
        {
            title = "List of Tables";
        }

        page.Canvas.DrawStyledText(title, x, top - TitleStyle.FontSize, TitleStyle);
        double y = top - TitleStyle.FontSize - GapAfterTitle;
        for (int pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
        {
            foreach (PdfPublishingMarker marker in document.PublishingMarkers)
            {
                if (marker.Kind == Kind && marker.Page == document.Pages[pageIndex])
                {
                    string pageLabel = document.GetPageLabel(pageIndex);
                    page.AddInternalLink(marker.DestinationName, x, y - 2, width, LineHeight);
                    page.Canvas.DrawStyledText(marker.Title, x, y, EntryStyle);
                    page.Canvas.DrawAlignedStyledText(pageLabel, x, y, width, EntryStyle, PdfTextAlignment.Right);
                    y -= LineHeight;
                }
            }
        }
    }

    /// <summary>Returns the figure or table marker kind selected by this generator.</summary>
    /// <returns>The selected publishing marker kind.</returns>
    public PdfPublishingMarkerKind GetMarkerKind()
    {
        return Kind;
    }

    internal static void ValidateKind(PdfPublishingMarkerKind kind)
    {
        if (kind == PdfPublishingMarkerKind.Figure || kind == PdfPublishingMarkerKind.Table)
        {
            return;
        }

        throw new ArgumentException("Publishing marker kind is not supported.", "kind");
    }
}
