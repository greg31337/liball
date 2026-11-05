namespace xml;

/// <summary>
/// Resolves public, system, and URI identifiers through XML Catalogs 1.1.
/// </summary>
public interface IXmlCatalogResolver
{
    /// <summary><para>Loads an ordered catalog entry set.</para></summary>
    /// <param name="entries">The catalog entries.</param><param name="resolver">The resolver for chained catalogs.</param><param name="policy">The resource policy.</param>
    void LoadXmlCatalog(XmlCatalogEntry[] entries, IXmlResourceResolver resolver, XmlResolverPolicy policy);
    /// <summary><para>Resolves an identifier through the loaded catalogs.</para></summary>
    /// <param name="kind">The resource kind.</param><param name="publicIdentifier">The optional public identifier.</param><param name="systemIdentifier">The optional system identifier.</param><param name="uri">The optional URI.</param>
    /// <returns>The resolved resource.</returns>
    XmlResolvedResource ResolveXmlCatalog(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? uri);
    /// <summary><para>Attempts catalog resolution without throwing for a miss.</para></summary>
    /// <param name="kind">The resource kind.</param><param name="publicIdentifier">The optional public identifier.</param><param name="systemIdentifier">The optional system identifier.</param><param name="uri">The optional URI.</param>
    /// <returns>The resolved resource, or null for a miss.</returns>
    XmlResolvedResource? TryResolveXmlCatalog(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? uri);
    /// <summary><para>Clears catalog match and resource caches.</para></summary>
    void ClearXmlCatalogCache();
}

