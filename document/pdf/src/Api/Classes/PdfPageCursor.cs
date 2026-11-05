using System;

namespace pdf;

/// <summary>
/// Tracks the active page and remaining content area while flowing layout items across pages.
/// </summary>
public class PdfPageCursor : IPdfPageCursor
{
    /// <summary>
    /// Stores the document used by this object.
    /// </summary>
    internal PdfDocument Document = new PdfDocument();
    /// <summary>
    /// Stores the page size used by this object.
    /// </summary>
    internal PdfPageDimensions PageSize = new PdfPageSizeFactory().Letter();
    /// <summary>
    /// Stores the margins used by this object.
    /// </summary>
    internal PdfPageMargins Margins = new PdfPageMargins(0, 0, 0, 0);
    /// <summary>
    /// Stores the current page used by this object.
    /// </summary>
    internal PdfPage CurrentPage = new PdfPage(PdfPageSize.LetterWidth, PdfPageSize.LetterHeight);
    /// <summary>
    /// Specifies the current y in PDF points.
    /// </summary>
    internal double CurrentY;
    /// <summary>
    /// Indicates whether this object has content on page.
    /// </summary>
    internal bool HasContentOnPage;
    /// <summary>
    /// Indicates whether this object has header template.
    /// </summary>
    internal bool HasHeaderTemplate;
    /// <summary>
    /// Stores the header template used by this object.
    /// </summary>
    internal PdfPageTemplate HeaderTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
    /// <summary>
    /// Indicates whether this object has footer template.
    /// </summary>
    internal bool HasFooterTemplate;
    /// <summary>
    /// Stores the footer template used by this object.
    /// </summary>
    internal PdfPageTemplate FooterTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
    /// <summary>
    /// Indicates whether this object has first page header template.
    /// </summary>
    internal bool HasFirstPageHeaderTemplate;
    /// <summary>
    /// Stores the first page header template used by this object.
    /// </summary>
    internal PdfPageTemplate FirstPageHeaderTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
    /// <summary>
    /// Indicates whether this object has first page footer template.
    /// </summary>
    internal bool HasFirstPageFooterTemplate;
    /// <summary>
    /// Stores the first page footer template used by this object.
    /// </summary>
    internal PdfPageTemplate FirstPageFooterTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
    /// <summary>
    /// Indicates whether this object has odd page header template.
    /// </summary>
    internal bool HasOddPageHeaderTemplate;
    /// <summary>
    /// Stores the odd page header template used by this object.
    /// </summary>
    internal PdfPageTemplate OddPageHeaderTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
    /// <summary>
    /// Indicates whether this object has odd page footer template.
    /// </summary>
    internal bool HasOddPageFooterTemplate;
    /// <summary>
    /// Stores the odd page footer template used by this object.
    /// </summary>
    internal PdfPageTemplate OddPageFooterTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
    /// <summary>
    /// Indicates whether this object has even page header template.
    /// </summary>
    internal bool HasEvenPageHeaderTemplate;
    /// <summary>
    /// Stores the even page header template used by this object.
    /// </summary>
    internal PdfPageTemplate EvenPageHeaderTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
    /// <summary>
    /// Indicates whether this object has even page footer template.
    /// </summary>
    internal bool HasEvenPageFooterTemplate;
    /// <summary>
    /// Stores the even page footer template used by this object.
    /// </summary>
    internal PdfPageTemplate EvenPageFooterTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
    /// <summary>
    /// Indicates whether debug overlay enabled is enabled.
    /// </summary>
    internal bool DebugOverlayEnabled;
    /// <summary>
    /// Stores the debug overlay color used by this object.
    /// </summary>
    internal PdfColor DebugOverlayColor = PdfColor.Rgb(1, 0, 0);
    /// <summary>
    /// Specifies the debug overlay line width in PDF points.
    /// </summary>
    internal double DebugOverlayLineWidth;
    internal int FirstPageIndex;
    internal int PageNumberStart;
    internal int ColumnCount;
    internal double ColumnGap;
    internal int CurrentColumnIndex;
    internal string RunningHead = string.Empty;

    internal PdfPageCursor(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins, bool hasHeaderTemplate, PdfPageTemplate headerTemplate, bool hasFooterTemplate, PdfPageTemplate footerTemplate, bool hasFirstPageHeaderTemplate, PdfPageTemplate firstPageHeaderTemplate, bool hasFirstPageFooterTemplate, PdfPageTemplate firstPageFooterTemplate, bool hasOddPageHeaderTemplate, PdfPageTemplate oddPageHeaderTemplate, bool hasOddPageFooterTemplate, PdfPageTemplate oddPageFooterTemplate, bool hasEvenPageHeaderTemplate, PdfPageTemplate evenPageHeaderTemplate, bool hasEvenPageFooterTemplate, PdfPageTemplate evenPageFooterTemplate)
    {
        Initialize(
            document,
            pageSize,
            margins,
            hasHeaderTemplate,
            headerTemplate,
            hasFooterTemplate,
            footerTemplate,
            hasFirstPageHeaderTemplate,
            firstPageHeaderTemplate,
            hasFirstPageFooterTemplate,
            firstPageFooterTemplate,
            hasOddPageHeaderTemplate,
            oddPageHeaderTemplate,
            hasOddPageFooterTemplate,
            oddPageFooterTemplate,
            hasEvenPageHeaderTemplate,
            evenPageHeaderTemplate,
            hasEvenPageFooterTemplate,
            evenPageFooterTemplate);
    }

