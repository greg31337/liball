using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Represents a grid element in the paginated PDF layout system.
/// </summary>
public class PdfGridBlock : IPdfLayoutElement, IPdfGridBlock
{
    /// <summary>
    /// Stores the numeric column count used by this object.
    /// </summary>
    internal int ColumnCount;
    /// <summary>
    /// Specifies the column spacing in PDF points.
    /// </summary>
    internal double ColumnSpacing;
    /// <summary>
    /// Specifies the row spacing in PDF points.
    /// </summary>
    internal double RowSpacing;
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
    /// Initializes a new grid block.
    /// </summary>
    public PdfGridBlock()
    {
        Initialize(1, 0, 0);
    }

    /// <summary>
    /// Creates a grid block with an explicit column count.
    /// </summary>
    /// <param name="columnCount">The column count to use for this operation.</param>
    /// <returns>The resulting grid block.</returns>
    internal static PdfGridBlock WithColumnCount(int columnCount)
    {
        return Create(columnCount, 0, 0);
    }

    /// <summary>
    /// Creates a grid block with an explicit column count.
    /// </summary>
    /// <param name="columnCount">The column count to use for this operation.</param>
    /// <returns>The resulting grid block.</returns>
    internal static PdfGridBlock Create(int columnCount)
    {
        return WithColumnCount(columnCount);
    }

    /// <summary>
    /// Creates a grid block with explicit dimensions.
    /// </summary>
    /// <param name="columnCount">The column count to use for this operation.</param>
    /// <param name="columnSpacing">The column spacing to use for this operation.</param>
    /// <param name="rowSpacing">The row spacing to use for this operation.</param>
    /// <returns>The resulting grid block.</returns>
    internal static PdfGridBlock Create(int columnCount, double columnSpacing, double rowSpacing)
    {
        PdfGridBlock block = new PdfGridBlock();
        block.Initialize(columnCount, columnSpacing, rowSpacing);
        return block;
    }

    /// <summary>
    /// Adds a block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddBlock(PdfBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a text block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddTextBlock(PdfTextBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a stack block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddStackBlock(PdfStackBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a row block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddRowBlock(PdfRowBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a column block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddColumnBlock(PdfColumnBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a grid block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddGridBlock(PdfGridBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a table block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddTableBlock(PdfTableBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a list block to this grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    public void AddListBlock(PdfListBlock block)
    {
        AddElement(block);
    }

    /// <summary>
    /// Adds a layout element to this grid block.
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
    /// Sets the horizontal and vertical gaps between grid children.
    /// </summary>
    /// <param name="columnSpacing">The non-negative horizontal gap.</param>
    /// <param name="rowSpacing">The non-negative vertical gap.</param>
    public void SetGaps(double columnSpacing, double rowSpacing)
    {
        ValidateSpacing(columnSpacing, "columnSpacing");
        ValidateSpacing(rowSpacing, "rowSpacing");
        ColumnSpacing = columnSpacing;
        RowSpacing = rowSpacing;
    }

    /// <summary>
    /// Applies a defensive copy of a reusable element style and uses item spacing for both grid gaps.
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
        ColumnSpacing = Style.ItemSpacing;
        RowSpacing = Style.ItemSpacing;
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
        double cellWidth = GetCellWidth(contentAvailableWidth);
        double contentHeight = 0;
        bool childrenFit = true;
        int rowCount = GetRowCount();

        for (int row = 0; row < rowCount; row++)
        {
            double rowHeight = MeasureRowHeight(row, cellWidth, contentAvailableHeight - contentHeight);
            if (row > 0)
            {
                contentHeight += RowSpacing;
            }

            contentHeight += rowHeight;

            int firstItemIndex = row * ColumnCount;
            int itemsInRow = GetItemCountInRow(firstItemIndex);
            for (int column = 0; column < itemsInRow; column++)
            {
                int itemIndex = firstItemIndex + column;
                PdfLayoutContext childContext = new PdfLayoutContext(cellWidth, rowHeight);
                PdfLayoutResult childResult = _items[itemIndex].Measure(childContext);
                if (!childResult.Fits)
                {
                    childrenFit = false;
                }
            }
        }

