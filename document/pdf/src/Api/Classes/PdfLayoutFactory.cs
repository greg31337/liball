namespace pdf;

/// <summary>
/// Creates commonly configured layout blocks and reusable page templates.
/// </summary>
public class PdfLayoutFactory : IPdfLayoutFactory
{
    /// <summary>
    /// Creates a layout factory.
    /// </summary>
    public PdfLayoutFactory()
    {
    }

    /// <summary>
    /// Creates a fixed-size layout block.
    /// </summary>
    /// <param name="width">The content width in PDF points.</param>
    /// <param name="height">The content height in PDF points.</param>
    /// <returns>The configured block.</returns>
    public PdfBlock CreateFixedBlock(double width, double height)
    {
        return PdfBlock.WithSize(width, height);
    }

    /// <summary>
    /// Creates a vertical stack with spacing between adjacent items.
    /// </summary>
    /// <param name="spacing">The non-negative spacing in PDF points.</param>
    /// <returns>The configured stack block.</returns>
    public PdfStackBlock CreateStackBlock(double spacing)
    {
        return PdfStackBlock.WithSpacing(spacing);
    }

    /// <summary>
    /// Creates a horizontal row with spacing between adjacent items.
    /// </summary>
    /// <param name="spacing">The non-negative spacing in PDF points.</param>
    /// <returns>The configured row block.</returns>
    public PdfRowBlock CreateRowBlock(double spacing)
    {
        return PdfRowBlock.WithSpacing(spacing);
    }

    /// <summary>
    /// Creates a fixed-width vertical column without item spacing.
    /// </summary>
    /// <param name="width">The non-negative column width in PDF points; zero sizes the column from its content.</param>
    /// <returns>The configured column block.</returns>
    public PdfColumnBlock CreateColumnBlock(double width)
    {
        return PdfColumnBlock.Create(width);
    }

    /// <summary>
    /// Creates a fixed-width vertical column with spacing between adjacent items.
    /// </summary>
    /// <param name="width">The non-negative column width in PDF points; zero sizes the column from its content.</param>
    /// <param name="spacing">The non-negative spacing in PDF points.</param>
    /// <returns>The configured column block.</returns>
    public PdfColumnBlock CreateSpacedColumnBlock(double width, double spacing)
    {
        return PdfColumnBlock.Create(width, spacing);
    }

    /// <summary>
    /// Creates a grid with the requested number of columns and no gaps.
    /// </summary>
    /// <param name="columnCount">The positive number of columns.</param>
    /// <returns>The configured grid block.</returns>
    public PdfGridBlock CreateGridBlock(int columnCount)
    {
        return PdfGridBlock.Create(columnCount);
    }

    /// <summary>
    /// Creates a grid with explicit column and row gaps.
    /// </summary>
    /// <param name="columnCount">The positive number of columns.</param>
    /// <param name="columnGap">The non-negative gap between columns in PDF points.</param>
    /// <param name="rowGap">The non-negative gap between rows in PDF points.</param>
    /// <returns>The configured grid block.</returns>
    public PdfGridBlock CreateGappedGridBlock(int columnCount, double columnGap, double rowGap)
    {
        return PdfGridBlock.Create(columnCount, columnGap, rowGap);
    }

    /// <summary>
    /// Creates an empty group whose ordered items stay in the same flow area when possible.
    /// </summary>
    /// <returns>The configured keep-with-next group.</returns>
    public PdfKeepWithNextGroup CreateKeepWithNextGroup()
    {
        return new PdfKeepWithNextGroup();
    }

    /// <summary>Creates a generator for linked alphabetical indexes.</summary>
    /// <returns>The configured index generator.</returns>
    public PdfGeneratedIndex CreateGeneratedIndex()
    {
        return new PdfGeneratedIndex();
    }

    /// <summary>Creates a generator for linked lists of figures.</summary>
    /// <returns>The configured figure-list generator.</returns>
    public PdfGeneratedList CreateListOfFigures()
    {
        return new PdfGeneratedList(PdfPublishingMarkerKind.Figure);
    }

    /// <summary>Creates a generator for linked lists of tables.</summary>
    /// <returns>The configured table-list generator.</returns>
    public PdfGeneratedList CreateListOfTables()
    {
        return new PdfGeneratedList(PdfPublishingMarkerKind.Table);
    }

    /// <summary>
    /// Creates a page template that replaces page-number placeholders when drawn.
    /// </summary>
    /// <param name="text">The template text; <c>{page}</c>, <c>{total}</c>, and <c>{running}</c> are supported.</param>
    /// <param name="style">The paragraph style used to draw the text.</param>
    /// <param name="height">The template region height in PDF points.</param>
    /// <returns>The configured page template.</returns>
    public PdfPageTemplate CreatePageTextTemplate(string text, PdfParagraphStyle style, double height)
    {
        return PdfPageTemplate.FromPageText(text, style, height);
    }