    /// <summary>
    /// Creates a page cursor with letter-sized pages and no margins.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <returns>The resulting page cursor.</returns>
    internal static PdfPageCursor Default(PdfDocument document)
    {
        return Create(document, new PdfPageSizeFactory().Letter(), new PdfPageMargins(0, 0, 0, 0));
    }

    /// <summary>
    /// Creates a page cursor with letter-sized pages and no margins.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <returns>The resulting page cursor.</returns>
    internal static PdfPageCursor Create(PdfDocument document)
    {
        return Default(document);
    }

    /// <summary>
    /// Creates a page cursor with explicit page settings.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="pageSize">The page size to use for this operation.</param>
    /// <param name="margins">The margins to use for this operation.</param>
    /// <returns>The resulting page cursor.</returns>
    internal static PdfPageCursor Create(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins)
    {
        PdfPageTemplate emptyTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        PdfPageCursor cursor = new PdfPageCursor(document, pageSize, margins, false, emptyTemplate, false, emptyTemplate, false, emptyTemplate, false, emptyTemplate, false, emptyTemplate, false, emptyTemplate, false, emptyTemplate, false, emptyTemplate);
        cursor.StartFlow();
        return cursor;
    }

    /// <summary>
    /// Creates a page cursor with header and footer templates.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="pageSize">The page size to use for this operation.</param>
    /// <param name="margins">The margins to use for this operation.</param>
    /// <param name="hasHeaderTemplate">A value indicating whether has header template.</param>
    /// <param name="headerTemplate">The header template to use for this operation.</param>
    /// <param name="hasFooterTemplate">A value indicating whether has footer template.</param>
    /// <param name="footerTemplate">The footer template to use for this operation.</param>
    /// <returns>The resulting page cursor.</returns>
    internal static PdfPageCursor WithPageTemplates(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins, bool hasHeaderTemplate, PdfPageTemplate headerTemplate, bool hasFooterTemplate, PdfPageTemplate footerTemplate)
    {
        PdfPageTemplate emptyTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        PdfPageCursor cursor = new PdfPageCursor(document, pageSize, margins, hasHeaderTemplate, headerTemplate, hasFooterTemplate, footerTemplate, false, emptyTemplate, false, emptyTemplate, false, emptyTemplate, false, emptyTemplate, false, emptyTemplate, false, emptyTemplate);
        cursor.StartFlow();
        return cursor;
    }

    /// <summary>
    /// Creates a page cursor with header and footer templates.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="pageSize">The page size to use for this operation.</param>
    /// <param name="margins">The margins to use for this operation.</param>
    /// <param name="hasHeaderTemplate">A value indicating whether has header template.</param>
    /// <param name="headerTemplate">The header template to use for this operation.</param>
    /// <param name="hasFooterTemplate">A value indicating whether has footer template.</param>
    /// <param name="footerTemplate">The footer template to use for this operation.</param>
    /// <returns>The resulting page cursor.</returns>
    internal static PdfPageCursor Create(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins, bool hasHeaderTemplate, PdfPageTemplate headerTemplate, bool hasFooterTemplate, PdfPageTemplate footerTemplate)
    {
        return WithPageTemplates(document, pageSize, margins, hasHeaderTemplate, headerTemplate, hasFooterTemplate, footerTemplate);
    }

    /// <summary>
    /// Creates a page cursor with page-specific header and footer templates.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="pageSize">The page size to use for this operation.</param>
    /// <param name="margins">The margins to use for this operation.</param>
    /// <param name="hasHeaderTemplate">A value indicating whether has header template.</param>
    /// <param name="headerTemplate">The header template to use for this operation.</param>
    /// <param name="hasFooterTemplate">A value indicating whether has footer template.</param>
    /// <param name="footerTemplate">The footer template to use for this operation.</param>
    /// <param name="hasFirstPageHeaderTemplate">A value indicating whether has first page header template.</param>
    /// <param name="firstPageHeaderTemplate">The first page header template to use for this operation.</param>
    /// <param name="hasFirstPageFooterTemplate">A value indicating whether has first page footer template.</param>
    /// <param name="firstPageFooterTemplate">The first page footer template to use for this operation.</param>
    /// <param name="hasOddPageHeaderTemplate">A value indicating whether has odd page header template.</param>
    /// <param name="oddPageHeaderTemplate">The odd page header template to use for this operation.</param>
    /// <param name="hasOddPageFooterTemplate">A value indicating whether has odd page footer template.</param>
    /// <param name="oddPageFooterTemplate">The odd page footer template to use for this operation.</param>
    /// <param name="hasEvenPageHeaderTemplate">A value indicating whether has even page header template.</param>
    /// <param name="evenPageHeaderTemplate">The even page header template to use for this operation.</param>
    /// <param name="hasEvenPageFooterTemplate">A value indicating whether has even page footer template.</param>
    /// <param name="evenPageFooterTemplate">The even page footer template to use for this operation.</param>
    /// <returns>The resulting page cursor.</returns>
    internal static PdfPageCursor WithTemplates(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins, bool hasHeaderTemplate, PdfPageTemplate headerTemplate, bool hasFooterTemplate, PdfPageTemplate footerTemplate, bool hasFirstPageHeaderTemplate, PdfPageTemplate firstPageHeaderTemplate, bool hasFirstPageFooterTemplate, PdfPageTemplate firstPageFooterTemplate, bool hasOddPageHeaderTemplate, PdfPageTemplate oddPageHeaderTemplate, bool hasOddPageFooterTemplate, PdfPageTemplate oddPageFooterTemplate, bool hasEvenPageHeaderTemplate, PdfPageTemplate evenPageHeaderTemplate, bool hasEvenPageFooterTemplate, PdfPageTemplate evenPageFooterTemplate)
    {
        PdfPageCursor cursor = PrepareWithTemplates(document, pageSize, margins, hasHeaderTemplate, headerTemplate, hasFooterTemplate, footerTemplate, hasFirstPageHeaderTemplate, firstPageHeaderTemplate, hasFirstPageFooterTemplate, firstPageFooterTemplate, hasOddPageHeaderTemplate, oddPageHeaderTemplate, hasOddPageFooterTemplate, oddPageFooterTemplate, hasEvenPageHeaderTemplate, evenPageHeaderTemplate, hasEvenPageFooterTemplate, evenPageFooterTemplate);
        cursor.StartFlow();
        return cursor;
    }

