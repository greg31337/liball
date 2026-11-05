namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgElementCatalog"/>.</summary>
public interface ISvgElementCatalog
{
    /// <summary>Finds a definition by recognized element kind.</summary>
    /// <param name="kind">The recognized kind.</param>
    /// <returns>The definition, or <c>null</c> for unknown and legacy kinds.</returns>
    SvgElementDefinition? Find(SvgElementKind kind);
    /// <summary>Finds a definition by expanded XML name.</summary>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>The current-element definition, or <c>null</c>.</returns>
    SvgElementDefinition? FindByName(string? namespaceUri, string? localName);
    /// <summary>Returns an independent catalog-order snapshot.</summary>
    /// <returns>Every current element definition exactly once.</returns>
    System.Collections.Generic.List<SvgElementDefinition> GetEntries();
}
