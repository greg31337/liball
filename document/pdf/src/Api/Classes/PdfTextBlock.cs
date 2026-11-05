using System;

namespace pdf;

/// <summary>
/// Represents a text element in the paginated PDF layout system.
/// </summary>
public class PdfTextBlock : IPdfLayoutElement, IPdfTextBlock
{
    /// <summary>
    /// Stores the text used by this object.
    /// </summary>
    internal string Text = string.Empty;
    /// <summary>
    /// Stores the style used by this object.
    /// </summary>
    internal PdfParagraphStyle Style = new PdfParagraphStyle();
    /// <summary>
    /// Stores the padding used by this object.
    /// </summary>
    internal PdfInsets Padding = PdfInsets.None();
    /// <summary>
    /// Stores the border used by this object.
    /// </summary>
    internal PdfInsets Border = PdfInsets.None();
    /// <summary>
    /// Stores the margin used by this object.
    /// </summary>
    internal PdfInsets Margin = PdfInsets.None();
    internal PdfElementStyle ElementStyle = new PdfElementStyle();

    /// <summary>
    /// Initializes a new text block.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <param name="style">The text or paragraph style that controls measurement and drawing.</param>
    public PdfTextBlock(string text, PdfParagraphStyle style)
    {
        Initialize(text, style);
    }

    /// <summary>
    /// Creates a text block with the default paragraph style.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <returns>The resulting text block.</returns>
    internal static PdfTextBlock Plain(string text)
    {
        return new PdfTextBlock(text, new PdfParagraphStyle());
    }

    /// <summary>
    /// Sets the padding used for subsequent PDF generation.
    /// </summary>
    /// <param name="padding">The padding to use for this operation.</param>
    public void SetPadding(PdfInsets padding)
    {
        ValidateInsets(padding, "padding");
        Padding = padding;
    }

    /// <summary>
    /// Sets the border used for subsequent PDF generation.
    /// </summary>
    /// <param name="border">The border to use for this operation.</param>
    public void SetBorder(PdfInsets border)
    {
        ValidateInsets(border, "border");
        Border = border;
    }

    /// <summary>
    /// Sets the margin used for subsequent PDF generation.
    /// </summary>
    /// <param name="margin">The margin to use for this operation.</param>
    public void SetMargin(PdfInsets margin)
    {
        ValidateInsets(margin, "margin");
        Margin = margin;
    }

    /// <summary>
    /// Applies a defensive copy of a reusable element style.
    /// </summary>
    /// <param name="style">The element style to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when style is null.</exception>
    public void SetStyle(PdfElementStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        ElementStyle = style.Copy();
        Padding = PdfElementStyle.CopyInsets(ElementStyle.Padding);
        Border = PdfElementStyle.CopyInsets(ElementStyle.Border);
        Margin = PdfElementStyle.CopyInsets(ElementStyle.Margin);
    }

    /// <summary>
    /// Replaces the paragraph style used to measure and draw this text block.
    /// </summary>
    /// <param name="style">The paragraph style to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when style is null.</exception>
    public void SetParagraphStyle(PdfParagraphStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        Style = style.Copy();
    }

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        PdfBlock.ValidateContext(context);

        double contentWidth = context.AvailableWidth - Padding.GetHorizontal() - Border.GetHorizontal() - Margin.GetHorizontal();
        if (contentWidth <= 0)
        {
            throw new ArgumentException("Text block insets must leave positive content width.");
        }

        string[] lines = PdfTextWrapper.WrapText(Text, contentWidth, Style.TextStyle);
        double contentHeight = Style.SpacingBefore + Style.SpacingAfter + lines.Length * Style.LineHeight;
        PdfRectangle contentRectangle = new PdfRectangle(0, 0, contentWidth, contentHeight);
        PdfBox box = new PdfBox(contentRectangle, Padding, Border, Margin);
        PdfLayoutSize size = box.GetOuterSize();
        bool fits = PdfBlock.SizeFits(size, context);

        return PdfLayoutResult.WithLines(size, box, fits, lines);
    }

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        PdfBlock.ValidateDrawContext(context);

        PdfLayoutResult result = Measure(context.ToLayoutContext());
        ElementStyle.DrawBox(context, result, Margin, Border);
        double contentX = context.X + Margin.Left + Border.Left + Padding.Left;
        double contentY = context.Y + Margin.Bottom + Border.Bottom + Padding.Bottom;
        double baseline = contentY + result.Box.ContentRectangle.Height - Style.SpacingBefore - Style.TextStyle.FontSize;

        for (int index = 0; index < result.Lines.Length; index++)
        {
            double lineX = contentX;
            double lineWidth = result.Box.ContentRectangle.Width;
            if (index == 0 && Style.FirstLineIndent > 0)
            {
                lineX += Style.FirstLineIndent;
                lineWidth -= Style.FirstLineIndent;
                if (lineWidth < 0)
                {
                    lineWidth = 0;
                }
            }

            context.Page.Canvas.DrawAlignedStyledText(result.Lines[index], lineX, baseline, lineWidth, Style.TextStyle, Style.Alignment);
            baseline -= Style.LineHeight;
        }

        return result;
    }

    /// <summary>
    /// Flows this layout element through the supplied page cursor.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        return cursor.DrawItem(this);
    }

    internal void Initialize(string text, PdfParagraphStyle style)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        Text = text;
        Style = style;
        Padding = PdfInsets.None();
        Border = PdfInsets.None();
        Margin = PdfInsets.None();
        ElementStyle = new PdfElementStyle();
    }

    internal static void ValidateInsets(PdfInsets insets, string name)
    {
        if (insets == null)
        {
            throw new ArgumentNullException(name);
        }
    }
}
