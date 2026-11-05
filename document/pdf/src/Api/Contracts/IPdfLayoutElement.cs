namespace pdf;

/// <summary>
/// Defines an element that can be measured, drawn in a fixed area, and flowed through PDF pages.
/// </summary>
/// <remarks>
/// Implement this interface to add custom elements to layout documents and container blocks without requiring a library-defined wrapper type.
/// </remarks>
public interface IPdfLayoutElement
{
    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context);

    /// <summary>
    /// Draws this measured layout element into the supplied page area.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context);

    /// <summary>
    /// Flows this layout element through the supplied page cursor.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination and the current drawing position.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Flow(PdfPageCursor cursor);
}
