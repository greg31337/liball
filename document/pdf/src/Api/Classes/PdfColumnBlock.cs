using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents a column element in the paginated PDF layout system.
/// </summary>
public class PdfColumnBlock : IPdfLayoutElement, IPdfColumnBlock
{
    /// <summary>
    /// Specifies the width in PDF points.
    /// </summary>
    internal double Width;
    /// <summary>
    /// Specifies the spacing in PDF points.
    /// </summary>
    internal double Spacing;
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
    internal List<IPdfLayoutElement> _items = new List<IPdfLayoutElement>();
    /// <summary>
    /// Exposes the items currently stored by this object.
    /// </summary>
    internal IReadOnlyList<IPdfLayoutElement> Items = new List<IPdfLayoutElement>();

    /// <summary>
    /// Initializes a new column block.
    /// </summary>
    public PdfColumnBlock()
    {
        Initialize(0, 0);
    }

    /// <summary>
    /// Creates a fixed-width column block.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting column block.</returns>
    internal static PdfColumnBlock WithWidth(double width)
    {
        return Create(width, 0);
    }

    /// <summary>
    /// Creates a fixed-width column block.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <returns>The resulting column block.</returns>
    internal static PdfColumnBlock Create(double width)
    {
        return WithWidth(width);
    }

    /// <summary>
    /// Creates a column block with explicit width and item spacing.
    /// </summary>
    /// <param name="width">The width in PDF points; it must be positive unless the method states otherwise.</param>
    /// <param name="spacing">The spacing to use for this operation.</param>
    /// <returns>The resulting column block.</returns>
    internal static PdfColumnBlock Create(double width, double spacing)
    {
        PdfColumnBlock block = new PdfColumnBlock();
        block.Initialize(width, spacing);
        return block;
    }

    /// <summary>
    /// Adds a block to this column block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddBlock(PdfBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a text block to this column block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddTextBlock(PdfTextBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a stack block to this column block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddStackBlock(PdfStackBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a row block to this column block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddRowBlock(PdfRowBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a column block to this column block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddColumnBlock(PdfColumnBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a grid block to this column block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddGridBlock(PdfGridBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a table block to this column block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddTableBlock(PdfTableBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a list block to this column block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddListBlock(PdfListBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a layout element to this column block.
    /// </summary>
    /// <param name="element">The layout element to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddElement(IPdfLayoutElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        _items.Add(element);
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
    /// Sets spacing between adjacent child elements.
    /// </summary>
    /// <param name="spacing">The non-negative spacing in PDF points.</param>
    public void SetSpacing(double spacing)
    {
        ValidateSpacing(spacing);
        Spacing = spacing;
    }

    /// <summary>
    /// Applies a defensive copy of a reusable element style, including item spacing.
    /// </summary>
    /// <param name="style">The element style to apply.</param>
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
        Spacing = Style.ItemSpacing;
    }

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        PdfBlock.ValidateContext(context);

        double contentAvailableWidth = GetContentAvailableWidth(context);
        double contentAvailableHeight = GetContentAvailableHeight(context);
        double contentWidth = Width;
        double contentHeight = 0;
        bool childrenFit = true;

        for (int index = 0; index < _items.Count; index++)
        {
            double remainingHeight = contentAvailableHeight - contentHeight;
            if (remainingHeight < 0)
            {
                remainingHeight = 0;
            }

            PdfLayoutContext childContext = new PdfLayoutContext(contentAvailableWidth, remainingHeight);
            PdfLayoutResult childResult = _items[index].Measure(childContext);
            if (!childResult.Fits)
            {
                childrenFit = false;
            }

            if (Width == 0 && childResult.Size.Width > contentWidth)
            {
                contentWidth = childResult.Size.Width;
            }

            if (index > 0)
            {
                contentHeight += Spacing;
            }

            contentHeight += childResult.Size.Height;
        }

        PdfBox box = CreateMeasuredBox(contentWidth, contentHeight);
        PdfLayoutSize size = box.GetOuterSize();
        bool fits = PdfBlock.SizeFits(size, context) && childrenFit;

        return new PdfLayoutResult(size, box, fits);
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
        Style.DrawBox(context, result, Margin, Border);
        double contentX = context.X + Margin.Left + Border.Left + Padding.Left;
        double contentY = context.Y + Margin.Bottom + Border.Bottom + Padding.Bottom;
        double currentTop = contentY + result.Box.ContentRectangle.Height;

        for (int index = 0; index < _items.Count; index++)
        {
            PdfLayoutContext childMeasureContext = new PdfLayoutContext(result.Box.ContentRectangle.Width, currentTop - contentY);
            PdfLayoutResult childResult = _items[index].Measure(childMeasureContext);
            double childY = currentTop - childResult.Size.Height;
            PdfLayoutDrawContext childContext = new PdfLayoutDrawContext(
                context.Page,
                contentX,
                childY,
                result.Box.ContentRectangle.Width,
                childResult.Size.Height);
            _items[index].Draw(childContext);
            currentTop = childY - Spacing;
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

    internal PdfBox CreateMeasuredBox(double contentWidth, double contentHeight)
    {
        PdfRectangle contentRectangle = new PdfRectangle(0, 0, contentWidth, contentHeight);
        return new PdfBox(contentRectangle, Padding, Border, Margin);
    }

    internal double GetContentAvailableWidth(PdfLayoutContext context)
    {
        double width = context.AvailableWidth - Padding.GetHorizontal() - Border.GetHorizontal() - Margin.GetHorizontal();
        if (Width > 0)
        {
            width = Width;
        }

        if (width <= 0)
        {
            throw new ArgumentException("Column block width must be positive.");
        }

        return width;
    }

    internal double GetContentAvailableHeight(PdfLayoutContext context)
    {
        double height = context.AvailableHeight - Padding.GetVertical() - Border.GetVertical() - Margin.GetVertical();
        if (height < 0)
        {
            return 0;
        }

        return height;
    }

    internal void Initialize(double width, double spacing)
    {
        if (double.IsNaN(width) || double.IsInfinity(width) || width < 0)
        {
            throw new ArgumentOutOfRangeException("width", "Column width cannot be negative.");
        }

        if (double.IsNaN(spacing) || double.IsInfinity(spacing) || spacing < 0)
        {
            throw new ArgumentOutOfRangeException("spacing", "Column spacing cannot be negative.");
        }

        Width = width;
        Spacing = spacing;
        Padding = PdfInsets.None();
        Border = PdfInsets.None();
        Margin = PdfInsets.None();
        Style = new PdfElementStyle();
        _items = new List<IPdfLayoutElement>();
        Items = _items;
    }

    internal static void ValidateInsets(PdfInsets insets, string name)
    {
        if (insets == null)
        {
            throw new ArgumentNullException(name);
        }
    }

    internal static void ValidateSpacing(double spacing)
    {
        if (double.IsNaN(spacing) || double.IsInfinity(spacing) || spacing < 0)
        {
            throw new ArgumentOutOfRangeException("spacing", "Column spacing must be non-negative and finite.");
        }
    }
}
