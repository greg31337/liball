using System;

namespace pdf;

/// <summary>
/// Collects content and page settings that are paginated independently within a layout document.
/// </summary>
public class PdfDocumentSection : IPdfDocumentSection
{
    internal PdfLayoutDocument Layout;

    internal PdfDocumentSection(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins)
    {
        if (pageSize == null)
        {
            throw new ArgumentNullException("pageSize");
        }

        if (margins == null)
        {
            throw new ArgumentNullException("margins");
        }

        ValidatePageGeometry(pageSize.Width, pageSize.Height, margins);
        PdfPageDimensions copiedSize = new PdfPageDimensions(pageSize.Width, pageSize.Height);
        PdfPageMargins copiedMargins = new PdfPageMargins(margins.Left, margins.Top, margins.Right, margins.Bottom);
        Layout = new PdfLayoutDocument(document, copiedSize, copiedMargins);
    }

    /// <summary>
    /// Adds a general layout element to the section content flow.
    /// </summary>
    /// <param name="item">The layout element to add.</param>
    public void AddItem(IPdfLayoutElement item)
    {
        Layout.AddItem(item);
    }

    /// <summary>
    /// Adds a block to the section content flow.
    /// </summary>
    /// <param name="block">The block to add.</param>
    public void AddBlock(PdfBlock block)
    {
        Layout.AddBlock(block);
    }

    /// <summary>
    /// Adds a text block to the section content flow.
    /// </summary>
    /// <param name="block">The text block to add.</param>
    public void AddTextBlock(PdfTextBlock block)
    {
        Layout.AddTextBlock(block);
    }

    /// <summary>
    /// Forces subsequent section content onto a new physical page.
    /// </summary>
    public void AddPageBreak()
    {
        Layout.AddPageBreak();
    }

    /// <summary>
    /// Selects the orientation applied to this section's page size.
    /// </summary>
    /// <param name="orientation">The page orientation.</param>
    /// <exception cref="ArgumentException">Thrown when the orientation is unsupported.</exception>
    public void SetOrientation(PdfPageOrientation orientation)
    {
        if (orientation != PdfPageOrientation.Portrait && orientation != PdfPageOrientation.Landscape)
        {
            throw new ArgumentException("Page orientation is not supported.", "orientation");
        }

        double width = Layout.PageSize.Width;
        double height = Layout.PageSize.Height;
        if ((orientation == PdfPageOrientation.Portrait && width > height) || (orientation == PdfPageOrientation.Landscape && width < height))
        {
            double swapped = width;
            width = height;
            height = swapped;
        }

        ValidatePageGeometry(width, height, Layout.Margins);
        ValidateColumnWidth(Layout.ColumnCount, Layout.ColumnGap, width);
        Layout.PageSize = new PdfPageDimensions(width, height);
    }

    /// <summary>
    /// Restarts the displayed page number at the supplied positive value.
    /// </summary>
    /// <param name="pageNumber">The first displayed page number in this section.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the page number is not positive.</exception>
    public void SetPageNumberStart(int pageNumber)
    {
        if (pageNumber <= 0)
        {
            throw new ArgumentOutOfRangeException("pageNumber", "The first section page number must be positive.");
        }

        Layout.HasPageNumberStart = true;
        Layout.PageNumberStart = pageNumber;
    }

    /// <summary>
    /// Configures top-to-bottom content flow through one or more equal-width columns.
    /// </summary>
    /// <param name="columnCount">The positive number of columns.</param>
    /// <param name="columnGap">The non-negative gap between columns in PDF points.</param>
    public void SetContentColumns(int columnCount, double columnGap)
    {
        ValidateColumnSettings(columnCount, columnGap);
        Layout.ColumnCount = columnCount;
        Layout.ColumnGap = columnGap;
    }

    /// <summary>
    /// Sets text used by the <c>{running}</c> page-template placeholder.
    /// </summary>
    /// <param name="text">The running-head text.</param>
    /// <exception cref="ArgumentNullException">Thrown when text is null.</exception>
    public void SetRunningHead(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Layout.RunningHead = text;
    }

    /// <summary>
    /// Sets reusable defaults applied to subsequently added typed section elements.
    /// </summary>
    /// <param name="theme">The document theme to copy.</param>
    public void SetTheme(PdfDocumentTheme theme)
    {
        Layout.SetTheme(theme);
    }

