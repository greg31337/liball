namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgEffectResolver"/>.</summary>
public interface ISvgEffectResolver
{
    /// <summary>Resolves one supported effect resource using deterministic defaults.</summary>
    /// <param name="node">The marker, clipPath, or mask element.</param>
    /// <returns>The normalized effect resource.</returns>
    SvgEffectResult ResolveWithNode(SvgNode node);
    /// <summary>Resolves one supported effect resource with explicit geometry, style, reference, and limit inputs.</summary>
    /// <param name="node">The marker, clipPath, or mask element.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The normalized effect resource.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node is not a supported SVG effect element.</exception>
    SvgEffectResult ResolveWithNodeAndOptions(SvgNode node, SvgEffectResolveOptions options);
}
