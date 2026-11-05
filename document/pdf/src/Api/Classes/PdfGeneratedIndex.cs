using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Generates a linked alphabetical index from markers registered on a document.
/// </summary>
public class PdfGeneratedIndex : IPdfGeneratedIndex
{
    internal PdfTextStyle TitleStyle = PdfTextStyle.Bold(18);
    internal PdfTextStyle EntryStyle = PdfTextStyle.Regular(11);
    internal double LineHeight = 16;
    internal double GapAfterTitle = 18;

    /// <summary>
    /// Creates an index generator with standard title and entry styles.
    /// </summary>
    public PdfGeneratedIndex()
    {
    }

    /// <summary>Inserts a letter-sized generated index page.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="pageIndex">The zero-based insertion position.</param>
    /// <returns>The inserted page.</returns>
    public PdfPage InsertPage(PdfDocument document, int pageIndex)
    {
        return InsertPageWithLayout(document, pageIndex, new PdfPageSizeFactory().Letter(), new PdfPageMargins(72, 72, 72, 72));
    }

    /// <summary>Inserts a generated index page with explicit page settings.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="pageIndex">The zero-based insertion position.</param>
    /// <param name="pageSize">The inserted page size.</param>
    /// <param name="margins">The inserted page margins.</param>
    /// <returns>The inserted page.</returns>
    public PdfPage InsertPageWithLayout(PdfDocument document, int pageIndex, PdfPageDimensions pageSize, PdfPageMargins margins)
    {
        ValidateInsert(document, pageSize, margins);
        PdfPage page = document.InsertPageWithDimensions(pageIndex, pageSize);
        page.SetMargins(margins.Left, margins.Top, margins.Right, margins.Bottom);
        Draw(document, page, page.GetContentLeft(), page.GetContentTop(), page.GetContentWidth());
        return page;
    }

    /// <summary>Draws linked entries for the document's index markers.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="page">The page receiving the index.</param>
    /// <param name="x">The left coordinate.</param>
    /// <param name="top">The top coordinate.</param>
    /// <param name="width">The available width.</param>
    public void Draw(PdfDocument document, PdfPage page, double x, double top, double width)
    {
        PdfTableOfContents validator = new PdfTableOfContents();
        validator.ValidateDraw(document, page, width);
        page.Canvas.DrawStyledText("Index", x, top - TitleStyle.FontSize, TitleStyle);
        double y = top - TitleStyle.FontSize - GapAfterTitle;
        List<PdfIndexMarker> markers = SortMarkers(document.IndexMarkers);

        foreach (PdfIndexMarker marker in markers)
        {
            string entryText = marker.Term;
            if (marker.Subterm.Length > 0)
            {
                entryText += " — " + marker.Subterm;
            }

            int pageIndex = document.GetPageIndex(marker.Page);
            string pageLabel = document.GetPageLabel(pageIndex);
            page.AddInternalLink(marker.DestinationName, x, y - 2, width, LineHeight);
            page.Canvas.DrawStyledText(entryText, x, y, EntryStyle);
            page.Canvas.DrawAlignedStyledText(pageLabel, x, y, width, EntryStyle, PdfTextAlignment.Right);
            y -= LineHeight;
        }
    }

    internal static void ValidateInsert(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (pageSize == null)
        {
            throw new ArgumentNullException("pageSize");
        }

        if (margins == null)
        {
            throw new ArgumentNullException("margins");
        }
    }

    internal static List<PdfIndexMarker> SortMarkers(IReadOnlyList<PdfIndexMarker> source)
    {
        List<PdfIndexMarker> sorted = new List<PdfIndexMarker>();
        foreach (PdfIndexMarker marker in source)
        {
            int index = 0;
            while (index < sorted.Count && CompareMarkers(sorted[index], marker) <= 0)
            {
                index++;
            }

            sorted.Insert(index, marker);
        }

        return sorted;
    }

    internal static int CompareMarkers(PdfIndexMarker first, PdfIndexMarker second)
    {
        int primary = string.Compare(first.Term, second.Term, StringComparison.OrdinalIgnoreCase);
        if (primary != 0)
        {
            return primary;
        }

        int secondary = string.Compare(first.Subterm, second.Subterm, StringComparison.OrdinalIgnoreCase);
        if (secondary != 0)
        {
            return secondary;
        }

        return string.CompareOrdinal(first.DestinationName, second.DestinationName);
    }
}
