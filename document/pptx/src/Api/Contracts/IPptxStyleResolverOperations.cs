namespace pptx;

/// <summary>
/// Defines effective-style resolution operations.
/// </summary>
public interface IPptxStyleResolverOperations
{
    /// <summary>
    /// Resolves drawing, text, and background formatting for a shape.
    /// </summary>
    /// <param name="target">The shape to resolve.</param>
    /// <returns>The effective style.</returns>
    PptxEffectiveStyle ResolveShape(PptxShape target);
    /// <summary>
    /// Resolves formatting for a table cell, including its table style and owning shape.
    /// </summary>
    /// <param name="target">The table cell to resolve.</param>
    /// <returns>The effective style.</returns>
    PptxEffectiveStyle ResolveTableCell(PptxTableCell target);
    /// <summary>
    /// Resolves formatting for a chart and its owning shape.
    /// </summary>
    /// <param name="target">The chart to resolve.</param>
    /// <returns>The effective style.</returns>
    PptxEffectiveStyle ResolveChart(PptxChart target);
    /// <summary>
    /// Resolves inherited text formatting for a run.
    /// </summary>
    /// <param name="target">The text run to resolve.</param>
    /// <returns>The effective style.</returns>
    PptxEffectiveStyle ResolveRun(PptxRun target);
    /// <summary>
    /// Resolves a slide background through layout and master inheritance.
    /// </summary>
    /// <param name="target">The slide to resolve.</param>
    /// <returns>The effective style.</returns>
    PptxEffectiveStyle ResolveBackground(PptxSlide target);
}
