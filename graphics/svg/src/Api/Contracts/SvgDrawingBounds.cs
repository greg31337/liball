namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgDrawingBounds"/>.</summary>
public interface ISvgDrawingBounds
{
    /// <summary>Computes exact path fill bounds in local coordinates.</summary>
    /// <param name="path">The valid path.</param>
    /// <returns>The explicitly empty or finite fill bounds.</returns>
    SvgDrawingBoundsResult Fill(SvgPathData path);
    /// <summary>Computes combined path drawing bounds with explicit components and effects.</summary>
    /// <param name="path">The valid path.</param><param name="options">The component, world-transform, and effect options.</param>
    /// <returns>The component and final world-space bounds.</returns>
    SvgDrawingBoundsResult Path(SvgPathData? path, SvgDrawingBoundsOptions options);
    /// <summary>Computes resolved basic-shape drawing bounds with explicit components and effects.</summary>
    /// <param name="shape">The resolved shape.</param><param name="options">The component, world-transform, and effect options.</param>
    /// <returns>The component and final world-space bounds.</returns>
    SvgDrawingBoundsResult Shape(SvgShapeResult? shape, SvgDrawingBoundsOptions options);
    /// <summary>Aggregates current renderable resolved-node drawing bounds.</summary>
    /// <param name="document">The current resolved document.</param><param name="options">The traversal, viewport, stroke, marker, and effect options.</param>
    /// <returns>The aggregate world-space bounds and contribution counts.</returns>
    SvgDocumentBoundsResult Document(SvgResolvedDocument? document, SvgDocumentBoundsOptions? options);
}
