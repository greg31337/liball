using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Defines the operations exposed by <see cref="PdfLayoutDocument"/>.
/// </summary>
public interface IPdfLayoutDocument
{
    /// <summary>
    /// Adds a block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddBlock(PdfBlock block);

    /// <summary>
    /// Adds a text block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddTextBlock(PdfTextBlock block);

    /// <summary>
    /// Adds a page break to this layout document.
    /// </summary>
    void AddPageBreak();

    /// <summary>
    /// Adds a page break block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddPageBreakBlock(PdfPageBreakBlock block);

    /// <summary>
    /// Adds a stack block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddStackBlock(PdfStackBlock block);

    /// <summary>
    /// Adds a row block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddRowBlock(PdfRowBlock block);

    /// <summary>
    /// Adds a column block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddColumnBlock(PdfColumnBlock block);

    /// <summary>
    /// Adds a grid block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddGridBlock(PdfGridBlock block);

    /// <summary>
    /// Adds a table block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddTableBlock(PdfTableBlock block);

    /// <summary>
    /// Adds a list block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddListBlock(PdfListBlock block);

    /// <summary>
    /// Sets reusable defaults applied to subsequently added typed layout elements.
    /// </summary>
    /// <param name="theme">The document theme to copy.</param>
    void SetTheme(PdfDocumentTheme theme);

    /// <summary>
    /// Adds an ensure space to this layout document.
    /// </summary>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    void AddEnsureSpace(double height);

    /// <summary>
    /// Adds an ensure space block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddEnsureSpaceBlock(PdfEnsureSpaceBlock block);

    /// <summary>
    /// Adds an image block to this layout document.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    void AddImageBlock(PdfImageBlock block);

    /// <summary>
    /// Adds a semantic frame to this layout document.
    /// </summary>
    /// <param name="frame">The frame to add.</param>
    void AddFrame(PdfFrame frame);

    /// <summary>
    /// Adds a semantic caption to this layout document.
    /// </summary>
    /// <param name="caption">The caption to add.</param>
    void AddCaption(PdfCaption caption);

    /// <summary>
    /// Adds a semantic figure to this layout document.
    /// </summary>
    /// <param name="figure">The figure to add.</param>
    void AddFigure(PdfFigure figure);

    /// <summary>
    /// Adds a semantic sidebar to this layout document.
    /// </summary>
    /// <param name="sidebar">The sidebar to add.</param>
    void AddSidebar(PdfSidebar sidebar);

    /// <summary>
    /// Adds a semantic callout to this layout document.
    /// </summary>
    /// <param name="callout">The callout to add.</param>
    void AddCallout(PdfCallout callout);

    /// <summary>
    /// Adds a chart placeholder to this layout document.
    /// </summary>
    /// <param name="chart">The chart placeholder to add.</param>
    void AddChartPlaceholder(PdfChartPlaceholder chart);

    /// <summary>
    /// Adds an independently configured section to this layout document.
    /// </summary>
    /// <param name="pageSize">The page size before section orientation is applied.</param>
    /// <param name="margins">The section page margins.</param>
    /// <returns>The section that was added.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    PdfDocumentSection AddSection(PdfPageDimensions pageSize, PdfPageMargins margins);

    /// <summary>
    /// Adds an item to this layout document.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddItem(IPdfLayoutElement item);

    /// <summary>
    /// Adds a keep together item to this layout document.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void AddKeepTogetherItem(IPdfLayoutElement item);

    /// <summary>
    /// Adds a keep-with-next group to this layout document.
    /// </summary>
    /// <param name="group">The ordered group to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when the group is null.</exception>
    void AddKeepWithNextGroup(PdfKeepWithNextGroup group);

    /// <summary>
    /// Sets the header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Clears the current header template.
    /// </summary>
    void ClearHeaderTemplate();

    /// <summary>
    /// Clears the current footer template.
    /// </summary>
    void ClearFooterTemplate();

    /// <summary>
    /// Sets the first page header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetFirstPageHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the first page footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetFirstPageFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the odd page header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetOddPageHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the odd page footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetOddPageFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the even page header template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetEvenPageHeaderTemplate(PdfPageTemplate template);

    /// <summary>
    /// Sets the even page footer template used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetEvenPageFooterTemplate(PdfPageTemplate template);

    /// <summary>
    /// Clears the current first page header template.
    /// </summary>
    void ClearFirstPageHeaderTemplate();

    /// <summary>
    /// Clears the current first page footer template.
    /// </summary>
    void ClearFirstPageFooterTemplate();

    /// <summary>
    /// Clears the current odd page header template.
    /// </summary>
    void ClearOddPageHeaderTemplate();

    /// <summary>
    /// Clears the current odd page footer template.
    /// </summary>
    void ClearOddPageFooterTemplate();

    /// <summary>
    /// Clears the current even page header template.
    /// </summary>
    void ClearEvenPageHeaderTemplate();

    /// <summary>
    /// Clears the current even page footer template.
    /// </summary>
    void ClearEvenPageFooterTemplate();

    /// <summary>
    /// Sets the background layer used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetBackgroundLayer(PdfPageTemplate template);

    /// <summary>
    /// Sets the watermark layer used for subsequent PDF generation.
    /// </summary>
    /// <param name="template">The template to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SetWatermarkLayer(PdfPageTemplate template);

    /// <summary>
    /// Clears the current background layer.
    /// </summary>
    void ClearBackgroundLayer();

    /// <summary>
    /// Clears the current watermark layer.
    /// </summary>
    void ClearWatermarkLayer();

    /// <summary>
    /// Adds a reusable ordered layer to every rendered page.
    /// </summary>
    /// <param name="layer">The content layer to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when layer is null.</exception>
    void AddContentLayer(PdfContentLayer layer);

    /// <summary>
    /// Removes all reusable ordered content layers.
    /// </summary>
    void ClearContentLayers();

    /// <summary>
    /// Enables debug overlay for subsequent operations.
    /// </summary>
    void EnableDebugOverlay();

    /// <summary>
    /// Enables debug overlay for subsequent operations.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    /// <param name="lineWidth">The line width in PDF points.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    void EnableDebugOverlayWithStyle(PdfColor color, double lineWidth);

    /// <summary>
    /// Disables debug overlay for subsequent operations.
    /// </summary>
    void DisableDebugOverlay();

    /// <summary>
    /// Flows the collected layout items into PDF pages and returns the cursor at the final position.
    /// </summary>
    /// <returns>The cursor positioned immediately after the final rendered item.</returns>
    PdfPageCursor Render();
}
