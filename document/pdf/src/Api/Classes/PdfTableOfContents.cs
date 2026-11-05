using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

/// <summary>
/// Collects table-of-contents entries and emits linked layout content for document navigation.
/// </summary>
public class PdfTableOfContents : IPdfTableOfContents
{
    /// <summary>
    /// Stores the title used by this object.
    /// </summary>
    internal string Title;
    /// <summary>
    /// Stores the title style used by this object.
    /// </summary>
    internal PdfTextStyle TitleStyle;
    /// <summary>
    /// Stores the entry style used by this object.
    /// </summary>
    internal PdfTextStyle EntryStyle;
    /// <summary>
    /// Specifies the line height in PDF points.
    /// </summary>
    internal double LineHeight;
    /// <summary>
    /// Specifies the indent width in PDF points.
    /// </summary>
    internal double IndentWidth;
    /// <summary>
    /// Stores the numeric gap after title used by this object.
    /// </summary>
    internal double GapAfterTitle;

    /// <summary>
    /// Initializes a new table of contents.
    /// </summary>
    public PdfTableOfContents()
    {
        Title = "Contents";
        TitleStyle = PdfTextStyle.Bold(18);
        EntryStyle = PdfTextStyle.Regular(11);
        LineHeight = 16;
        IndentWidth = 18;
        GapAfterTitle = 18;
    }

    /// <summary>
    /// Inserts a page at the requested position and returns the new value.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="pageIndex">The page index to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfPage InsertPage(PdfDocument document, int pageIndex)
    {
        return InsertPageWithLayout(document, pageIndex, new PdfPageSizeFactory().Letter(), new PdfPageMargins(72, 72, 72, 72));
    }

    /// <summary>
    /// Inserts a page at the requested position and returns the new value.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="pageIndex">The page index to use for this operation.</param>
    /// <param name="pageSize">The page size to use for this operation.</param>
    /// <param name="margins">The margins to use for this operation.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfPage InsertPageWithLayout(PdfDocument document, int pageIndex, PdfPageDimensions pageSize, PdfPageMargins margins)
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

        PdfPage page = document.InsertPageWithDimensions(pageIndex, pageSize);
        page.SetMargins(margins.Left, margins.Top, margins.Right, margins.Bottom);
        Draw(document, page, page.GetContentLeft(), page.GetContentTop(), page.GetContentWidth());
        return page;
    }

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="top">The top in PDF points.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    public void Draw(PdfDocument document, PdfPage page, double x, double top, double width)
    {
        ValidateDraw(document, page, width);

        page.Canvas.DrawStyledText(Title, x, top - TitleStyle.FontSize, TitleStyle);
        double y = top - TitleStyle.FontSize - GapAfterTitle;
        DrawEntries(document, page, document.Outlines, 0, x, y, width);
    }

    internal double DrawEntries(PdfDocument document, PdfPage page, IReadOnlyList<PdfOutlineItem> items, int level, double x, double y, double width)
    {
        for (int index = 0; index < items.Count; index++)
        {
            PdfOutlineItem item = items[index];
            int pageNumber = GetPageNumber(document, item.Page);
            string pageNumberText = pageNumber.ToString(CultureInfo.InvariantCulture);
            double indent = level * IndentWidth;
            double textX = x + indent;
            string destinationName = CreateDestinationName(document);

            document.AddNamedDestination(destinationName, item.Page, item.X, item.Y);
            page.AddInternalLink(destinationName, x, y - 2, width, LineHeight);
            page.Canvas.DrawStyledText(item.Title, textX, y, EntryStyle);
            page.Canvas.DrawAlignedStyledText(pageNumberText, x, y, width, EntryStyle, PdfTextAlignment.Right);

            y -= LineHeight;
            if (item.Children.Count > 0)
            {
                y = DrawEntries(document, page, item.Children, level + 1, x, y, width);
            }
        }

        return y;
    }

    internal string CreateDestinationName(PdfDocument document)
    {
        int index = 1;
        while (true)
        {
            string name = "toc-" + index.ToString(CultureInfo.InvariantCulture);
            if (!HasNamedDestination(document, name))
            {
                return name;
            }

            index++;
        }
    }

    internal static bool HasNamedDestination(PdfDocument document, string name)
    {
        foreach (PdfNamedDestination destination in document.NamedDestinations)
        {
            if (destination.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    internal static int GetPageNumber(PdfDocument document, PdfPage page)
    {
        for (int index = 0; index < document.Pages.Count; index++)
        {
            if (document.Pages[index] == page)
            {
                return index + 1;
            }
        }

        throw new InvalidOperationException("Table of contents entry page does not belong to the document.");
    }

    internal void ValidateDraw(PdfDocument document, PdfPage page, double width)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        if (!ContainsPage(document, page))
        {
            throw new ArgumentException("Table of contents page must belong to the document.", "page");
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException("width", "Table of contents width must be positive.");
        }
    }

    internal static bool ContainsPage(PdfDocument document, PdfPage page)
    {
        foreach (PdfPage existingPage in document.Pages)
        {
            if (existingPage == page)
            {
                return true;
            }
        }

        return false;
    }
}