    /// <summary>
    /// Sets the default header template for this section.
    /// </summary>
    /// <param name="template">The header template.</param>
    public void SetHeaderTemplate(PdfPageTemplate template)
    {
        Layout.SetHeaderTemplate(template);
    }

    /// <summary>
    /// Sets the default footer template for this section.
    /// </summary>
    /// <param name="template">The footer template.</param>
    public void SetFooterTemplate(PdfPageTemplate template)
    {
        Layout.SetFooterTemplate(template);
    }

    /// <summary>
    /// Sets the first-page header template for this section.
    /// </summary>
    /// <param name="template">The first-page header template.</param>
    public void SetFirstPageHeaderTemplate(PdfPageTemplate template)
    {
        Layout.SetFirstPageHeaderTemplate(template);
    }

    /// <summary>
    /// Sets the first-page footer template for this section.
    /// </summary>
    /// <param name="template">The first-page footer template.</param>
    public void SetFirstPageFooterTemplate(PdfPageTemplate template)
    {
        Layout.SetFirstPageFooterTemplate(template);
    }

    /// <summary>
    /// Sets the odd-numbered-page header template for this section.
    /// </summary>
    /// <param name="template">The odd-page header template.</param>
    public void SetOddPageHeaderTemplate(PdfPageTemplate template)
    {
        Layout.SetOddPageHeaderTemplate(template);
    }

    /// <summary>
    /// Sets the odd-numbered-page footer template for this section.
    /// </summary>
    /// <param name="template">The odd-page footer template.</param>
    public void SetOddPageFooterTemplate(PdfPageTemplate template)
    {
        Layout.SetOddPageFooterTemplate(template);
    }

    /// <summary>
    /// Sets the even-numbered-page header template for this section.
    /// </summary>
    /// <param name="template">The even-page header template.</param>
    public void SetEvenPageHeaderTemplate(PdfPageTemplate template)
    {
        Layout.SetEvenPageHeaderTemplate(template);
    }

    /// <summary>
    /// Sets the even-numbered-page footer template for this section.
    /// </summary>
    /// <param name="template">The even-page footer template.</param>
    public void SetEvenPageFooterTemplate(PdfPageTemplate template)
    {
        Layout.SetEvenPageFooterTemplate(template);
    }

    internal PdfPageCursor RenderSection()
    {
        return Layout.Render();
    }

    internal void ValidateColumnSettings(int columnCount, double columnGap)
    {
        if (columnCount <= 0)
        {
            throw new ArgumentOutOfRangeException("columnCount", "Section column count must be positive.");
        }

        if (double.IsNaN(columnGap) || double.IsInfinity(columnGap) || columnGap < 0)
        {
            throw new ArgumentOutOfRangeException("columnGap", "Section column gap must be non-negative and finite.");
        }

        ValidateColumnWidth(columnCount, columnGap, Layout.PageSize.Width);
    }

    internal void ValidateColumnWidth(int columnCount, double columnGap, double pageWidth)
    {
        double contentWidth = pageWidth - Layout.Margins.Left - Layout.Margins.Right;
        if (columnGap * (columnCount - 1) >= contentWidth)
        {
            throw new ArgumentException("Section columns and gaps must leave a positive width for every column.", "columnGap");
        }
    }

    internal static void ValidatePageGeometry(double width, double height, PdfPageMargins margins)
    {
        if (double.IsNaN(width) || double.IsInfinity(width) || double.IsNaN(height) || double.IsInfinity(height) || width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException("pageSize", "Section page dimensions must be positive and finite.");
        }

        if (double.IsNaN(margins.Left) || double.IsInfinity(margins.Left) || double.IsNaN(margins.Top) || double.IsInfinity(margins.Top) || double.IsNaN(margins.Right) || double.IsInfinity(margins.Right) || double.IsNaN(margins.Bottom) || double.IsInfinity(margins.Bottom) || margins.Left < 0 || margins.Top < 0 || margins.Right < 0 || margins.Bottom < 0)
        {
            throw new ArgumentOutOfRangeException("margins", "Section margins must be non-negative and finite.");
        }

        if (margins.Left + margins.Right >= width || margins.Top + margins.Bottom >= height)
        {
            throw new ArgumentException("Section margins must leave positive page content dimensions.", "margins");
        }
    }
}
