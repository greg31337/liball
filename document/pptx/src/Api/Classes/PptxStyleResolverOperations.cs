namespace pptx;

/// <summary>
/// Resolves effective styles across presentation inheritance layers.
/// </summary>
public class PptxStyleResolverOperations : IPptxStyleResolverOperations
{
    /// <summary>
    /// Creates an effective-style resolver.
    /// </summary>
    public PptxStyleResolverOperations()
    {
    }

    /// <summary>
    /// Resolves drawing, text, and background formatting for a shape.
    /// </summary>
    /// <param name="target">The shape to resolve.</param>
    /// <returns>The effective style.</returns>
    public PptxEffectiveStyle ResolveShape(PptxShape target)
    {
        return PptxEffectiveStyleCore.ResolveShape(target);
    }

    /// <summary>
    /// Resolves formatting for a table cell, including its table style and owning shape.
    /// </summary>
    /// <param name="target">The table cell to resolve.</param>
    /// <returns>The effective style.</returns>
    public PptxEffectiveStyle ResolveTableCell(PptxTableCell target)
    {
        return PptxEffectiveStyleCore.ResolveTableCell(target);
    }

    /// <summary>
    /// Resolves formatting for a chart and its owning shape.
    /// </summary>
    /// <param name="target">The chart to resolve.</param>
    /// <returns>The effective style.</returns>
    public PptxEffectiveStyle ResolveChart(PptxChart target)
    {
        return PptxEffectiveStyleCore.ResolveChart(target);
    }

    /// <summary>
    /// Resolves inherited text formatting for a run.
    /// </summary>
    /// <param name="target">The text run to resolve.</param>
    /// <returns>The effective style.</returns>
    public PptxEffectiveStyle ResolveRun(PptxRun target)
    {
        return PptxEffectiveStyleCore.ResolveRun(target);
    }

    /// <summary>
    /// Resolves a slide background through layout and master inheritance.
    /// </summary>
    /// <param name="target">The slide to resolve.</param>
    /// <returns>The effective style.</returns>
    public PptxEffectiveStyle ResolveBackground(PptxSlide target)
    {
        return PptxEffectiveStyleCore.ResolveBackground(target);
    }
}
