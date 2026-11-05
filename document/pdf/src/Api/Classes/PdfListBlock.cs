using System;
using System.Collections.Generic;
using System.Globalization;

namespace pdf;

/// <summary>
/// Represents a list element in the paginated PDF layout system.
/// </summary>
public class PdfListBlock : IPdfLayoutElement, IPdfListBlock
{
    /// <summary>
    /// Stores the marker style used by this object.
    /// </summary>
    internal PdfListMarkerStyle MarkerStyle;
    /// <summary>
    /// Stores the numeric start number used by this object.
    /// </summary>
    internal int StartNumber;
    /// <summary>
    /// Specifies the marker width in PDF points.
    /// </summary>
    internal double MarkerWidth;
    /// <summary>
    /// Specifies the marker gap in PDF points.
    /// </summary>
    internal double MarkerGap;
    /// <summary>
    /// Specifies the spacing in PDF points.
    /// </summary>
    internal double Spacing;
    /// <summary>
    /// Stores the marker text style used by this object.
    /// </summary>
    internal PdfTextStyle MarkerTextStyle = new PdfTextStyle();
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
    internal List<PdfListItem> _items = new List<PdfListItem>();
    /// <summary>
    /// Exposes the items currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfListItem> Items = new List<PdfListItem>();

    /// <summary>
    /// Initializes a new list block.
    /// </summary>
    public PdfListBlock()
    {
        Initialize(PdfListMarkerStyle.Bullet);
    }

    /// <summary>
    /// Creates a list block with an explicit marker style.
    /// </summary>
    /// <param name="markerStyle">The marker style to use for this operation.</param>
    /// <returns>The resulting list block.</returns>
    internal static PdfListBlock WithMarkerStyle(PdfListMarkerStyle markerStyle)
    {
        PdfListBlock block = new PdfListBlock();
        block.Initialize(markerStyle);
        return block;
    }

    /// <summary>
    /// Adds a block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfListItem AddBlock(PdfBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a text block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfListItem AddTextBlock(PdfTextBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a stack block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfListItem AddStackBlock(PdfStackBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a row block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfListItem AddRowBlock(PdfRowBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a column block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfListItem AddColumnBlock(PdfColumnBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a grid block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfListItem AddGridBlock(PdfGridBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a table block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfListItem AddTableBlock(PdfTableBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a list block to this list block and returns the stored value.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>The object that was added to the owning document or collection.</returns>
    public PdfListItem AddListBlock(PdfListBlock block)
    {
        return AddElement(block);
    }

