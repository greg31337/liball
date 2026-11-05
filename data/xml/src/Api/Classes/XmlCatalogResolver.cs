using System;

namespace xml;

/// <summary>Resolves resources through an ordered XML Catalog entry set.</summary>
public class XmlCatalogResolver : IXmlCatalogResolver
{
    internal XmlCatalogEntry[] Entries;
    internal IXmlResourceResolver? ResourceResolver;
    internal XmlResolverPolicy? ResolverPolicy;
    internal CatalogResourceSource? CatalogSource;
    internal CatalogMatcher? Matcher;
    internal ResourceCache ResolvedResourceCache;

    /// <summary>Creates an XML Catalog resolver.</summary>
    public XmlCatalogResolver()
    {
        Entries = [];
        ResolvedResourceCache = new ResourceCache();
    }

    /// <summary>Loads XML Catalog entries.</summary>
    public void LoadXmlCatalog(XmlCatalogEntry[] entries, IXmlResourceResolver resolver, XmlResolverPolicy policy)
    {
        Guard.NotNull(entries, "entries");
        Guard.NotNull(resolver, "resolver");
        Guard.NotNull(policy, "policy");
        Entries = entries;
        ResourceResolver = resolver;
        ResolverPolicy = policy;
        CatalogSource = new CatalogResourceSource(resolver, policy);
        Matcher = new CatalogMatcher(entries, CatalogSource, policy.CacheResources);
        ResolvedResourceCache.ClearXmlResources();
    }

    /// <summary>Resolves through the loaded catalog.</summary>
    public XmlResolvedResource ResolveXmlCatalog(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? uri)
    {
        XmlResolvedResource? result = TryResolveXmlCatalog(kind, publicIdentifier, systemIdentifier, uri);
        if (result == null)
        {
            throw CreateXmlCatalogResolutionError("No XML Catalog entry matched the supplied identifier", XmlErrorCodes.ResourceNotFound, uri == null ? systemIdentifier : uri);
        }

        return result;
    }

    /// <summary>Attempts resolution through the loaded catalog.</summary>
    public XmlResolvedResource? TryResolveXmlCatalog(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? uri)
    {
        CatalogMatcher? optionalMatcher = Matcher;
        IXmlResourceResolver? optionalResolver = ResourceResolver;
        XmlResolverPolicy? optionalPolicy = ResolverPolicy;
        if (optionalMatcher == null || optionalResolver == null || optionalPolicy == null)
        {
            throw new InvalidOperationException("No XML Catalog has been loaded.");
        }

        string? resolvedUri;
        if (uri != null)
        {
            resolvedUri = optionalMatcher.ResolveUriIdentifier(uri);
        }
        else
        {
            resolvedUri = optionalMatcher.ResolveExternalIdentifier(publicIdentifier, systemIdentifier);
        }

        if (resolvedUri == null)
        {
            return null;
        }

        string? normalizedPublic = publicIdentifier == null ? null : CatalogIdentifiers.NormalizeXmlPublicIdentifierInput(publicIdentifier);
        string cacheKey = CreateXmlCatalogResourceKey(kind, normalizedPublic, resolvedUri);
        if (optionalPolicy.CacheResources)
        {
            XmlResolvedResource? cached = ResolvedResourceCache.FindXmlResource(cacheKey);
            if (cached != null)
            {
                return cached;
            }
        }

        XmlResolvedResource resource = optionalResolver.ResolveXmlResource(kind, normalizedPublic, resolvedUri, null, optionalPolicy);
        Guard.NotNull(resource, "resolvedResource");
        if (resource.Kind != kind)
        {
            throw CreateXmlCatalogResolutionError("The resolver returned a resource of the wrong kind", XmlErrorCodes.ResourceNotFound, resolvedUri);
        }

        if (resource.Access == XmlResourceAccess.Allowed && resource.Input == null)
        {
            throw CreateXmlCatalogResolutionError("The resolver allowed a catalog-mapped resource without supplying input", XmlErrorCodes.ResourceNotFound, resolvedUri);
        }

        resource.PublicIdentifier = normalizedPublic;
        resource.SystemIdentifier = resolvedUri;
        if (resource.RequestedUri == null)
        {
            resource.RequestedUri = resolvedUri;
        }

        XmlInput? input = resource.Input;
        if (input != null)
        {
            if (input.BaseUri == null)
            {
                input.BaseUri = resource.AbsoluteUri == null ? resolvedUri : resource.AbsoluteUri;
            }

            if (input.MediaType == null)
            {
                input.MediaType = resource.MediaType;
            }

            if (input.EncodingName == null)
            {
                input.EncodingName = resource.EncodingName;
            }
        }

        if (optionalPolicy.CacheResources)
        {
            ResolvedResourceCache.StoreXmlResource(cacheKey, resource);
        }

        return resource;
    }

    /// <summary>Clears catalog state.</summary>
    public void ClearXmlCatalogCache()
    {
        ResolvedResourceCache.ClearXmlResources();
        CatalogResourceSource? source = CatalogSource;
        XmlResolverPolicy? policy = ResolverPolicy;
        if (source != null && policy != null)
        {
            Matcher = new CatalogMatcher(Entries, source, policy.CacheResources);
        }
    }

    internal string CreateXmlCatalogResourceKey(XmlResourceKind kind, string? publicIdentifier, string resolvedUri)
    {
        string publicKey = publicIdentifier == null ? "null" : publicIdentifier.Length + ":" + publicIdentifier;
        return (int)kind + "|" + publicKey + "|" + resolvedUri.Length + ":" + resolvedUri;
    }

    internal XmlResourceException CreateXmlCatalogResolutionError(string message, string code, string? sourceUri)
    {
        XmlResolverPolicy policy = ResolverPolicy == null ? new XmlResolverPolicy() : ResolverPolicy;
        ResolverGuard guard = new ResolverGuard(policy, null);
        return guard.CreateXmlResourceError(message, code, sourceUri);
    }
}
