namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgAccessibility"/>.</summary>
public interface ISvgAccessibility
{
    /// <summary>Calculates accessible semantics with secure defaults.</summary>
    /// <param name="node">The current resolved source or instance node.</param>
    /// <returns>The accessible semantic snapshot.</returns>
    SvgAccessibleSemantics ResolveWithNode(SvgResolvedNode node);
    /// <summary>Calculates accessible semantics with explicit bounds.</summary>
    /// <param name="node">The current resolved source or instance node.</param>
    /// <param name="options">The text and relationship bounds.</param>
    /// <returns>The accessible semantic snapshot.</returns>
    SvgAccessibleSemantics ResolveWithNodeAndOptions(SvgResolvedNode? node, SvgAccessibilityOptions options);
}
