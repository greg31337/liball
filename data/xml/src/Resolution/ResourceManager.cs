namespace xml;

internal class ResourceManager : IXmlResourceResolver
{
    internal IXmlResourceResolver CallerResolver;
    internal XmlResolverPolicy Policy;
    internal XmlCatalogResolver? CatalogResolver;

    internal ResourceManager(IXmlResourceResolver callerResolver, XmlResolverPolicy policy)
    {
        Guard.NotNull(callerResolver, "callerResolver");
        Guard.NotNull(policy, "policy");
        CallerResolver = callerResolver;
        Policy = policy;
        XmlCatalogEntry[]? entries = policy.CatalogEntries;
        if (entries != null && entries.Length > 0)
        {
            CatalogResolver = new XmlCatalogResolver();
            CatalogResolver.LoadXmlCatalog(entries, callerResolver, policy);
        }
    }

    public XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy)
    {
        Guard.NotNull(policy, "policy");
        XmlCatalogResolver? catalog = CatalogResolver;
        if (catalog != null)
        {
            string? uri = UsesExternalXmlIdentifier(kind) ? null : systemIdentifier;
            XmlResolvedResource? catalogResource = catalog.TryResolveXmlCatalog(kind, publicIdentifier, systemIdentifier, uri);
            if (catalogResource != null)
            {
                return catalogResource;
            }
        }

        return CallerResolver.ResolveXmlResource(kind, publicIdentifier, systemIdentifier, baseUri, Policy);
    }

    internal bool UsesExternalXmlIdentifier(XmlResourceKind kind)
    {
        return kind == XmlResourceKind.ExternalSubset || kind == XmlResourceKind.GeneralEntity || kind == XmlResourceKind.ParameterEntity;
    }
}