    internal static PdfPageCursor PrepareWithTemplates(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins, bool hasHeaderTemplate, PdfPageTemplate headerTemplate, bool hasFooterTemplate, PdfPageTemplate footerTemplate, bool hasFirstPageHeaderTemplate, PdfPageTemplate firstPageHeaderTemplate, bool hasFirstPageFooterTemplate, PdfPageTemplate firstPageFooterTemplate, bool hasOddPageHeaderTemplate, PdfPageTemplate oddPageHeaderTemplate, bool hasOddPageFooterTemplate, PdfPageTemplate oddPageFooterTemplate, bool hasEvenPageHeaderTemplate, PdfPageTemplate evenPageHeaderTemplate, bool hasEvenPageFooterTemplate, PdfPageTemplate evenPageFooterTemplate)
    {
        return new PdfPageCursor(document, pageSize, margins, hasHeaderTemplate, headerTemplate, hasFooterTemplate, footerTemplate, hasFirstPageHeaderTemplate, firstPageHeaderTemplate, hasFirstPageFooterTemplate, firstPageFooterTemplate, hasOddPageHeaderTemplate, oddPageHeaderTemplate, hasOddPageFooterTemplate, oddPageFooterTemplate, hasEvenPageHeaderTemplate, evenPageHeaderTemplate, hasEvenPageFooterTemplate, evenPageFooterTemplate);
    }

    /// <summary>
    /// Creates a page cursor with page-specific header and footer templates.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="pageSize">The page size to use for this operation.</param>
    /// <param name="margins">The margins to use for this operation.</param>
    /// <param name="hasHeaderTemplate">A value indicating whether has header template.</param>
    /// <param name="headerTemplate">The header template to use for this operation.</param>
    /// <param name="hasFooterTemplate">A value indicating whether has footer template.</param>
    /// <param name="footerTemplate">The footer template to use for this operation.</param>
    /// <param name="hasFirstPageHeaderTemplate">A value indicating whether has first page header template.</param>
    /// <param name="firstPageHeaderTemplate">The first page header template to use for this operation.</param>
    /// <param name="hasFirstPageFooterTemplate">A value indicating whether has first page footer template.</param>
    /// <param name="firstPageFooterTemplate">The first page footer template to use for this operation.</param>
    /// <param name="hasOddPageHeaderTemplate">A value indicating whether has odd page header template.</param>
    /// <param name="oddPageHeaderTemplate">The odd page header template to use for this operation.</param>
    /// <param name="hasOddPageFooterTemplate">A value indicating whether has odd page footer template.</param>
    /// <param name="oddPageFooterTemplate">The odd page footer template to use for this operation.</param>
    /// <param name="hasEvenPageHeaderTemplate">A value indicating whether has even page header template.</param>
    /// <param name="evenPageHeaderTemplate">The even page header template to use for this operation.</param>
    /// <param name="hasEvenPageFooterTemplate">A value indicating whether has even page footer template.</param>
    /// <param name="evenPageFooterTemplate">The even page footer template to use for this operation.</param>
    /// <returns>The resulting page cursor.</returns>
    internal static PdfPageCursor Create(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins, bool hasHeaderTemplate, PdfPageTemplate headerTemplate, bool hasFooterTemplate, PdfPageTemplate footerTemplate, bool hasFirstPageHeaderTemplate, PdfPageTemplate firstPageHeaderTemplate, bool hasFirstPageFooterTemplate, PdfPageTemplate firstPageFooterTemplate, bool hasOddPageHeaderTemplate, PdfPageTemplate oddPageHeaderTemplate, bool hasOddPageFooterTemplate, PdfPageTemplate oddPageFooterTemplate, bool hasEvenPageHeaderTemplate, PdfPageTemplate evenPageHeaderTemplate, bool hasEvenPageFooterTemplate, PdfPageTemplate evenPageFooterTemplate)
    {
        return WithTemplates(document, pageSize, margins, hasHeaderTemplate, headerTemplate, hasFooterTemplate, footerTemplate, hasFirstPageHeaderTemplate, firstPageHeaderTemplate, hasFirstPageFooterTemplate, firstPageFooterTemplate, hasOddPageHeaderTemplate, oddPageHeaderTemplate, hasOddPageFooterTemplate, oddPageFooterTemplate, hasEvenPageHeaderTemplate, evenPageHeaderTemplate, hasEvenPageFooterTemplate, evenPageFooterTemplate);
    }

