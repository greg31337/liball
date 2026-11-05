using System;
using System.Globalization;

namespace pdf;

/// <summary>
/// Defines reusable page furniture such as headers, footers, backgrounds, watermarks, and page-number text.
/// </summary>
public class PdfPageTemplate : IPdfPageTemplate
{
    /// <summary>
    /// Stores the item used by this object.
    /// </summary>
    internal IPdfLayoutElement Item = new PdfBlock();
    /// <summary>
    /// Specifies the height in PDF points.
    /// </summary>
    internal double Height;
    /// <summary>
    /// Indicates whether this object has page text.
    /// </summary>
    internal bool HasPageText;
    /// <summary>
    /// Stores the page text used by this object.
    /// </summary>
    internal string PageText = string.Empty;
    /// <summary>
    /// Stores the page text style used by this object.
    /// </summary>
    internal PdfParagraphStyle PageTextStyle = new PdfParagraphStyle();
    /// <summary>
    /// Indicates whether this object has background color.
    /// </summary>
    internal bool HasBackgroundColor;
    /// <summary>
    /// Stores the background color used by this object.
    /// </summary>
    internal PdfColor BackgroundColor = PdfColor.Gray(1);
    /// <summary>
    /// Indicates whether this object has watermark text.
    /// </summary>
    internal bool HasWatermarkText;
    /// <summary>
    /// Stores the watermark text used by this object.
    /// </summary>
    internal string WatermarkText = string.Empty;
    /// <summary>
    /// Stores the watermark text style used by this object.
    /// </summary>
    internal PdfTextStyle WatermarkTextStyle = new PdfTextStyle();
    /// <summary>
    /// Stores the numeric watermark angle used by this object.
    /// </summary>
    internal double WatermarkAngle;

    /// <summary>
    /// Initializes a new page template.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public PdfPageTemplate(IPdfLayoutElement item, double height)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        if (double.IsNaN(height) || double.IsInfinity(height) || height < 0)
        {
            throw new ArgumentOutOfRangeException("height", "Page template height must be finite and nonnegative.");
        }

