using System;

namespace pdf;

/// <summary>
/// Decorates a layout element with document-flow options such as keep-together behavior.
/// </summary>
public class PdfLayoutItem : IPdfLayoutElement, IPdfLayoutItem
{
    /// <summary>
    /// Stores the layout element wrapped by this object.
    /// </summary>
    internal IPdfLayoutElement Element;
    /// <summary>
    /// Indicates whether keep together is enabled.
    /// </summary>
    internal bool KeepTogether;

    /// <summary>
    /// Initializes a layout item for the supplied element.
    /// </summary>
    /// <param name="element">The layout element to decorate.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutItem(IPdfLayoutElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        Element = element;
        KeepTogether = false;
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied layout element.
    /// </summary>
    /// <param name="element">The layout element to decorate.</param>
    /// <returns>A layout item wrapper that can apply document-flow options.</returns>
    internal static PdfLayoutItem FromElement(IPdfLayoutElement element)
    {
        return new PdfLayoutItem(element);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromBlock(PdfBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied text block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromTextBlock(PdfTextBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied page break block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromPageBreakBlock(PdfPageBreakBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied stack block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromStackBlock(PdfStackBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied row block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromRowBlock(PdfRowBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied column block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromColumnBlock(PdfColumnBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied grid block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromGridBlock(PdfGridBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied table block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromTableBlock(PdfTableBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied list block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromListBlock(PdfListBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied ensure-space block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromEnsureSpaceBlock(PdfEnsureSpaceBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Creates and returns a layout item from the supplied image block.
    /// </summary>
    /// <param name="block">The layout block to add, measure, draw, or wrap.</param>
    /// <returns>A layout item wrapper that the paginator can measure and draw.</returns>
    internal static PdfLayoutItem FromImageBlock(PdfImageBlock block)
    {
        return new PdfLayoutItem(block);
    }

    /// <summary>
    /// Sets the keep together used for subsequent PDF generation.
    /// </summary>
    public void SetKeepTogether()
    {
        KeepTogether = true;
    }

    /// <summary>
    /// Clears the current keep together.
    /// </summary>
    public void ClearKeepTogether()
    {
        KeepTogether = false;
    }

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        return Element.Measure(context);
    }

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        return Element.Draw(context);
    }

    /// <summary>
    /// Flows this layout element through the supplied page cursor.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        return DrawWithCursor(cursor);
    }

    /// <summary>
    /// Flows this layout item through the supplied page cursor.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawWithCursor(PdfPageCursor cursor)
    {
        if (cursor == null)
        {
            throw new ArgumentNullException("cursor");
        }

        if (KeepTogether)
        {
            return cursor.DrawKeepTogetherItem(this);
        }

        return DrawWithoutKeepTogether(cursor);
    }

    /// <summary>
    /// Flows this layout item without applying its keep-together option.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfLayoutResult DrawWithoutKeepTogether(PdfPageCursor cursor)
    {
        if (cursor == null)
        {
            throw new ArgumentNullException("cursor");
        }

        return Element.Flow(cursor);
    }
}