    /// <summary>
    /// Adds a page to this page cursor and returns the stored value.
    /// </summary>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfPage AddPage()
    {
        CurrentPage = Document.AddPageWithDimensions(PageSize);
        CurrentPage.SetMargins(Margins.Left, Margins.Top, Margins.Right, Margins.Bottom);
        CurrentColumnIndex = 0;
        int pageNumber = GetCurrentPageNumber();
        DrawPageTemplates(pageNumber);
        CurrentY = CurrentPage.GetContentTop() - GetHeaderTemplateHeight(pageNumber);
        HasContentOnPage = false;
        return CurrentPage;
    }

    /// <summary>
    /// Returns the remaining height calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetRemainingHeight()
    {
        double remainingHeight = CurrentY - GetContentBottomWithFooter();
        if (remainingHeight < 0)
        {
            return 0;
        }

        return remainingHeight;
    }

    /// <summary>
    /// Returns the available width calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated value in PDF points unless stated otherwise.</returns>
    public double GetAvailableWidth()
    {
        return (CurrentPage.GetContentWidth() - ColumnGap * (ColumnCount - 1)) / ColumnCount;
    }

    /// <summary>
    /// Returns the measure context calculated from the object's current state.
    /// </summary>
    /// <returns>The resulting layout context.</returns>
    public PdfLayoutContext GetMeasureContext()
    {
        return new PdfLayoutContext(GetAvailableWidth(), GetRemainingHeight());
    }

    /// <summary>
    /// Returns the draw context calculated from the object's current state.
    /// </summary>
    /// <param name="result">The result to use for this operation.</param>
    /// <returns>The resulting layout draw context.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutDrawContext GetDrawContext(PdfLayoutResult result)
    {
        if (result == null)
        {
            throw new ArgumentNullException("result");
        }

        return new PdfLayoutDrawContext(
            CurrentPage,
            CurrentPage.GetContentLeft() + CurrentColumnIndex * (GetAvailableWidth() + ColumnGap),
            CurrentY - result.Size.Height,
            GetAvailableWidth(),
            GetRemainingHeight());
    }

    /// <summary>
    /// Measures, paginates, draws, and advances past a layout element.
    /// </summary>
    /// <param name="item">The layout element to flow through the current page.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawItem(IPdfLayoutElement item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        PdfLayoutResult result = item.Measure(GetMeasureContext());
        if (NeedsNewPage(result))
        {
            MoveToNextFlowArea();
            result = item.Measure(GetMeasureContext());
        }

        ThrowIfCannotPlace(item, result);

