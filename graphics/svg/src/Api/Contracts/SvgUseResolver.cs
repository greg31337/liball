namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgUseResolver"/>.</summary>
public interface ISvgUseResolver
{
    /// <summary>Expands one <c>use</c> element using deterministic defaults.</summary>
    /// <param name="node">The authored <c>use</c> element.</param>
    /// <returns>The source-linked shadow expansion.</returns>
    SvgUseResult ResolveWithNode(SvgNode node);
    /// <summary>Expands one <c>use</c> element with explicit references, styles, geometry, and limits.</summary>
    /// <param name="node">The authored <c>use</c> element.</param>
    /// <param name="options">The expansion options.</param>
    /// <returns>The source-linked shadow expansion.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not an SVG <c>use</c> element.</exception>
    SvgUseResult ResolveWithNodeAndOptions(SvgNode node, SvgUseResolveOptions options);
}
