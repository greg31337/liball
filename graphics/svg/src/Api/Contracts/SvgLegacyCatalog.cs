namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgLegacyCatalog"/>.</summary>
public interface ISvgLegacyCatalog
{
    /// <summary>Finds a legacy element definition.</summary>
    /// <param name="kind">The recognized element kind.</param>
    /// <returns>The definition, or <c>null</c>.</returns>
    SvgLegacyFeatureDefinition? FindElement(SvgElementKind kind);
    /// <summary>Finds a legacy attribute definition by expanded XML name.</summary>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>The definition, or <c>null</c>.</returns>
    SvgLegacyFeatureDefinition? FindAttribute(string? namespaceUri, string? localName);
    /// <summary>Returns an independent legacy-element definition snapshot.</summary>
    /// <returns>All recognized SVG 1.1 element definitions.</returns>
    System.Collections.Generic.List<SvgLegacyFeatureDefinition> GetElementEntries();
    /// <summary>Returns an independent legacy-attribute definition snapshot.</summary>
    /// <returns>All recognized SVG 1.1 and XLink attribute definitions.</returns>
    System.Collections.Generic.List<SvgLegacyFeatureDefinition> GetAttributeEntries();
    /// <summary>Analyzes one node and optionally its complete subtree.</summary>
    /// <param name="node">The document, element, or other node to inspect.</param>
    /// <param name="includeDescendants">Whether descendants are inspected in document order.</param>
    /// <returns>Located compatibility occurrences and diagnostics.</returns>
    SvgLegacyAnalysisResult Analyze(SvgNode node, bool includeDescendants);
}
