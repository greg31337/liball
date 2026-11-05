namespace xml;

internal class CatalogResourceSource : ICatalogEntrySource
{
    internal IXmlResourceResolver Resolver;
    internal XmlResolverPolicy Policy;

    internal CatalogResourceSource(IXmlResourceResolver resolver, XmlResolverPolicy policy)
    {
        Guard.NotNull(resolver, "resolver");
        Guard.NotNull(policy, "policy");
        Resolver = resolver;
        Policy = policy;
    }

    public XmlCatalogEntry[]? LoadXmlCatalogEntries(string catalogUri)
    {
        XmlResolvedResource resource = Resolver.ResolveXmlResource(XmlResourceKind.Catalog, null, catalogUri, null, Policy);
        Guard.NotNull(resource, "resolvedCatalog");
        if (resource.Access != XmlResourceAccess.Allowed)
        {
            return null;
        }

        if (resource.Kind != XmlResourceKind.Catalog)
        {
            throw CreateXmlCatalogResourceError("The resolver returned a non-catalog resource for an XML Catalog request", catalogUri);
        }

        XmlInput? input = resource.Input;
        if (input == null)
        {
            throw CreateXmlCatalogResourceError("The resolver allowed an XML Catalog resource without supplying input", catalogUri);
        }

        if (input.BaseUri == null)
        {
            input.BaseUri = resource.AbsoluteUri == null ? catalogUri : resource.AbsoluteUri;
        }

        if (input.MediaType == null)
        {
            input.MediaType = resource.MediaType;
        }

        if (input.EncodingName == null)
        {
            input.EncodingName = resource.EncodingName;
        }

        XmlParserOptions options = CreateXmlCatalogParserOptions();
        XmlDocument document = new XmlParser().ParseXmlDocument(input, options, null, null, null);
        return new CatalogParser(false).ParseXmlCatalog(document);
    }

    internal XmlParserOptions CreateXmlCatalogParserOptions()
    {
        XmlParserOptions options = new XmlParserOptions();
        options.Version = XmlVersion.Xml10;
        options.NamespaceHandling = XmlNamespaceHandling.Namespaces10;
        options.DtdProcessing = XmlDtdProcessing.Prohibit;
        options.EntityHandling = XmlEntityHandling.Expand;
        options.ValidationMode = XmlValidationMode.None;
        options.PreserveSourceLocations = true;
        return options;
    }

    internal XmlResourceException CreateXmlCatalogResourceError(string message, string sourceUri)
    {
        ResolverGuard guard = new ResolverGuard(Policy, null);
        return guard.CreateXmlResourceError(message, XmlErrorCodes.CatalogMalformed, sourceUri);
    }
}
