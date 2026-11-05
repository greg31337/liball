using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Collects layout items and renders them into pages while applying margins, page breaks, headers, and footers.
/// </summary>
/// <remarks>
/// Layout measures each item against the available content area and creates additional pages when an item cannot fit on the current page.
/// </remarks>
public class PdfLayoutDocument : IPdfLayoutDocument
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
    /// Indicates whether this object has background layer.
    /// </summary>
    internal bool HasBackgroundLayer;
    /// <summary>
    /// Stores the background layer used by this object.
    /// </summary>
    internal PdfPageTemplate BackgroundLayer = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
    /// <summary>
    /// Indicates whether this object has watermark layer.
    /// </summary>
    internal bool HasWatermarkLayer;
    /// <summary>
    /// Stores the watermark layer used by this object.
    /// </summary>
    internal PdfPageTemplate WatermarkLayer = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
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
    internal bool HasPageNumberStart;
    internal int PageNumberStart;
    internal int ColumnCount;
    internal double ColumnGap;
    internal string RunningHead = string.Empty;
    internal bool HasTheme;
    internal PdfDocumentTheme Theme = new PdfDocumentTheme();
    internal List<IPdfLayoutElement> _items = new List<IPdfLayoutElement>();
    /// <summary>
    /// Exposes the items currently stored by this object.
    /// </summary>
    internal IReadOnlyList<IPdfLayoutElement> Items = new List<IPdfLayoutElement>();
    internal List<PdfDocumentSection> _sections = new List<PdfDocumentSection>();
    internal IReadOnlyList<PdfDocumentSection> Sections = new List<PdfDocumentSection>();
    internal List<PdfContentLayer> _contentLayers = new List<PdfContentLayer>();

    /// <summary>
    /// Initializes a new layout document.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <param name="pageSize">The page size to use for this operation.</param>
    /// <param name="margins">The margins to use for this operation.</param>
    public PdfLayoutDocument(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins)
    {
        Initialize(document, pageSize, margins);
    }

    /// <summary>
    /// Creates a layout document with letter-sized pages and no margins.
    /// </summary>
    /// <param name="document">The PDF document to inspect, modify, or render.</param>
    /// <returns>The resulting layout document.</returns>
    internal static PdfLayoutDocument Default(PdfDocument document)
    {
        return new PdfLayoutDocument(document, new PdfPageSizeFactory().Letter(), new PdfPageMargins(0, 0, 0, 0));
    }

    /// <summary>
    /// Adds a block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddBlock(PdfBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (HasTheme)
        {
            block.SetStyle(Theme.Block);
        }

        AddItem(block);
    }

    /// <summary>
    /// Adds a text block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddTextBlock(PdfTextBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (HasTheme)
        {
            block.SetParagraphStyle(Theme.BodyParagraph);
            block.SetStyle(Theme.Block);
        }

        AddItem(block);
    }

    /// <summary>
    /// Adds a page break to this layout document.
    /// </summary>
    public void AddPageBreak()
    {
        AddItem(new PdfPageBreakBlock());
    }

    /// <summary>
    /// Adds a page break block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddPageBreakBlock(PdfPageBreakBlock block)
    {
        AddItem(block);
    }

    /// <summary>
    /// Adds a stack block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddStackBlock(PdfStackBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (HasTheme)
        {
            block.SetStyle(Theme.Block);
        }

        AddItem(block);
    }

    /// <summary>
    /// Adds a row block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddRowBlock(PdfRowBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (HasTheme)
        {
            block.SetStyle(Theme.Block);
        }

        AddItem(block);
    }

    /// <summary>
    /// Adds a column block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddColumnBlock(PdfColumnBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (HasTheme)
        {
            block.SetStyle(Theme.Block);
        }

        AddItem(block);
    }

    /// <summary>
    /// Adds a grid block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddGridBlock(PdfGridBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (HasTheme)
        {
            block.SetStyle(Theme.Block);
        }

        AddItem(block);
    }

    /// <summary>
    /// Adds a table block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddTableBlock(PdfTableBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (HasTheme)
        {
            block.SetTableStyle(Theme.Table);
        }

        AddItem(block);
    }

    /// <summary>
    /// Adds a list block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddListBlock(PdfListBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (HasTheme)
        {
            block.SetStyle(Theme.Block);
        }

        AddItem(block);
    }

    /// <summary>
    /// Sets reusable defaults applied to subsequently added typed layout elements.
    /// </summary>
    /// <param name="theme">The document theme to copy.</param>
    /// <exception cref="ArgumentNullException">Thrown when theme is null.</exception>
    public void SetTheme(PdfDocumentTheme theme)
    {
        if (theme == null)
        {
            throw new ArgumentNullException("theme");
        }

        Theme = theme.Copy();
        HasTheme = true;
    }

    /// <summary>
    /// Adds an ensure space to this layout document.
    /// </summary>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    public void AddEnsureSpace(double height)
    {
        AddItem(new PdfEnsureSpaceBlock(height));
    }

    /// <summary>
    /// Adds an ensure space block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddEnsureSpaceBlock(PdfEnsureSpaceBlock block)
    {
        AddItem(block);
    }

    /// <summary>
    /// Adds an image block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddImageBlock(PdfImageBlock block)
    {
        AddItem(block);
    }

    /// <summary>
    /// Adds a semantic frame to this layout document.
    /// </summary>
    /// <param name="frame">The frame to add.</param>
    public void AddFrame(PdfFrame frame)
    {
        AddItem(frame);
    }

    /// <summary>
    /// Adds a semantic caption to this layout document.
    /// </summary>
    /// <param name="caption">The caption to add.</param>
    public void AddCaption(PdfCaption caption)
    {
        AddItem(caption);
    }

    /// <summary>
    /// Adds a semantic figure to this layout document.
    /// </summary>
    /// <param name="figure">The figure to add.</param>
    public void AddFigure(PdfFigure figure)
    {
        AddItem(figure);
    }

    /// <summary>
    /// Adds a semantic sidebar to this layout document.
    /// </summary>
    /// <param name="sidebar">The sidebar to add.</param>
    public void AddSidebar(PdfSidebar sidebar)
    {
        AddItem(sidebar);
    }

    /// <summary>
    /// Adds a semantic callout to this layout document.
    /// </summary>
    /// <param name="callout">The callout to add.</param>
    public void AddCallout(PdfCallout callout)
    {
        AddItem(callout);
    }

    /// <summary>
    /// Adds a chart placeholder to this layout document.
    /// </summary>
    /// <param name="chart">The chart placeholder to add.</param>
    public void AddChartPlaceholder(PdfChartPlaceholder chart)
    {
        AddItem(chart);
    }

    /// <summary>
    /// Adds an independently configured section to this layout document.
    /// </summary>
    /// <param name="pageSize">The page size before section orientation is applied.</param>
    /// <param name="margins">The section page margins.</param>
    /// <returns>The section that was added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfDocumentSection AddSection(PdfPageDimensions pageSize, PdfPageMargins margins)
    {
        if (pageSize == null)
        {
            throw new ArgumentNullException("pageSize");
        }

        if (margins == null)
        {
            throw new ArgumentNullException("margins");
        }

        PdfDocumentSection section = new PdfDocumentSection(Document, pageSize, margins);
        _sections.Add(section);
        return section;
    }

    /// <summary>
    /// Adds an item to this layout document.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddItem(IPdfLayoutElement item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        _items.Add(item);
    }

    /// <summary>
    /// Adds a keep together item to this layout document.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddKeepTogetherItem(IPdfLayoutElement item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        PdfLayoutItem layoutItem = PdfLayoutItem.FromElement(item);
        layoutItem.SetKeepTogether();
        _items.Add(layoutItem);
    }

    /// <summary>
    /// Adds a keep-with-next group to this layout document.
    /// </summary>
    /// <param name="group">The ordered group to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when the group is null.</exception>
    public void AddKeepWithNextGroup(PdfKeepWithNextGroup group)
    {
        if (group == null)
        {
            throw new ArgumentNullException("group");
        }

        _items.Add(group);
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
    /// Sets the background layer used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetBackgroundLayer(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        BackgroundLayer = template;
        HasBackgroundLayer = true;
    }

    /// <summary>
    /// Sets the watermark layer used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetWatermarkLayer(PdfPageTemplate template)
    {
        if (template == null)
        {
            throw new ArgumentNullException("template");
        }

        WatermarkLayer = template;
        HasWatermarkLayer = true;
    }

    /// <summary>
    /// Clears the current background layer.
    /// </summary>
    public void ClearBackgroundLayer()
    {
        HasBackgroundLayer = false;
    }

    /// <summary>
    /// Clears the current watermark layer.
    /// </summary>
    public void ClearWatermarkLayer()
    {
        HasWatermarkLayer = false;
    }

    /// <summary>
    /// Adds a reusable ordered layer to every rendered page.
    /// </summary>
    /// <param name="layer">The content layer to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when layer is null.</exception>
    public void AddContentLayer(PdfContentLayer layer)
    {
        if (layer == null)
        {
            throw new ArgumentNullException("layer");
        }

        _contentLayers.Add(layer);
    }

    /// <summary>
    /// Removes all reusable ordered content layers.
    /// </summary>
    public void ClearContentLayers()
    {
        _contentLayers.Clear();
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
    /// Flows the collected layout items into PDF pages and returns the cursor at the final position.
    /// </summary>
    /// <returns>The cursor positioned immediately after the final rendered item.</returns>
    public PdfPageCursor Render()
    {
        PdfPageCursor cursor;
        if (_sections.Count == 0)
        {
            cursor = RenderContent();
        }
        else
        {
            int sectionIndex = 0;
            if (_items.Count > 0)
            {
                cursor = RenderContent();
            }
            else
            {
                cursor = _sections[0].RenderSection();
                sectionIndex = 1;
            }

            while (sectionIndex < _sections.Count)
            {
                cursor = _sections[sectionIndex].RenderSection();
                sectionIndex++;
            }
        }

        if (HasBackgroundLayer)
        {
            cursor.DrawBackgroundLayer(BackgroundLayer);
        }

        if (HasWatermarkLayer)
        {
            cursor.DrawWatermarkLayer(WatermarkLayer);
        }

        DrawContentLayers(cursor, PdfContentLayerPlacement.Background, true);
        DrawContentLayers(cursor, PdfContentLayerPlacement.Watermark, false);
        DrawContentLayers(cursor, PdfContentLayerPlacement.Foreground, false);
        DrawContentLayerDebugOverlays(cursor);

        return cursor;
    }

    internal void DrawContentLayers(PdfPageCursor cursor, PdfContentLayerPlacement placement, bool prepend)
    {
        List<PdfContentLayer> ordered = new List<PdfContentLayer>();
        foreach (PdfContentLayer layer in _contentLayers)
        {
            if (layer.Placement == placement)
            {
                ordered.Add(layer);
            }
        }

        int sortIndex = 1;
        while (sortIndex < ordered.Count)
        {
            PdfContentLayer current = ordered[sortIndex];
            int insertionIndex = sortIndex;
            while (insertionIndex > 0 && ordered[insertionIndex - 1].Order > current.Order)
            {
                ordered[insertionIndex] = ordered[insertionIndex - 1];
                insertionIndex--;
            }

            ordered[insertionIndex] = current;
            sortIndex++;
        }

        if (prepend)
        {
            int reverseIndex = ordered.Count - 1;
            while (reverseIndex >= 0)
            {
                cursor.DrawContentLayer(ordered[reverseIndex], true);
                reverseIndex--;
            }
            return;
        }

        foreach (PdfContentLayer layer in ordered)
        {
            cursor.DrawContentLayer(layer, false);
        }
    }

    internal void DrawContentLayerDebugOverlays(PdfPageCursor cursor)
    {
        if (!DebugOverlayEnabled)
        {
            return;
        }

        foreach (PdfContentLayer layer in _contentLayers)
        {
            cursor.DrawContentLayerDebug(layer, DebugOverlayColor, DebugOverlayLineWidth);
        }
    }

    internal PdfPageCursor RenderContent()
    {
        PdfPageCursor cursor = PdfPageCursor.PrepareWithTemplates(
            Document,
            PageSize,
            Margins,
            HasHeaderTemplate,
            HeaderTemplate,
            HasFooterTemplate,
            FooterTemplate,
            HasFirstPageHeaderTemplate,
            FirstPageHeaderTemplate,
            HasFirstPageFooterTemplate,
            FirstPageFooterTemplate,
            HasOddPageHeaderTemplate,
            OddPageHeaderTemplate,
            HasOddPageFooterTemplate,
            OddPageFooterTemplate,
            HasEvenPageHeaderTemplate,
            EvenPageHeaderTemplate,
            HasEvenPageFooterTemplate,
            EvenPageFooterTemplate);

        int pageNumberStart = Document.Pages.Count + 1;
        if (HasPageNumberStart)
        {
            pageNumberStart = PageNumberStart;
        }

        cursor.ConfigureSectionFlow(pageNumberStart, ColumnCount, ColumnGap, RunningHead);
        cursor.StartFlow();

        if (DebugOverlayEnabled)
        {
            cursor.EnableDebugOverlayWithStyle(DebugOverlayColor, DebugOverlayLineWidth);
        }

        foreach (IPdfLayoutElement item in _items)
        {
            item.Flow(cursor);
        }

        cursor.DrawFinalPageTemplates();
        return cursor;
    }

    internal void Initialize(PdfDocument document, PdfPageDimensions pageSize, PdfPageMargins margins)
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

        Document = document;
        PageSize = pageSize;
        Margins = margins;
        HasHeaderTemplate = false;
        HeaderTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        HasFooterTemplate = false;
        FooterTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        HasFirstPageHeaderTemplate = false;
        FirstPageHeaderTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        HasFirstPageFooterTemplate = false;
        FirstPageFooterTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        HasOddPageHeaderTemplate = false;
        OddPageHeaderTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        HasOddPageFooterTemplate = false;
        OddPageFooterTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        HasEvenPageHeaderTemplate = false;
        EvenPageHeaderTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        HasEvenPageFooterTemplate = false;
        EvenPageFooterTemplate = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        HasBackgroundLayer = false;
        BackgroundLayer = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        HasWatermarkLayer = false;
        WatermarkLayer = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), 0);
        DebugOverlayEnabled = false;
        DebugOverlayColor = PdfColor.Rgb(1, 0, 0);
        DebugOverlayLineWidth = 0.25;
        HasPageNumberStart = false;
        PageNumberStart = 1;
        ColumnCount = 1;
        ColumnGap = 0;
        RunningHead = string.Empty;
        HasTheme = false;
        Theme = new PdfDocumentTheme();
        _items = new List<IPdfLayoutElement>();
        Items = _items;
        _sections = new List<PdfDocumentSection>();
        Sections = _sections;
        _contentLayers = new List<PdfContentLayer>();
    }
}