    /// <summary>
    /// Creates a page layer that fills its region with a color.
    /// </summary>
    /// <param name="color">The fill color for the layer.</param>
    /// <param name="height">The positive layer height in PDF points.</param>
    /// <returns>The configured page template.</returns>
    public PdfPageTemplate CreateBackgroundTemplate(PdfColor color, double height)
    {
        return PdfPageTemplate.FromBackgroundColor(color, height);
    }

    /// <summary>
    /// Creates a page layer that draws centered rotated watermark text.
    /// </summary>
    /// <param name="text">The watermark text; <c>{page}</c> and <c>{total}</c> are supported.</param>
    /// <param name="style">The text style used to draw the watermark.</param>
    /// <param name="height">The positive layer height in PDF points.</param>
    /// <param name="angle">The watermark rotation angle in degrees.</param>
    /// <returns>The configured page template.</returns>
    public PdfPageTemplate CreateWatermarkTemplate(string text, PdfTextStyle style, double height, double angle)
    {
        return PdfPageTemplate.FromWatermarkText(text, style, height, angle);
    }

    /// <summary>
    /// Creates an absolute-positioned layout element whose frame participates in normal flow.
    /// </summary>
    /// <param name="item">The child layout element.</param>
    /// <param name="x">The non-negative horizontal frame offset.</param>
    /// <param name="y">The non-negative vertical frame offset.</param>
    /// <param name="width">The positive frame width in PDF points.</param>
    /// <param name="height">The positive frame height in PDF points.</param>
    /// <returns>The configured absolute-positioned element.</returns>
    public PdfAbsoluteLayoutElement CreateAbsoluteElement(IPdfLayoutElement item, double x, double y, double width, double height)
    {
        return new PdfAbsoluteLayoutElement(item, x, y, width, height);
    }

    /// <summary>
    /// Creates a reusable ordered content layer.
    /// </summary>
    /// <param name="template">The page template drawn by the layer.</param>
    /// <param name="placement">The layer band relative to normal content.</param>
    /// <param name="order">The stable order within that band.</param>
    /// <returns>The configured content layer.</returns>
    public PdfContentLayer CreateContentLayer(PdfPageTemplate template, PdfContentLayerPlacement placement, int order)
    {
        return new PdfContentLayer(template, placement, order);
    }

    /// <summary>
    /// Creates a reusable semantic frame around layout content.
    /// </summary>
    /// <param name="content">The framed content.</param>
    /// <returns>The configured frame.</returns>
    public PdfFrame CreateFrame(IPdfLayoutElement content)
    {
        return new PdfFrame(content);
    }

    /// <summary>
    /// Creates a reusable semantic caption from layout content.
    /// </summary>
    /// <param name="content">The caption content.</param>
    /// <returns>The configured caption.</returns>
    public PdfCaption CreateCaption(IPdfLayoutElement content)
    {
        return new PdfCaption(content);
    }

    /// <summary>
    /// Creates a reusable figure from content and a semantic caption.
    /// </summary>
    /// <param name="content">The primary figure content.</param>
    /// <param name="caption">The figure caption.</param>
    /// <returns>The configured figure.</returns>
    public PdfFigure CreateFigure(IPdfLayoutElement content, PdfCaption caption)
    {
        return new PdfFigure(content, caption);
    }

    /// <summary>
    /// Creates a reusable semantic sidebar from layout content.
    /// </summary>
    /// <param name="content">The sidebar content.</param>
    /// <returns>The configured sidebar.</returns>
    public PdfSidebar CreateSidebar(IPdfLayoutElement content)
    {
        return new PdfSidebar(content);
    }

    /// <summary>
    /// Creates a reusable semantic callout from layout content.
    /// </summary>
    /// <param name="content">The callout content.</param>
    /// <returns>The configured callout.</returns>
    public PdfCallout CreateCallout(IPdfLayoutElement content)
    {
        return new PdfCallout(content);
    }

    /// <summary>
    /// Creates a labeled fixed-size chart placeholder.
    /// </summary>
    /// <param name="label">The placeholder label.</param>
    /// <param name="width">The positive finite width in PDF points.</param>
    /// <param name="height">The positive finite height in PDF points.</param>
    /// <returns>The configured chart placeholder.</returns>
    public PdfChartPlaceholder CreateChartPlaceholder(string label, double width, double height)
    {
        return new PdfChartPlaceholder(label, width, height);
    }
}
