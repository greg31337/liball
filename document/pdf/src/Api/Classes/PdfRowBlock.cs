using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents a row element in the paginated PDF layout system.
/// </summary>
public class PdfRowBlock : IPdfLayoutElement, IPdfRowBlock
{
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
    /// Initializes a new row block.
    /// </summary>
    public PdfRowBlock()
    {
        Initialize(0);
    }

    /// <summary>
    /// Creates a row block with explicit item spacing.
    /// </summary>
    /// <param name="spacing">The spacing to use for this operation.</param>
    /// <returns>The resulting row block.</returns>
    internal static PdfRowBlock WithSpacing(double spacing)
    {
        PdfRowBlock block = new PdfRowBlock();
        block.Initialize(spacing);
        return block;
    }

    /// <summary>
    /// Adds a block to this row block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddBlock(PdfBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a text block to this row block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddTextBlock(PdfTextBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a stack block to this row block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddStackBlock(PdfStackBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a row block to this row block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddRowBlock(PdfRowBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a column block to this row block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddColumnBlock(PdfColumnBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a grid block to this row block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddGridBlock(PdfGridBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a table block to this row block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddTableBlock(PdfTableBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a list block to this row block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddListBlock(PdfListBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a layout element to this row block.
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
        double contentWidth = 0;
        double contentHeight = 0;
        bool childrenFit = true;

        for (int index = 0; index < _items.Count; index++)
        {
            double remainingWidth = contentAvailableWidth - contentWidth;
            if (index > 0)
            {
                remainingWidth -= Spacing;
            }

            if (remainingWidth < 0)
            {
                remainingWidth = 0;
            }

            PdfLayoutContext childContext = new PdfLayoutContext(remainingWidth, contentAvailableHeight);
            PdfLayoutResult childResult = _items[index].Measure(childContext);
            if (!childResult.Fits)
            {
                childrenFit = false;
            }

            if (index > 0)
            {
                contentWidth += Spacing;
            }

            contentWidth += childResult.Size.Width;
            if (childResult.Size.Height > contentHeight)
            {
                contentHeight = childResult.Size.Height;
            }
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
        double currentX = contentX;

        for (int index = 0; index < _items.Count; index++)
        {
            double remainingWidth = result.Box.ContentRectangle.Width - (currentX - contentX);
            if (remainingWidth < 0)
            {
                remainingWidth = 0;
            }

            PdfLayoutContext childMeasureContext = new PdfLayoutContext(remainingWidth, result.Box.ContentRectangle.Height);
            PdfLayoutResult childResult = _items[index].Measure(childMeasureContext);
            double childY = contentY + result.Box.ContentRectangle.Height - childResult.Size.Height;
            PdfLayoutDrawContext childContext = new PdfLayoutDrawContext(
                context.Page,
                currentX,
                childY,
                childResult.Size.Width,
                childResult.Size.Height);
            _items[index].Draw(childContext);
            currentX += childResult.Size.Width + Spacing;
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
        if (width <= 0)
        {
            throw new ArgumentException("Row block insets must leave positive content width.");
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

    internal void Initialize(double spacing)
    {
        if (double.IsNaN(spacing) || double.IsInfinity(spacing) || spacing < 0)
        {
            throw new ArgumentOutOfRangeException("spacing", "Row spacing cannot be negative.");
        }

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
            throw new ArgumentOutOfRangeException("spacing", "Row spacing must be non-negative and finite.");
        }
    }
}
