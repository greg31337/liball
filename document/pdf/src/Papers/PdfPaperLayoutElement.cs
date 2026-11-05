namespace pdf;

internal class PdfPaperAnchoredTextBlock : IPdfLayoutElement
{
    internal PdfPaper Paper;
    internal PdfTextBlock Block;
    internal string MarkerName;
    internal string MarkerText;

    internal PdfPaperAnchoredTextBlock(PdfPaper paper, PdfTextBlock block, string markerName, string markerText)
    {
        Paper = paper;
        Block = block;
        MarkerName = markerName;
        MarkerText = markerText;
    }

    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        return Block.Measure(context);
    }

    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        PdfLayoutResult result = Block.Draw(context);
        Paper.RegisterCrossReferenceTarget(MarkerName, MarkerText, context.Page, context.X, context.Y + result.Size.Height);
        return result;
    }

    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        return cursor.DrawItem(this);
    }
}

internal class PdfPaperLinkedTextBlock : IPdfLayoutElement
{
    internal PdfPaper Paper;
    internal PdfTextBlock Block;
    internal string MarkerName;

    internal PdfPaperLinkedTextBlock(PdfPaper paper, PdfTextBlock block, string markerName)
    {
        Paper = paper;
        Block = block;
        MarkerName = markerName;
    }

    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        return Block.Measure(context);
    }

    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        PdfLayoutResult result = Block.Draw(context);
        Paper.RegisterPendingLink(MarkerName, context.Page, context.X, context.Y, context.AvailableWidth, result.Size.Height);
        return result;
    }

    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        return cursor.DrawItem(this);
    }
}

internal class PdfPaperPublishingTextBlock : IPdfLayoutElement
{
    internal PdfPaper Paper;
    internal PdfTextBlock Block;
    internal string Title;
    internal PdfPublishingMarkerKind Kind;

    internal PdfPaperPublishingTextBlock(PdfPaper paper, PdfTextBlock block, string title, PdfPublishingMarkerKind kind)
    {
        Paper = paper;
        Block = block;
        Title = title;
        Kind = kind;
    }

    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        return Block.Measure(context);
    }

    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        PdfLayoutResult result = Block.Draw(context);
        Paper.RegisterPublishingTarget(Kind, Title, context.Page, context.X, context.Y + result.Size.Height);
        return result;
    }

    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        return cursor.DrawItem(this);
    }
}

internal class PdfPaperPendingLink
{
    internal string MarkerName;
    internal PdfPage Page;
    internal PdfRectangle Rectangle;

    internal PdfPaperPendingLink(string markerName, PdfPage page, PdfRectangle rectangle)
    {
        MarkerName = markerName;
        Page = page;
        Rectangle = rectangle;
    }
}

internal class PdfPaperTwoColumnSection
{
    internal string Heading;
    internal string[] Paragraphs;
    internal double ColumnGap;

    internal PdfPaperTwoColumnSection(string heading, string[] paragraphs, double columnGap)
    {
        Heading = heading;
        Paragraphs = paragraphs;
        ColumnGap = columnGap;
    }
}
