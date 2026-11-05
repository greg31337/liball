namespace htmlgui;

/// <summary>Expands accessibility-aware nodes and creates application accessibility reports.</summary>
public interface IUiAccessibilityEngine
{
    /// <summary>Expands a portable node into its deterministic accessibility-aware DOM representation.</summary>
    /// <param name="node">The node to expand.</param>
    /// <returns>The renderer-neutral DOM representation.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>node</c> is null.</exception>
    /// <exception cref="UiRenderException">Thrown when the node contains invalid render or accessibility data.</exception>
    DomNode ExpandAccessibility(UiNode node);

    /// <summary>Creates an exceptionless location-aware accessibility report for an application graph.</summary>
    /// <param name="application">The optional application graph to inspect.</param>
    /// <returns>The accessibility report.</returns>
    UiAccessibilityReport CreateAccessibilityReport(UiApplication? application);
}