        PdfLayoutDrawContext drawContext = GetDrawContext(result);
        result = item.Draw(drawContext);
        DrawDebugOverlay(drawContext, result);
        Advance(result.Size.Height);
        return result;
    }

    /// <summary>
    /// Appends drawing commands for a block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawBlock(PdfBlock block)
    {
        return DrawItem(block);
    }

    /// <summary>
    /// Appends drawing commands for a text block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawTextBlock(PdfTextBlock block)
    {
        return DrawItem(block);
    }

    /// <summary>
    /// Appends drawing commands for a page break block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawPageBreakBlock(PdfPageBreakBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        PdfLayoutResult result = block.Measure(GetMeasureContext());
        AddPage();
        return result;
    }

    /// <summary>
    /// Appends drawing commands for an ensure space block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawEnsureSpaceBlock(PdfEnsureSpaceBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        PdfLayoutResult result = block.Measure(GetMeasureContext());
        EnsureSpace(block.Height);
        return result;
    }

    /// <summary>
    /// Appends drawing commands for an image block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawImageBlock(PdfImageBlock block)
    {
        return DrawItem(block);
    }

    /// <summary>
    /// Appends drawing commands for a keep together item to the target PDF canvas.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawKeepTogetherItem(PdfLayoutItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        PdfLayoutResult result = item.Measure(GetMeasureContext());
        if (result.Size.Height > GetRemainingHeight() && HasContentOnPage)
        {
            MoveToNextFlowArea();
        }

        return item.DrawWithoutKeepTogether(this);
    }

    /// <summary>
    /// Appends drawing commands for a stack block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawStackBlock(PdfStackBlock block)
    {
        return DrawItem(block);
    }

    /// <summary>
    /// Appends drawing commands for a row block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawRowBlock(PdfRowBlock block)
    {
        return DrawItem(block);
    }

    /// <summary>
    /// Appends drawing commands for a column block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawColumnBlock(PdfColumnBlock block)
    {
        return DrawItem(block);
    }

    /// <summary>
    /// Appends drawing commands for a grid block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawGridBlock(PdfGridBlock block)
    {
        return DrawItem(block);
    }

    /// <summary>
    /// Appends drawing commands for a table block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawTableBlock(PdfTableBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        PdfLayoutResult result = block.Measure(GetMeasureContext());
        if (block.GetRowCount() == 0)
        {
            if (NeedsNewPage(result))
            {
                MoveToNextFlowArea();
                result = block.Measure(GetMeasureContext());
            }

            ThrowIfCannotPlace(block, result);

            PdfLayoutDrawContext drawContext = GetDrawContext(result);
            result = block.Draw(drawContext);
            DrawDebugOverlay(drawContext, result);
            Advance(result.Size.Height);
            return result;
        }

        int rowIndex = 0;
        while (rowIndex < block.GetRowCount())
        {
            int rowsOnPage = CountTableRowsForPage(block, rowIndex);
            if (rowsOnPage == 0)
            {
                MoveToNextFlowArea();
                rowsOnPage = CountTableRowsForPage(block, rowIndex);
            }

            PdfLayoutResult pageResult = block.MeasureRows(GetMeasureContext(), rowIndex, rowsOnPage);
            ThrowIfCannotPlace(block, pageResult);
            PdfLayoutDrawContext drawContext = GetDrawContext(pageResult);
            block.DrawRows(drawContext, rowIndex, rowsOnPage);
            DrawDebugOverlay(drawContext, pageResult);
            Advance(pageResult.Size.Height);
            rowIndex += rowsOnPage;

            if (rowIndex < block.GetRowCount())
            {
                MoveToNextFlowArea();
            }
        }

        return result;
    }

    /// <summary>
    /// Appends drawing commands for a list block to the target PDF canvas.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawListBlock(PdfListBlock block)
    {
        return DrawItem(block);
    }

    /// <summary>
    /// Sets the header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetHeaderTemplate(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        HeaderTemplate = template;
        HasHeaderTemplate = true;
    }

    /// <summary>
    /// Sets the footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetFooterTemplate(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        FooterTemplate = template;
        HasFooterTemplate = true;
    }

    /// <summary>
    /// Clears the current header template.
    /// </summary>
    public void ClearHeaderTemplate()
    {
        HasHeaderTemplate = false;
    }

    /// <summary>
    /// Clears the current footer template.
    /// </summary>
    public void ClearFooterTemplate()
    {
        HasFooterTemplate = false;
    }

    /// <summary>
    /// Sets the first page header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetFirstPageHeaderTemplate(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        FirstPageHeaderTemplate = template;
        HasFirstPageHeaderTemplate = true;
    }

    /// <summary>
    /// Sets the first page footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetFirstPageFooterTemplate(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        FirstPageFooterTemplate = template;
        HasFirstPageFooterTemplate = true;
    }

    /// <summary>
    /// Sets the odd page header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetOddPageHeaderTemplate(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        OddPageHeaderTemplate = template;
        HasOddPageHeaderTemplate = true;
    }

    /// <summary>
    /// Sets the odd page footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetOddPageFooterTemplate(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        OddPageFooterTemplate = template;
        HasOddPageFooterTemplate = true;
    }

    /// <summary>
    /// Sets the even page header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetEvenPageHeaderTemplate(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        EvenPageHeaderTemplate = template;
        HasEvenPageHeaderTemplate = true;
    }

    /// <summary>
    /// Sets the even page footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetEvenPageFooterTemplate(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        EvenPageFooterTemplate = template;
        HasEvenPageFooterTemplate = true;
    }

    /// <summary>
    /// Clears the current first page header template.
    /// </summary>
    public void ClearFirstPageHeaderTemplate()
    {
        HasFirstPageHeaderTemplate = false;
    }

    /// <summary>
    /// Clears the current first page footer template.
    /// </summary>
    public void ClearFirstPageFooterTemplate()
    {
        HasFirstPageFooterTemplate = false;
    }

    /// <summary>
    /// Clears the current odd page header template.
    /// </summary>
    public void ClearOddPageHeaderTemplate()
    {
        HasOddPageHeaderTemplate = false;
    }

    /// <summary>
    /// Clears the current odd page footer template.
    /// </summary>
    public void ClearOddPageFooterTemplate()
    {
        HasOddPageFooterTemplate = false;
    }

    /// <summary>
    /// Clears the current even page header template.
    /// </summary>
    public void ClearEvenPageHeaderTemplate()
    {
        HasEvenPageHeaderTemplate = false;
    }

    /// <summary>
    /// Clears the current even page footer template.
    /// </summary>
    public void ClearEvenPageFooterTemplate()
    {
        HasEvenPageFooterTemplate = false;
    }

    /// <summary>
    /// Enables debug overlay for subsequent operations.
    /// </summary>
    public void EnableDebugOverlay()
    {
        EnableDebugOverlayWithStyle(PdfColor.Rgb(1, 0, 0), 0.25);
    }

    /// <summary>
    /// Enables debug overlay for subsequent operations.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    /// <param name="lineWidth">The line width in PDF points.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public void EnableDebugOverlayWithStyle(PdfColor color, double lineWidth)
    {
        if (color == null)
        {
            throw new ArgumentNullException("color");
        }

        if (double.IsNaN(lineWidth) || double.IsInfinity(lineWidth) || lineWidth <= 0)
        {
            throw new ArgumentOutOfRangeException("lineWidth", "Debug overlay line width must be positive.");
        }

        DebugOverlayEnabled = true;
        DebugOverlayColor = color;
        DebugOverlayLineWidth = lineWidth;
    }

    /// <summary>
    /// Disables debug overlay for subsequent operations.
    /// </summary>
    public void DisableDebugOverlay()
    {
        DebugOverlayEnabled = false;
    }

    /// <summary>
    /// Appends drawing commands for a final page templates to the target PDF canvas.
    /// </summary>
    public void DrawFinalPageTemplates()
    {
        int totalPages = PageNumberStart + Document.Pages.Count - FirstPageIndex - 1;
        for (int index = FirstPageIndex; index < Document.Pages.Count; index++)
        {
            int pageNumber = PageNumberStart + index - FirstPageIndex;
            DrawFinalPageTemplatesForPage(Document.Pages[index], pageNumber, totalPages);
        }
    }

    /// <summary>
    /// Appends drawing commands for a background layer to the target PDF canvas.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void DrawBackgroundLayer(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        DrawPageLayer(template, true);
    }

    /// <summary>
    /// Appends drawing commands for a watermark layer to the target PDF canvas.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void DrawWatermarkLayer(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        DrawPageLayer(template, false);
    }

    /// <summary>
    /// Ensures that space, creating a new page when necessary.
    /// </summary>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public void EnsureSpace(double height)
    {
        if (double.IsNaN(height) || double.IsInfinity(height) || height < 0)
        {
            throw new ArgumentOutOfRangeException("height", "Ensure-space height cannot be negative.");
        }

        if (height > GetRemainingHeight() && HasContentOnPage)
        {
            MoveToNextFlowArea();
        }
    }

    /// <summary>
    /// Moves the active layout position by the requested amount.
    /// </summary>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public void Advance(double height)
    {
        if (double.IsNaN(height) || double.IsInfinity(height) || height < 0)
        {
            throw new ArgumentOutOfRangeException("height", "Cursor advance height cannot be negative.");
        }

        CurrentY -= height;
        HasContentOnPage = true;
    }

    internal void ConfigureSectionFlow(int pageNumberStart, int columnCount, double columnGap, string runningHead)
    {
        PageNumberStart = pageNumberStart;
        ColumnCount = columnCount;
        ColumnGap = columnGap;
        RunningHead = runningHead;
    }

    internal void StartFlow()
    {
        AddPage();
    }

    internal void MoveToNextFlowArea()
    {
        if (CurrentColumnIndex + 1 < ColumnCount)
        {
            CurrentColumnIndex++;
            CurrentY = CurrentPage.GetContentTop() - GetHeaderTemplateHeight(GetCurrentPageNumber());
            HasContentOnPage = false;
            return;
        }

        AddPage();
    }

    internal int GetCurrentPageNumber()
    {
        return PageNumberStart + Document.Pages.Count - FirstPageIndex - 1;
    }

    internal void DrawDebugOverlay(PdfLayoutDrawContext context, PdfLayoutResult result)
    {
        if (!DebugOverlayEnabled)
        {
            return;
        }

        if (result.Size.Width == 0 || result.Size.Height == 0)
        {
            return;
        }

        double x = context.X;
        double y = context.Y;
        double width = result.Size.Width;
        double height = result.Size.Height;
        if (result.HasDebugBounds)
        {
            x += result.DebugBounds.X;
            y += result.DebugBounds.Y;
            width = result.DebugBounds.Width;
            height = result.DebugBounds.Height;
        }

        context.Page.Canvas.SaveState();
        context.Page.Canvas.SetStrokeColorValue(DebugOverlayColor);
        context.Page.Canvas.SetLineWidth(DebugOverlayLineWidth);
        context.Page.Canvas.StrokeRectangle(x, y, width, height);
        context.Page.Canvas.RestoreState();
    }

    internal bool NeedsNewPage(PdfLayoutResult result)
    {
        if (result.Size.Height <= GetRemainingHeight())
        {
            return false;
        }

        if (!HasContentOnPage)
        {
            return false;
        }

        return true;
    }

    internal void ThrowIfCannotPlace(IPdfLayoutElement item, PdfLayoutResult result)
    {
        double availableWidth = GetAvailableWidth();
        double availableHeight = GetRemainingHeight();
        if (result.Fits && result.Size.Width <= availableWidth && result.Size.Height <= availableHeight)
        {
            return;
        }

        string elementType = item.GetType().Name;
        string message = $"Layout element {elementType} requires {result.Size.Width} by {result.Size.Height} points, but page {GetCurrentPageNumber()}, column {CurrentColumnIndex + 1} has {availableWidth} by {availableHeight} points available.";
        throw new PdfLayoutOverflowException(item, elementType, result.Size.Width, result.Size.Height, availableWidth, availableHeight, GetCurrentPageNumber(), CurrentColumnIndex + 1, message);
    }

    internal int CountTableRowsForPage(PdfTableBlock block, int startRowIndex)
    {
        double availableHeight = GetRemainingHeight();
        double usedHeight = block.GetOuterVertical();
        usedHeight += block.MeasureHeaderRowsHeight(availableHeight - usedHeight);
        usedHeight += block.MeasureFooterRowsHeight(availableHeight - usedHeight);
        int rowCount = 0;

        while (startRowIndex + rowCount < block.GetRowCount())
        {
            int paginationRows = block.GetPaginationRowCount(startRowIndex + rowCount);
            double rowHeight = block.MeasureBodyRowsHeight(startRowIndex + rowCount, paginationRows, availableHeight - usedHeight);
            double nextHeight = usedHeight + rowHeight;
            if (nextHeight > availableHeight)
            {
                if (rowCount == 0)
                {
                    if (HasContentOnPage)
                    {
                        return 0;
                    }

                    return paginationRows;
                }

                return rowCount;
            }

            usedHeight = nextHeight;
            rowCount += paginationRows;
        }

        return rowCount;
    }

    internal double GetHeaderTemplateHeight(int pageNumber)
    {
        if (HasHeaderTemplateForPage(pageNumber))
        {
            return GetHeaderTemplateForPage(pageNumber).Height;
        }

        return 0;
    }

    internal double GetFooterTemplateHeight(int pageNumber)
    {
        if (HasFooterTemplateForPage(pageNumber))
        {
            return GetFooterTemplateForPage(pageNumber).Height;
        }

        return 0;
    }

    internal double GetContentBottomWithFooter()
    {
        return CurrentPage.GetContentBottom() + GetFooterTemplateHeight(GetCurrentPageNumber());
    }

    internal void DrawPageTemplates(int pageNumber)
    {
        double contentLeft = CurrentPage.GetContentLeft();
        double contentWidth = CurrentPage.GetContentWidth();

        if (HasHeaderTemplateForPage(pageNumber))
        {
            PdfPageTemplate headerTemplate = GetHeaderTemplateForPage(pageNumber);
            if (!headerTemplate.NeedsFinalTemplatePass())
            {
                headerTemplate.Draw(CurrentPage, contentLeft, CurrentPage.GetContentTop() - headerTemplate.Height, contentWidth);
            }
        }

        if (HasFooterTemplateForPage(pageNumber))
        {
            PdfPageTemplate footerTemplate = GetFooterTemplateForPage(pageNumber);
            if (!footerTemplate.NeedsFinalTemplatePass())
            {
                footerTemplate.Draw(CurrentPage, contentLeft, CurrentPage.GetContentBottom(), contentWidth);
            }
        }
    }

    internal void DrawFinalPageTemplatesForPage(PdfPage page, int pageNumber, int totalPages)
    {
        double contentLeft = page.GetContentLeft();
        double contentWidth = page.GetContentWidth();

        if (HasHeaderTemplateForPage(pageNumber))
        {
            PdfPageTemplate headerTemplate = GetHeaderTemplateForPage(pageNumber);
            if (headerTemplate.NeedsFinalTemplatePass())
            {
                headerTemplate.DrawForPageWithRunningHead(page, contentLeft, page.GetContentTop() - headerTemplate.Height, contentWidth, pageNumber, totalPages, RunningHead);
            }
        }

        if (HasFooterTemplateForPage(pageNumber))
        {
            PdfPageTemplate footerTemplate = GetFooterTemplateForPage(pageNumber);
            if (footerTemplate.NeedsFinalTemplatePass())
            {
                footerTemplate.DrawForPageWithRunningHead(page, contentLeft, page.GetContentBottom(), contentWidth, pageNumber, totalPages, RunningHead);
            }
        }
    }

    internal void DrawPageLayer(PdfPageTemplate template, bool prepend)
    {
        int totalPages = Document.Pages.Count;
        for (int index = 0; index < Document.Pages.Count; index++)
        {
            PdfPage page = Document.Pages[index];
            PdfPage layerPage = new PdfPage(page.Width, page.Height);
            int pageNumber = index + 1;

            layerPage.SetMargins(page.Margins.Left, page.Margins.Top, page.Margins.Right, page.Margins.Bottom);
            template.DrawForPage(layerPage, 0, 0, page.Width, pageNumber, totalPages);
            if (prepend)
            {
                page.Canvas.PrependCanvas(layerPage.Canvas);
            }
            else
            {
                page.Canvas.AppendCanvas(layerPage.Canvas);
            }
        }
    }

    internal void DrawContentLayer(PdfContentLayer layer, bool prepend)
    {
        int totalPages = Document.Pages.Count;
        for (int index = 0; index < Document.Pages.Count; index++)
        {
            PdfPage page = Document.Pages[index];
            PdfCanvas layerCanvas = layer.DrawForPage(page, index + 1, totalPages);
            if (prepend)
            {
                page.Canvas.PrependCanvas(layerCanvas);
            }
            else
            {
                page.Canvas.AppendCanvas(layerCanvas);
            }
        }
    }

    internal void DrawContentLayerDebug(PdfContentLayer layer, PdfColor color, double lineWidth)
    {
        foreach (PdfPage page in Document.Pages)
        {
            page.Canvas.AppendCanvas(layer.DrawDebugForPage(page, color, lineWidth));
        }
    }

    internal bool HasHeaderTemplateForPage(int pageNumber)
    {
        if (pageNumber == PageNumberStart && HasFirstPageHeaderTemplate)
        {
            return true;
        }

        if (IsEvenPage(pageNumber) && HasEvenPageHeaderTemplate)
        {
            return true;
        }

        if (!IsEvenPage(pageNumber) && HasOddPageHeaderTemplate)
        {
            return true;
        }

        return HasHeaderTemplate;
    }

    internal bool HasFooterTemplateForPage(int pageNumber)
    {
        if (pageNumber == PageNumberStart && HasFirstPageFooterTemplate)
        {
            return true;
        }

        if (IsEvenPage(pageNumber) && HasEvenPageFooterTemplate)
        {
            return true;
        }

        if (!IsEvenPage(pageNumber) && HasOddPageFooterTemplate)
        {
            return true;
        }

        return HasFooterTemplate;
    }

    internal PdfPageTemplate GetHeaderTemplateForPage(int pageNumber)
    {
        if (pageNumber == PageNumberStart && HasFirstPageHeaderTemplate)
        {
            return FirstPageHeaderTemplate;
        }

        if (IsEvenPage(pageNumber) && HasEvenPageHeaderTemplate)
        {
            return EvenPageHeaderTemplate;
        }

        if (!IsEvenPage(pageNumber) && HasOddPageHeaderTemplate)
        {
            return OddPageHeaderTemplate;
        }

        return HeaderTemplate;
    }

    internal PdfPageTemplate GetFooterTemplateForPage(int pageNumber)
    {
        if (pageNumber == PageNumberStart && HasFirstPageFooterTemplate)
        {
            return FirstPageFooterTemplate;
        }

        if (IsEvenPage(pageNumber) && HasEvenPageFooterTemplate)
        {
            return EvenPageFooterTemplate;
        }

        if (!IsEvenPage(pageNumber) && HasOddPageFooterTemplate)
        {
            return OddPageFooterTemplate;
        }

        return FooterTemplate;
    }

    internal bool IsEvenPage(int pageNumber)
    {
        return pageNumber % 2 == 0;
    }

    internal void Initialize(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins, bool hasHeaderTemplate, PdfPageTemplate headerTemplate, bool hasFooterTemplate, PdfPageTemplate footerTemplate, bool hasFirstPageHeaderTemplate, PdfPageTemplate firstPageHeaderTemplate, bool hasFirstPageFooterTemplate, PdfPageTemplate firstPageFooterTemplate, bool hasOddPageHeaderTemplate, PdfPageTemplate oddPageHeaderTemplate, bool hasOddPageFooterTemplate, PdfPageTemplate oddPageFooterTemplate, bool hasEvenPageHeaderTemplate, PdfPageTemplate evenPageHeaderTemplate, bool hasEvenPageFooterTemplate, PdfPageTemplate evenPageFooterTemplate)
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

        if (hasHeaderTemplate && headerTemplate == null)
        {
            throw new ArgumentNullException("headerTemplate");
        }

        if (hasFooterTemplate && footerTemplate == null)
        {
            throw new ArgumentNullException("footerTemplate");
        }

        if (hasFirstPageHeaderTemplate && firstPageHeaderTemplate == null)
        {
            throw new ArgumentNullException("firstPageHeaderTemplate");
        }

        if (hasFirstPageFooterTemplate && firstPageFooterTemplate == null)
        {
            throw new ArgumentNullException("firstPageFooterTemplate");
        }

        if (hasOddPageHeaderTemplate && oddPageHeaderTemplate == null)
        {
            throw new ArgumentNullException("oddPageHeaderTemplate");
        }

        if (hasOddPageFooterTemplate && oddPageFooterTemplate == null)
        {
            throw new ArgumentNullException("oddPageFooterTemplate");
        }

        if (hasEvenPageHeaderTemplate && evenPageHeaderTemplate == null)
        {
            throw new ArgumentNullException("evenPageHeaderTemplate");
        }

        if (hasEvenPageFooterTemplate && evenPageFooterTemplate == null)
        {
            throw new ArgumentNullException("evenPageFooterTemplate");
        }

        Document = document;
        PageSize = pageSize;
        Margins = margins;
        HasHeaderTemplate = hasHeaderTemplate;
        HeaderTemplate = headerTemplate;
        HasFooterTemplate = hasFooterTemplate;
        FooterTemplate = footerTemplate;
        HasFirstPageHeaderTemplate = hasFirstPageHeaderTemplate;
        FirstPageHeaderTemplate = firstPageHeaderTemplate;
        HasFirstPageFooterTemplate = hasFirstPageFooterTemplate;
        FirstPageFooterTemplate = firstPageFooterTemplate;
        HasOddPageHeaderTemplate = hasOddPageHeaderTemplate;
        OddPageHeaderTemplate = oddPageHeaderTemplate;
        HasOddPageFooterTemplate = hasOddPageFooterTemplate;
        OddPageFooterTemplate = oddPageFooterTemplate;
        HasEvenPageHeaderTemplate = hasEvenPageHeaderTemplate;
        EvenPageHeaderTemplate = evenPageHeaderTemplate;
        HasEvenPageFooterTemplate = hasEvenPageFooterTemplate;
        EvenPageFooterTemplate = evenPageFooterTemplate;
        DebugOverlayEnabled = false;
        DebugOverlayColor = PdfColor.Rgb(1, 0, 0);
        DebugOverlayLineWidth = 0.25;
        FirstPageIndex = document.Pages.Count;
        PageNumberStart = FirstPageIndex + 1;
        ColumnCount = 1;
        ColumnGap = 0;
        CurrentColumnIndex = 0;
        RunningHead = string.Empty;
        CurrentPage = new PdfPage(pageSize.Width, pageSize.Height);
    }
}
