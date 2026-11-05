using System;

namespace pdf;

/// <summary>
/// Represents a table cell used by the PDF generation API.
/// </summary>
public class PdfTableCell : IPdfTableCell
{
    /// <summary>
    /// Stores the item used by this object.
    /// </summary>
    internal IPdfLayoutElement Item = new PdfBlock();
    /// <summary>
    /// Stores the padding used by this object.
    /// </summary>
    internal PdfInsets Padding = PdfInsets.None();
    /// <summary>
    /// Stores the border used by this object.
    /// </summary>
    internal PdfInsets Border = PdfInsets.None();
    /// <summary>
    /// Indicates whether this object has background.
    /// </summary>
    internal bool HasBackground;
    /// <summary>
    /// Stores the background color used by this object.
    /// </summary>
    internal PdfColor BackgroundColor = PdfColor.Gray(1);
    /// <summary>
    /// Stores the border color used by this object.
    /// </summary>
    internal PdfColor BorderColor = PdfColor.Gray(0);
    internal PdfElementStyle Style = new PdfElementStyle();
    internal bool UsesReusableStyle;
    internal int ColumnSpan;
    internal int RowSpan;

    /// <summary>
    /// Initializes a new table cell.
    /// </summary>
    /// <param name="item">The item to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfTableCell(IPdfLayoutElement item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        Item = item;
        Padding = PdfInsets.None();
        Border = PdfInsets.None();
        HasBackground = false;
        BackgroundColor = PdfColor.Gray(1);
        BorderColor = PdfColor.Gray(0);
        Style = new PdfElementStyle();
        UsesReusableStyle = false;
        ColumnSpan = 1;
        RowSpan = 1;
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
        UsesReusableStyle = true;
        Padding = PdfElementStyle.CopyInsets(Style.Padding);
        Border = PdfElementStyle.CopyInsets(Style.Border);
        HasBackground = Style.HasBackground;
        BackgroundColor = PdfElementStyle.CopyColor(Style.BackgroundColor);
        BorderColor = PdfElementStyle.CopyColor(Style.BorderColor);
    }

    /// <summary>
    /// Sets the background used for subsequent PDF generation.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    public void SetBackground(PdfColor color)
    {
        ValidateColor(color, "color");
        BackgroundColor = color;
        HasBackground = true;
        Style.BackgroundColor = PdfElementStyle.CopyColor(color);
        Style.HasBackground = true;
    }

    /// <summary>
    /// Clears the current background.
    /// </summary>
    public void ClearBackground()
    {
        HasBackground = false;
        Style.HasBackground = false;
    }

    /// <summary>
    /// Sets the border color used for subsequent PDF generation.
    /// </summary>
    /// <param name="color">The PDF color to apply.</param>
    public void SetBorderColor(PdfColor color)
    {
        ValidateColor(color, "color");
        BorderColor = color;
        Style.BorderColor = PdfElementStyle.CopyColor(color);
    }

    /// <summary>
    /// Sets the number of consecutive columns occupied by this cell.
    /// </summary>
    /// <param name="columnSpan">The positive column span.</param>
    public void SetColumnSpan(int columnSpan)
    {
        if (columnSpan <= 0)
        {
            throw new ArgumentOutOfRangeException("columnSpan", "Table cell column span must be positive.");
        }

        ColumnSpan = columnSpan;
    }

    /// <summary>
    /// Sets the number of consecutive rows occupied by this cell.
    /// </summary>
    /// <param name="rowSpan">The positive row span.</param>
    public void SetRowSpan(int rowSpan)
    {
        if (rowSpan <= 0)
        {
            throw new ArgumentOutOfRangeException("rowSpan", "Table cell row span must be positive.");
        }

        RowSpan = rowSpan;
    }

    /// <summary>
    /// Returns the configured column span.
    /// </summary>
    /// <returns>The positive column span.</returns>
    public int GetColumnSpan()
    {
        return ColumnSpan;
    }

    /// <summary>
    /// Returns the configured row span.
    /// </summary>
    /// <returns>The positive row span.</returns>
    public int GetRowSpan()
    {
        return RowSpan;
    }

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        PdfBlock.ValidateContext(context);

        double contentWidth = GetContentWidth(context.AvailableWidth);
        double contentHeightAvailable = context.AvailableHeight - Padding.GetVertical() - Border.GetVertical();
        if (contentHeightAvailable < 0)
        {
            contentHeightAvailable = 0;
        }

        PdfLayoutContext childContext = new PdfLayoutContext(contentWidth, contentHeightAvailable);
        PdfLayoutResult childResult = Item.Measure(childContext);
        double height = childResult.Size.Height + Padding.GetVertical() + Border.GetVertical();
        PdfBox box = CreateMeasuredBox(contentWidth, childResult.Size.Height);
        PdfLayoutSize size = new PdfLayoutSize(context.AvailableWidth, height);
        bool fits = PdfBlock.SizeFits(size, context) && childResult.Fits;

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
        if (UsesReusableStyle)
        {
            PdfLayoutSize paintSize = new PdfLayoutSize(context.AvailableWidth, context.AvailableHeight);
            PdfLayoutResult paintResult = new PdfLayoutResult(
                paintSize,
                PdfBox.FromCoordinates(0, 0, context.AvailableWidth, context.AvailableHeight),
                result.Fits);
            Style.DrawBox(context, paintResult, PdfInsets.None(), Border);
        }
        else
        {
            DrawBackground(context);
            DrawBorder(context);
        }

        double contentWidth = GetContentWidth(context.AvailableWidth);
        double contentX = context.X + Border.Left + Padding.Left;
        double childY = context.Y + context.AvailableHeight - Border.Top - Padding.Top - result.Box.ContentRectangle.Height;
        PdfLayoutDrawContext childContext = new PdfLayoutDrawContext(
            context.Page,
            contentX,
            childY,
            contentWidth,
            result.Box.ContentRectangle.Height);
        Item.Draw(childContext);

        return result;
    }

    internal PdfBox CreateMeasuredBox(double contentWidth, double contentHeight)
    {
        PdfRectangle contentRectangle = new PdfRectangle(0, 0, contentWidth, contentHeight);
        return new PdfBox(contentRectangle, Padding, Border, PdfInsets.None());
    }

    internal double GetContentWidth(double availableWidth)
    {
        double contentWidth = availableWidth - Padding.GetHorizontal() - Border.GetHorizontal();
        if (contentWidth <= 0)
        {
            throw new ArgumentException("Table cell insets must leave positive content width.");
        }

        return contentWidth;
    }

    internal double MeasureNaturalWidth(double availableWidth, double availableHeight)
    {
        double insetWidth = Padding.GetHorizontal() + Border.GetHorizontal();
        double maximumContentWidth = availableWidth - insetWidth;
        if (maximumContentWidth <= 0)
        {
            maximumContentWidth = 1;
        }

        PdfLayoutResult maximumResult = Item.Measure(new PdfLayoutContext(maximumContentWidth, double.MaxValue));
        if (maximumResult.Size.Width < maximumContentWidth)
        {
            return maximumResult.Size.Width + insetWidth;
        }

        double low = 0.000001;
        double high = maximumContentWidth;
        int iteration = 0;
        while (iteration < 24)
        {
            double candidateWidth = (low + high) / 2;
            PdfLayoutResult candidate = Item.Measure(new PdfLayoutContext(candidateWidth, double.MaxValue));
            if (candidate.Fits && candidate.Size.Height <= maximumResult.Size.Height + 0.000001)
            {
                high = candidateWidth;
            }
            else
            {
                low = candidateWidth;
            }

            iteration++;
        }

        return high + insetWidth;
    }

    internal void DrawBackground(PdfLayoutDrawContext context)
    {
        if (!HasBackground)
        {
            return;
        }

        context.Page.Canvas.SetFillColorValue(BackgroundColor);
        context.Page.Canvas.FillRectangle(context.X, context.Y, context.AvailableWidth, context.AvailableHeight);
    }

    internal void DrawBorder(PdfLayoutDrawContext context)
    {
        if (Border.GetHorizontal() == 0 && Border.GetVertical() == 0)
        {
            return;
        }

        context.Page.Canvas.SetFillColorValue(BorderColor);
        if (Border.Bottom > 0)
        {
            context.Page.Canvas.FillRectangle(context.X, context.Y, context.AvailableWidth, Border.Bottom);
        }

        if (Border.Top > 0)
        {
            context.Page.Canvas.FillRectangle(context.X, context.Y + context.AvailableHeight - Border.Top, context.AvailableWidth, Border.Top);
        }

        if (Border.Left > 0)
        {
            context.Page.Canvas.FillRectangle(context.X, context.Y, Border.Left, context.AvailableHeight);
        }

        if (Border.Right > 0)
        {
            context.Page.Canvas.FillRectangle(context.X + context.AvailableWidth - Border.Right, context.Y, Border.Right, context.AvailableHeight);
        }
    }

    internal static void ValidateInsets(PdfInsets insets, string name)
    {
        if (insets == null)
        {
            throw new ArgumentNullException(name);
        }
    }

    internal static void ValidateColor(PdfColor color, string name)
    {
        if (color == null)
        {
            throw new ArgumentNullException(name);
        }
    }
}