        Item = item;
        Height = height;
        HasPageText = false;
        PageText = string.Empty;
        PageTextStyle = new PdfParagraphStyle();
        HasBackgroundColor = false;
        BackgroundColor = PdfColor.Gray(1);
        HasWatermarkText = false;
        WatermarkText = string.Empty;
        WatermarkTextStyle = new PdfTextStyle();
        WatermarkAngle = 0;
    }

    /// <summary>
    /// Creates and returns a page template from the supplied block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page template.</returns>
    internal static PdfPageTemplate FromBlock(PdfBlock block, double height)
    {
        return new PdfPageTemplate(block, height);
    }

    /// <summary>
    /// Creates and returns a page template from the supplied text block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page template.</returns>
    internal static PdfPageTemplate FromTextBlock(PdfTextBlock block, double height)
    {
        return new PdfPageTemplate(block, height);
    }

    /// <summary>
    /// Creates and returns a page template from the supplied stack block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page template.</returns>
    internal static PdfPageTemplate FromStackBlock(PdfStackBlock block, double height)
    {
        return new PdfPageTemplate(block, height);
    }

    /// <summary>
    /// Creates and returns a page template from the supplied row block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page template.</returns>
    internal static PdfPageTemplate FromRowBlock(PdfRowBlock block, double height)
    {
        return new PdfPageTemplate(block, height);
    }

    /// <summary>
    /// Creates and returns a page template from the supplied column block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page template.</returns>
    internal static PdfPageTemplate FromColumnBlock(PdfColumnBlock block, double height)
    {
        return new PdfPageTemplate(block, height);
    }

    /// <summary>
    /// Creates and returns a page template from the supplied grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page template.</returns>
    internal static PdfPageTemplate FromGridBlock(PdfGridBlock block, double height)
    {
        return new PdfPageTemplate(block, height);
    }

    /// <summary>
    /// Creates and returns a page template from the supplied table block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page template.</returns>
    internal static PdfPageTemplate FromTableBlock(PdfTableBlock block, double height)
    {
        return new PdfPageTemplate(block, height);
    }

    /// <summary>
    /// Creates and returns a page template from the supplied list block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page template.</returns>
    internal static PdfPageTemplate FromListBlock(PdfListBlock block, double height)
    {
        return new PdfPageTemplate(block, height);
    }

    /// <summary>
    /// Creates and returns a page template from the supplied page text.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page template.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static PdfPageTemplate FromPageText(string text, PdfParagraphStyle style, double height)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        PdfPageTemplate template = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), height);
        template.HasPageText = true;
        template.PageText = text;
        template.PageTextStyle = style;
        return template;
    }

    /// <summary>
    /// Creates and returns a page template from the supplied background color.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting page template.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static PdfPageTemplate FromBackgroundColor(PdfColor color, double height)
    {
        if (color == null)
        {
            throw new ArgumentNullException("color");
        }

        ValidateLayerHeight(height);

        PdfPageTemplate template = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), height);
        template.HasBackgroundColor = true;
        template.BackgroundColor = color;
        return template;
    }

    /// <summary>
    /// Creates and returns a page template from the supplied watermark text.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="angle">The angle to use for this operation.</param>
    /// <returns>The resulting page template.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    internal static PdfPageTemplate FromWatermarkText(string text, PdfTextStyle style, double height, double angle)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        ValidateLayerHeight(height);

        if (double.IsNaN(angle) || double.IsInfinity(angle))
        {
            throw new ArgumentOutOfRangeException("angle", "Watermark angle must be finite.");
        }

        PdfPageTemplate template = new PdfPageTemplate(PdfLayoutItem.FromBlock(new PdfBlock()), height);
        template.HasWatermarkText = true;
        template.WatermarkText = text;
        template.WatermarkTextStyle = style;
        template.WatermarkAngle = angle;
        return template;
    }

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Draw(PdfPage page, double x, double y, double width)
    {
        return DrawForPage(page, x, y, width, 1, 1);
    }

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="page">The PDF page that receives or owns the content.</param>
    /// <param name="x">The horizontal coordinate in PDF points from the page's left edge.</param>
    /// <param name="y">The vertical coordinate in PDF points from the page's bottom edge.</param>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="pageNumber">The one-based page number.</param>
    /// <param name="totalPages">The total number of pages in the document.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawForPage(PdfPage page, double x, double y, double width, int pageNumber, int totalPages)
    {
        return DrawForPageWithRunningHead(page, x, y, width, pageNumber, totalPages, string.Empty);
    }

    internal PdfLayoutResult DrawForPageWithRunningHead(PdfPage page, double x, double y, double width, int pageNumber, int totalPages, string runningHead)
    {
        return DrawPageRegion(page, x, y, width, Height, pageNumber, totalPages, runningHead);
    }

    internal PdfLayoutResult DrawForPageInRegion(PdfPage page, double x, double y, double width, double height, int pageNumber, int totalPages)
    {
        return DrawPageRegion(page, x, y, width, height, pageNumber, totalPages, string.Empty);
    }

    internal PdfLayoutResult DrawPageRegion(PdfPage page, double x, double y, double width, double height, int pageNumber, int totalPages, string runningHead)
    {
        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        if (HasPageText)
        {
            return DrawPageText(page, x, y, width, height, pageNumber, totalPages, runningHead);
        }

        if (HasBackgroundColor)
        {
            return DrawBackgroundColor(page, x, y, width, height);
        }

        if (HasWatermarkText)
        {
            return DrawWatermarkText(page, x, y, width, height, pageNumber, totalPages, runningHead);
        }

        PdfLayoutContext measureContext = new PdfLayoutContext(width, height);
        PdfLayoutResult result = Item.Measure(measureContext);
        double drawY = y + height - result.Size.Height;
        PdfLayoutDrawContext drawContext = new PdfLayoutDrawContext(page, x, drawY, width, result.Size.Height);
        return Item.Draw(drawContext);
    }

    /// <summary>
    /// Determines whether final template pass is required for this operation.
    /// </summary>
    /// <returns>True when the condition is satisfied; otherwise, false.</returns>
    public bool NeedsFinalTemplatePass()
    {
        return HasPageText || HasWatermarkText;
    }

    internal PdfLayoutResult DrawPageText(PdfPage page, double x, double y, double width, double height, int pageNumber, int totalPages, string runningHead)
    {
        string text = ReplacePageTextPlaceholders(PageText, pageNumber, totalPages, runningHead);
        PdfTextBlock block = new PdfTextBlock(text, PageTextStyle);
        PdfLayoutContext measureContext = new PdfLayoutContext(width, height);
        PdfLayoutResult result = block.Measure(measureContext);
        double drawY = y + height - result.Size.Height;
        PdfLayoutDrawContext drawContext = new PdfLayoutDrawContext(page, x, drawY, width, result.Size.Height);
        return block.Draw(drawContext);
    }

    internal PdfLayoutResult DrawBackgroundColor(PdfPage page, double x, double y, double width, double height)
    {
        page.Canvas.SaveState();
        page.Canvas.SetFillColorValue(BackgroundColor);
        page.Canvas.FillRectangle(x, y, width, height);
        page.Canvas.RestoreState();
        return CreateLayerResult(x, y, width, height);
    }

    internal PdfLayoutResult DrawWatermarkText(PdfPage page, double x, double y, double width, double height, int pageNumber, int totalPages, string runningHead)
    {
        string text = ReplacePageTextPlaceholders(WatermarkText, pageNumber, totalPages, runningHead);
        double textWidth = PdfCanvas.MeasureText(text, WatermarkTextStyle);
        double centerX = x + width / 2;
        double centerY = y + height / 2;

        page.Canvas.SaveState();
        page.Canvas.Translate(centerX, centerY);
        page.Canvas.Rotate(WatermarkAngle);
        page.Canvas.DrawStyledText(text, -textWidth / 2, -WatermarkTextStyle.FontSize / 2, WatermarkTextStyle);
        page.Canvas.RestoreState();
        return CreateLayerResult(x, y, width, height);
    }

    internal PdfLayoutResult CreateLayerResult(double x, double y, double width, double height)
    {
        PdfLayoutSize size = new PdfLayoutSize(width, height);
        PdfBox box = PdfBox.FromCoordinates(x, y, width, height);
        return new PdfLayoutResult(size, box, true);
    }

    internal string ReplacePageTextPlaceholders(string text, int pageNumber, int totalPages, string runningHead)
    {
        string value = text.Replace("{page}", pageNumber.ToString(CultureInfo.InvariantCulture));
        value = value.Replace("{total}", totalPages.ToString(CultureInfo.InvariantCulture));
        return value.Replace("{running}", runningHead);
    }

    internal static void ValidateLayerHeight(double height)
    {
        if (double.IsNaN(height) || double.IsInfinity(height) || height <= 0)
        {
            throw new ArgumentOutOfRangeException("height", "Layer height must be finite and positive.");
        }
    }
}
