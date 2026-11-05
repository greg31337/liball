namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgFilterResolver"/>.</summary>
public interface ISvgFilterResolver
{
    /// <summary>Resolves one filter element using deterministic defaults.</summary>
    /// <param name="node">The authored filter element.</param>
    /// <returns>The normalized filter definition and primitive graph.</returns>
    SvgFilterResult ResolveWithNode(SvgNode node);
    /// <summary>Resolves one filter with explicit geometry, style, reference, and filter limits.</summary>
    /// <param name="node">The authored filter element.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The normalized filter definition and primitive graph.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not an SVG filter element.</exception>
    SvgFilterResult ResolveWithNodeAndOptions(SvgNode node, SvgFilterResolveOptions options);
}
