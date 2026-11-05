using System;

namespace pdf;

/// <summary>
/// Positions a layout element inside a fixed frame relative to the bottom-left of its occupied flow area.
/// </summary>
/// <remarks>
/// The occupied size includes the frame offsets so the positioned element participates in ordinary measurement and pagination.
/// </remarks>
public class PdfAbsoluteLayoutElement : IPdfLayoutElement, IPdfAbsoluteLayoutElement
{
    internal IPdfLayoutElement Item = new PdfBlock();
    internal double X;
    internal double Y;
    internal double Width;
    internal double Height;
    internal bool ClipToBounds;

    /// <summary>
    /// Initializes an absolute-positioned layout element.
    /// </summary>
    /// <param name="item">The child layout element.</param>
    /// <param name="x">The non-negative horizontal offset from the occupied area's left edge.</param>
    /// <param name="y">The non-negative vertical offset from the occupied area's bottom edge.</param>
    /// <param name="width">The positive frame width in PDF points.</param>
    /// <param name="height">The positive frame height in PDF points.</param>
    /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an offset is negative or a frame dimension is not positive.</exception>
    public PdfAbsoluteLayoutElement(IPdfLayoutElement item, double x, double y, double width, double height)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        ValidateOffset(x, "x");
        ValidateOffset(y, "y");
        ValidateDimension(width, "width");
        ValidateDimension(height, "height");

        Item = item;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        ClipToBounds = true;
    }

    /// <summary>
    /// Clips child drawing to the configured absolute frame.
    /// </summary>
    public void EnableClipping()
    {
        ClipToBounds = true;
    }

    /// <summary>
    /// Allows child drawing to extend beyond the configured absolute frame.
    /// </summary>
    public void DisableClipping()
    {
        ClipToBounds = false;
    }

    /// <summary>
    /// Measures the positioned frame and its child against the available area.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured size, fit status, and positioned bounds.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        PdfBlock.ValidateContext(context);

        PdfLayoutResult childResult = Item.Measure(new PdfLayoutContext(Width, Height));
        double occupiedWidth = X + Width;
        double occupiedHeight = Y + Height;
        PdfLayoutSize size = new PdfLayoutSize(occupiedWidth, occupiedHeight);
        bool fits = childResult.Fits && PdfBlock.SizeFits(size, context);
        PdfLayoutResult result = new PdfLayoutResult(size, PdfBox.FromCoordinates(0, 0, occupiedWidth, occupiedHeight), fits);
        result.SetDebugBounds(new PdfRectangle(X, Y, Width, Height));
        return result;
    }

    /// <summary>
    /// Draws the child at its absolute frame and optionally clips overflowing drawing.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured size, fit status, and positioned bounds.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        PdfBlock.ValidateDrawContext(context);

        PdfLayoutResult result = Measure(context.ToLayoutContext());
        double childX = context.X + X;
        double childY = context.Y + Y;
        PdfLayoutDrawContext childContext = new PdfLayoutDrawContext(context.Page, childX, childY, Width, Height);

        if (ClipToBounds)
        {
            context.Page.Canvas.SaveState();
            context.Page.Canvas.ClipRectangle(childX, childY, Width, Height);
        }

        Item.Draw(childContext);

        if (ClipToBounds)
        {
            context.Page.Canvas.RestoreState();
        }

        return result;
    }

    /// <summary>
    /// Flows the positioned element through the supplied page cursor.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        if (cursor == null)
        {
            throw new ArgumentNullException("cursor");
        }

        return cursor.DrawItem(this);
    }

    internal static void ValidateOffset(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Absolute layout offsets must be finite and cannot be negative.");
        }
    }

    internal static void ValidateDimension(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, "Absolute layout frame dimensions must be finite and positive.");
        }
    }
}