    /// <summary>
    /// Adds a layout element to this list block and returns its list item.
    /// </summary>
    /// <param name="element">The layout element to add.</param>
    /// <returns>The list item that contains the element.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfListItem AddElement(IPdfLayoutElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        return AddLayoutItem(element);
    }

    /// <summary>
    /// Adds an item to this list block.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddItem(PdfListItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        _items.Add(item);
    }

    /// <summary>
    /// Sets the marker style used for subsequent list rendering.
    /// </summary>
    /// <param name="markerStyle">The marker style to use for this operation.</param>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    public void SetMarkerStyle(PdfListMarkerStyle markerStyle)
    {
        ValidateMarkerStyle(markerStyle);
        MarkerStyle = markerStyle;
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
    /// Sets the spacing used for subsequent PDF generation.
    /// </summary>
    /// <param name="spacing">The spacing to use for this operation.</param>
    public void SetSpacing(double spacing)
    {
        ValidateNotNegative(spacing, "spacing", "List spacing cannot be negative.");
        Spacing = spacing;
    }

    /// <summary>
    /// Sets the marker width used for subsequent PDF generation.
    /// </summary>
    /// <param name="markerWidth">The marker width in PDF points.</param>
    public void SetMarkerWidth(double markerWidth)
    {
        ValidateNotNegative(markerWidth, "markerWidth", "List marker width cannot be negative.");
        MarkerWidth = markerWidth;
    }

    /// <summary>
    /// Sets the marker gap used for subsequent PDF generation.
    /// </summary>
    /// <param name="markerGap">The marker gap to use for this operation.</param>
    public void SetMarkerGap(double markerGap)
    {
        ValidateNotNegative(markerGap, "markerGap", "List marker gap cannot be negative.");
        MarkerGap = markerGap;
    }

    /// <summary>
    /// Sets the start number used for subsequent PDF generation.
    /// </summary>
    /// <param name="startNumber">The start number to use for this operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public void SetStartNumber(int startNumber)
    {
        if (startNumber <= 0)
        {
            throw new ArgumentOutOfRangeException("startNumber", "List start number must be positive.");
        }

        StartNumber = startNumber;
    }

    /// <summary>
    /// Sets the marker text style used for subsequent PDF generation.
    /// </summary>
    /// <param name="markerTextStyle">The marker text style to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetMarkerTextStyle(PdfTextStyle markerTextStyle)
    {
        if (markerTextStyle == null)
        {
            throw new ArgumentNullException("markerTextStyle");
        }

        MarkerTextStyle = markerTextStyle;
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
        double itemAvailableWidth = GetItemAvailableWidth(contentAvailableWidth);
        double contentAvailableHeight = GetContentAvailableHeight(context);
        double contentWidth = 0;
        double contentHeight = 0;
        bool childrenFit = true;

        for (int index = 0; index < _items.Count; index++)
        {
            double remainingHeight = contentAvailableHeight - contentHeight;
            if (remainingHeight < 0)
            {
                remainingHeight = 0;
            }

            PdfLayoutContext itemContext = new PdfLayoutContext(itemAvailableWidth, remainingHeight);
            PdfLayoutResult itemResult = _items[index].Item.Measure(itemContext);
            if (!itemResult.Fits)
            {
                childrenFit = false;
            }

            double itemHeight = GetListItemHeight(itemResult);
            double itemWidth = MarkerWidth + MarkerGap + itemResult.Size.Width;
            if (itemWidth > contentWidth)
            {
                contentWidth = itemWidth;
            }

            if (index > 0)
            {
                contentHeight += Spacing;
            }

            contentHeight += itemHeight;
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
        double itemAvailableWidth = GetItemAvailableWidth(result.Box.ContentRectangle.Width);
        double currentTop = contentY + result.Box.ContentRectangle.Height;

        for (int index = 0; index < _items.Count; index++)
        {
            PdfLayoutContext itemMeasureContext = new PdfLayoutContext(itemAvailableWidth, currentTop - contentY);
            PdfLayoutResult itemResult = _items[index].Item.Measure(itemMeasureContext);
            double itemHeight = GetListItemHeight(itemResult);
            string marker = GetMarkerText(index);
            double markerY = currentTop - MarkerTextStyle.FontSize;
            double itemX = contentX + MarkerWidth + MarkerGap;
            double itemY = currentTop - itemResult.Size.Height;

            context.Page.Canvas.DrawStyledText(marker, contentX, markerY, MarkerTextStyle);
            PdfLayoutDrawContext itemContext = new PdfLayoutDrawContext(
                context.Page,
                itemX,
                itemY,
                itemAvailableWidth,
                itemResult.Size.Height);
            _items[index].Item.Draw(itemContext);
            currentTop -= itemHeight + Spacing;
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

    internal double GetListItemHeight(PdfLayoutResult itemResult)
    {
        double height = itemResult.Size.Height;
        if (MarkerTextStyle.FontSize > height)
        {
            height = MarkerTextStyle.FontSize;
        }

        return height;
    }

    internal string GetMarkerText(int itemIndex)
    {
        if (MarkerStyle == PdfListMarkerStyle.Bullet)
        {
            return "*";
        }

        if (MarkerStyle == PdfListMarkerStyle.Numbered)
        {
            long number = (long)StartNumber + itemIndex;
            return number.ToString(CultureInfo.InvariantCulture) + ".";
        }

        throw new ArgumentException("List marker style is not supported.", "markerStyle");
    }

    internal double GetContentAvailableWidth(PdfLayoutContext context)
    {
        double width = context.AvailableWidth - Padding.GetHorizontal() - Border.GetHorizontal() - Margin.GetHorizontal();
        if (width <= 0)
        {
            throw new ArgumentException("List block insets must leave positive content width.");
        }

        return width;
    }

    internal double GetItemAvailableWidth(double contentAvailableWidth)
    {
        double width = contentAvailableWidth - MarkerWidth - MarkerGap;
        if (width <= 0)
        {
            throw new ArgumentException("List marker area must leave positive item width.");
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

    internal PdfListItem AddLayoutItem(IPdfLayoutElement item)
    {
        PdfListItem listItem = new PdfListItem(item);
        _items.Add(listItem);
        return listItem;
    }

    internal void Initialize(PdfListMarkerStyle markerStyle)
    {
        ValidateMarkerStyle(markerStyle);

        MarkerStyle = markerStyle;
        StartNumber = 1;
        MarkerWidth = 12;
        MarkerGap = 4;
        Spacing = 0;
        MarkerTextStyle = new PdfTextStyle();
        Padding = PdfInsets.None();
        Border = PdfInsets.None();
        Margin = PdfInsets.None();
        Style = new PdfElementStyle();
        _items = new List<PdfListItem>();
        Items = _items;
    }

    internal static void ValidateMarkerStyle(PdfListMarkerStyle markerStyle)
    {
        if (markerStyle == PdfListMarkerStyle.Bullet)
        {
            return;
        }

        if (markerStyle == PdfListMarkerStyle.Numbered)
        {
            return;
        }

        throw new ArgumentException("List marker style is not supported.", "markerStyle");
    }

    internal static void ValidateInsets(PdfInsets insets, string name)
    {
        if (insets == null)
        {
            throw new ArgumentNullException(name);
        }
    }

    internal static void ValidateNotNegative(double value, string name, string message)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, message);
        }
    }
}
