namespace pdf;

/// <summary>
/// Specifies how glyph outlines are painted or added to the clipping path.
/// </summary>
public enum PdfTextRenderingMode
{
    /// <summary>
    /// Fills glyph outlines.
    /// </summary>
    Fill = 0,
    /// <summary>
    /// Strokes glyph outlines.
    /// </summary>
    Stroke = 1,
    /// <summary>
    /// Fills and strokes glyph outlines.
    /// </summary>
    FillStroke = 2,
    /// <summary>
    /// Does not paint glyph outlines.
    /// </summary>
    Invisible = 3,
    /// <summary>
    /// Fills glyph outlines and adds them to the clipping path.
    /// </summary>
    FillClip = 4,
    /// <summary>
    /// Strokes glyph outlines and adds them to the clipping path.
    /// </summary>
    StrokeClip = 5,
    /// <summary>
    /// Fills and strokes glyph outlines and adds them to the clipping path.
    /// </summary>
    FillStrokeClip = 6,
    /// <summary>
    /// Adds glyph outlines to the clipping path without painting them.
    /// </summary>
    Clip = 7
}
