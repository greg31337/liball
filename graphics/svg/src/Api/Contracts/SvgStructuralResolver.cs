namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgStructuralResolver"/>.</summary>
public interface ISvgStructuralResolver
{
    /// <summary>Resolves one supported structural SVG element with secure defaults.</summary>
    /// <param name="node">The group, definitions, symbol, view, link, or descriptive element.</param>
    /// <returns>The resolved structural semantics.</returns>
    SvgStructuralResult ResolveWithNode(SvgNode node);
    /// <summary>Resolves one supported structural SVG element with explicit context and limits.</summary>
    /// <param name="node">The group, definitions, symbol, view, link, or descriptive element.</param>
    /// <param name="options">The reference, style, parsing, and security context.</param>
    /// <returns>The resolved structural semantics or a located failure.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when the node has no supported structural role.</exception>
    SvgStructuralResult ResolveWithNodeAndOptions(SvgNode node, SvgStructuralResolveOptions options);
}
