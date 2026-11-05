using System;

namespace pdf;

/// <summary>
/// Represents a fixed-size rectangular spacer or placeholder in the paginated PDF layout system.
/// </summary>
public class PdfBlock : IPdfLayoutElement, IPdfBlock
{
    /// <summary>
    /// Stores the content size used by this object.
    /// </summary>
    internal PdfLayoutSize ContentSize = new PdfLayoutSize(0, 0);
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
    internal PdfElementStyle Style = new PdfElementStyle();

    /// <summary>
    /// Initializes a new block.
    /// </summary>
    public PdfBlock()
    {
        ContentSize = new PdfLayoutSize(0, 0);
        Padding = PdfInsets.None();
        Border = PdfInsets.None();
        Margin = PdfInsets.None();
        Style = new PdfElementStyle();
    }

    /// <summary>
    /// Creates a fixed-size block.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting block.</returns>
    internal static PdfBlock WithSize(double width, double height)
    {
        PdfBlock block = new PdfBlock();
        block.ContentSize = new PdfLayoutSize(width, height);
        return block;
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

        Style = style.Copy();
        Padding = PdfElementStyle.CopyInsets(Style.Padding);
        Border = PdfElementStyle.CopyInsets(Style.Border);
        Margin = PdfElementStyle.CopyInsets(Style.Margin);
    }

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        ValidateContext(context);

        PdfBox box = CreateMeasuredBox(ContentSize.Width, ContentSize.Height);
        PdfLayoutSize size = box.GetOuterSize();
        bool fits = SizeFits(size, context);

        return new PdfLayoutResult(size, box, fits);
    }

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        ValidateDrawContext(context);

        PdfLayoutResult result = Measure(context.ToLayoutContext());
        Style.DrawBox(context, result, Margin, Border);
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

    internal PdfBox CreateMeasuredBox(double contentWidth, double contentHeight)
    {
        PdfRectangle contentRectangle = new PdfRectangle(0, 0, contentWidth, contentHeight);
        return new PdfBox(contentRectangle, Padding, Border, Margin);
    }

    internal static bool SizeFits(PdfLayoutSize size, PdfLayoutContext context)
    {
        ValidateContext(context);
        if (size == null)
        {
            throw new ArgumentNullException("size");
        }

        if (size.Width > context.AvailableWidth)
        {
            return false;
        }

        if (size.Height > context.AvailableHeight)
        {
            return false;
        }

        return true;
    }

    internal static void ValidateContext(PdfLayoutContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
    }

    internal static void ValidateDrawContext(PdfLayoutDrawContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
    }

    internal static void ValidateInsets(PdfInsets insets, string name)
    {
        if (insets == null)
        {
            throw new ArgumentNullException(name);
        }
    }
}