        PdfBox box = CreateMeasuredBox(contentAvailableWidth, contentHeight);
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
        double cellWidth = GetCellWidth(result.Box.ContentRectangle.Width);
        double currentTop = contentY + result.Box.ContentRectangle.Height;
        int rowCount = GetRowCount();

        for (int row = 0; row < rowCount; row++)
        {
            double rowHeight = MeasureRowHeight(row, cellWidth, currentTop - contentY);
            int firstItemIndex = row * ColumnCount;
            int itemsInRow = GetItemCountInRow(firstItemIndex);
            for (int column = 0; column < itemsInRow; column++)
            {
                int itemIndex = firstItemIndex + column;
                PdfLayoutContext childMeasureContext = new PdfLayoutContext(cellWidth, rowHeight);
                PdfLayoutResult childResult = _items[itemIndex].Measure(childMeasureContext);
                double childX = contentX + column * (cellWidth + ColumnSpacing);
                double childY = currentTop - childResult.Size.Height;
                PdfLayoutDrawContext childContext = new PdfLayoutDrawContext(
                    context.Page,
                    childX,
                    childY,
                    cellWidth,
                    rowHeight);
                _items[itemIndex].Draw(childContext);
            }

            currentTop -= rowHeight + RowSpacing;
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

    /// <summary>
    /// Returns the row count calculated from the object's current state.
    /// </summary>
    /// <returns>The calculated integer value.</returns>
    public int GetRowCount()
    {
        if (_items.Count == 0)
        {
            return 0;
        }

        return (_items.Count - 1) / ColumnCount + 1;
    }

    internal double MeasureRowHeight(int row, double cellWidth, double availableHeight)
    {
        double height = 0;
        double remainingHeight = availableHeight;
        if (remainingHeight < 0)
        {
            remainingHeight = 0;
        }

        int firstItemIndex = row * ColumnCount;
        int itemsInRow = GetItemCountInRow(firstItemIndex);
        for (int column = 0; column < itemsInRow; column++)
        {
            int itemIndex = firstItemIndex + column;
            PdfLayoutContext childContext = new PdfLayoutContext(cellWidth, remainingHeight);
            PdfLayoutResult childResult = _items[itemIndex].Measure(childContext);
            if (childResult.Size.Height > height)
            {
                height = childResult.Size.Height;
            }
        }

        return height;
    }

    internal int GetItemCountInRow(int firstItemIndex)
    {
        int count = _items.Count - firstItemIndex;
        if (count > ColumnCount)
        {
            return ColumnCount;
        }

        return count;
    }

    internal double GetCellWidth(double contentWidth)
    {
        double spacingWidth = (ColumnCount - 1) * ColumnSpacing;
        double width = (contentWidth - spacingWidth) / ColumnCount;
        if (width <= 0)
        {
            throw new ArgumentException("Grid spacing must leave positive cell width.");
        }

        return width;
    }

    internal double GetContentAvailableWidth(PdfLayoutContext context)
    {
        double width = context.AvailableWidth - Padding.GetHorizontal() - Border.GetHorizontal() - Margin.GetHorizontal();
        if (width <= 0)
        {
            throw new ArgumentException("Grid block insets must leave positive content width.");
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

    internal void Initialize(int columnCount, double columnSpacing, double rowSpacing)
    {
        if (columnCount <= 0)
        {
            throw new ArgumentOutOfRangeException("columnCount", "Grid column count must be positive.");
        }

        if (double.IsNaN(columnSpacing) || double.IsInfinity(columnSpacing) || columnSpacing < 0)
        {
            throw new ArgumentOutOfRangeException("columnSpacing", "Grid column spacing cannot be negative.");
        }

        if (double.IsNaN(rowSpacing) || double.IsInfinity(rowSpacing) || rowSpacing < 0)
        {
            throw new ArgumentOutOfRangeException("rowSpacing", "Grid row spacing cannot be negative.");
        }

        ColumnCount = columnCount;
        ColumnSpacing = columnSpacing;
        RowSpacing = rowSpacing;
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

    internal static void ValidateSpacing(double spacing, string name)
    {
        if (double.IsNaN(spacing) || double.IsInfinity(spacing) || spacing < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Grid spacing must be non-negative and finite.");
        }
    }
}
