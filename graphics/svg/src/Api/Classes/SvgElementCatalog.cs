namespace svg;

/// <summary>Provides data-driven definitions for every current SVG element kind.</summary>
public class SvgElementCatalog : ISvgElementCatalog
{
    System.Collections.Generic.List<SvgElementDefinition> entries;

    internal SvgElementCatalog(System.Collections.Generic.List<SvgElementDefinition> definitions)
    {
        entries = definitions;
    }

    /// <summary>Creates the complete current-element catalog.</summary>
    /// <returns>A new catalog containing every SVG 2 element kind.</returns>
    internal SvgElementCatalog CreateDefault()
    {
        SvgElementCatalogBuilder builder = new SvgElementCatalogBuilder();
        return builder.Build();
    }

    /// <summary>Finds a definition by recognized element kind.</summary>
    /// <param name="kind">The recognized kind.</param>
    /// <returns>The definition, or <c>null</c> for unknown and legacy kinds.</returns>
    public SvgElementDefinition? Find(SvgElementKind kind)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].Kind == kind) return entries[index];
            index = index + 1;
        }
        return null;
    }

    /// <summary>Finds a definition by expanded XML name.</summary>
    /// <param name="namespaceUri">The resolved namespace URI.</param>
    /// <param name="localName">The case-sensitive local name.</param>
    /// <returns>The current-element definition, or <c>null</c>.</returns>
    public SvgElementDefinition? FindByName(string? namespaceUri, string? localName)
    {
        if (namespaceUri == null || localName == null) return null;
        SvgElementKind kind = new SvgNodeNames().GetElementKind(namespaceUri, localName);
        return Find(kind);
    }

    /// <summary>Returns an independent catalog-order snapshot.</summary>
    /// <returns>Every current element definition exactly once.</returns>
    public System.Collections.Generic.List<SvgElementDefinition> GetEntries()
    {
        return new System.Collections.Generic.List<SvgElementDefinition>(entries);
    }
}
