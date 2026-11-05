namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgPaintServerResolver"/>.</summary>
public interface ISvgPaintServerResolver
{
    /// <summary>Resolves one gradient or pattern using deterministic defaults.</summary>
    /// <param name="node">The paint-server element.</param>
    /// <returns>The resolved template, geometry, and content.</returns>
    SvgPaintServerResult ResolveWithNode(SvgNode node);
    /// <summary>Resolves one gradient or pattern with explicit target geometry, styles, references, and limits.</summary>
    /// <param name="node">The paint-server element.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The resolved template, geometry, and content.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not a supported paint server.</exception>
    SvgPaintServerResult ResolveWithNodeAndOptions(SvgNode node, SvgPaintServerResolveOptions options);
}
