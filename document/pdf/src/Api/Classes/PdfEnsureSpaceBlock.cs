using System;

namespace pdf;

/// <summary>
/// Represents a ensure space element in the paginated PDF layout system.
/// </summary>
public class PdfEnsureSpaceBlock : IPdfLayoutElement, IPdfEnsureSpaceBlock
{
    /// <summary>
    /// Specifies the height in PDF points.
    /// </summary>
    internal double Height;

    /// <summary>
    /// Initializes a new ensure space block.
    /// </summary>
    /// <param name="height">The height in PDF points; it must be positive unless the method states otherwise.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a numeric argument falls outside its supported range.</exception>
    public PdfEnsureSpaceBlock(double height)
    {
        if (double.IsNaN(height) || double.IsInfinity(height) || height < 0)
        {
            throw new ArgumentOutOfRangeException("height", "Ensure-space height must be finite and nonnegative.");
        }

        Height = height;
    }

    /// <summary>
    /// Measures this layout element against the available area without emitting drawing commands.
    /// </summary>
    /// <param name="context">The available layout area and drawing state for this operation.</param>
    /// <returns>The measured or drawn size, fit status, and occupied bounds.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context)
    {
        PdfBlock.ValidateContext(context);
        return new PdfLayoutResult(new PdfLayoutSize(0, 0), PdfBox.FromCoordinates(0, 0, 0, 0), true);
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
        return cursor.DrawEnsureSpaceBlock(this);
    }
}
