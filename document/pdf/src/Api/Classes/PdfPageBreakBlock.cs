namespace pdf;

/// <summary>
/// Represents a page break element in the paginated PDF layout system.
/// </summary>
public class PdfPageBreakBlock : IPdfLayoutElement, IPdfPageBreakBlock
{
    /// <summary>
    /// Initializes a new page break block.
    /// </summary>
    public PdfPageBreakBlock()
    {
    }

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        PdfBlock.ValidateContext(context);

        PdfLayoutSize size = new PdfLayoutSize(0, 0);
        PdfBox box = PdfBox.FromCoordinates(0, 0, 0, 0);
        return new PdfLayoutResult(size, box, true);
    }

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context)
    {
        PdfBlock.ValidateDrawContext(context);

        return Measure(context.ToLayoutContext());
    }

    /// <summary>
    /// Flows this layout element through the supplied page cursor.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        return cursor.DrawPageBreakBlock(this);
    }
}
