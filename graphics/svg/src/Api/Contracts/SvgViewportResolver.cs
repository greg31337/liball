namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgViewportResolver"/>.</summary>
public interface ISvgViewportResolver
{
    /// <summary>Resolves a root viewport against explicit host viewport dimensions.</summary>
    /// <param name="node">The root SVG element.</param>
    /// <param name="hostWidth">The nonnegative host viewport width.</param>
    /// <param name="hostHeight">The nonnegative host viewport height.</param>
    /// <returns>The established root viewport or a located failure.</returns>
    SvgViewportResult ResolveRoot(SvgNode node, double hostWidth, double hostHeight);
    /// <summary>Resolves a nested SVG viewport using a containing viewport result.</summary>
    /// <param name="node">The nested SVG element.</param>
    /// <param name="parentViewport">The nearest containing SVG viewport.</param>
    /// <returns>The established nested viewport or a located failure.</returns>
    SvgViewportResult ResolveNested(SvgNode node, SvgViewportResult parentViewport);
    /// <summary>Resolves one SVG element viewport with explicit context, style, and fragment options.</summary>
    /// <param name="node">The SVG element that establishes the viewport.</param>
    /// <param name="options">The containing context and optional computed inputs.</param>
    /// <returns>The established viewport or a located failure.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not an SVG <c>svg</c> element.</exception>
    SvgViewportResult Resolve(SvgNode node, SvgViewportResolveOptions options);
}
