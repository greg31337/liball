namespace xml;

/// <summary>
/// Resolves XPath document and collection resources under an explicit policy.
/// </summary>
public interface IXPathCollectionResolver
{
    /// <summary>
    /// Resolves one document URI.
    /// </summary>
    /// <param name="uri">The document URI.</param>
    /// <param name="policy">The resource policy.</param>
    /// <returns>The resolved document, or null when it is unavailable.</returns>
    XmlDocument? ResolveXPathDocument(string uri, XmlResolverPolicy policy);

    /// <summary>
    /// Resolves an XDM collection.
    /// </summary>
    /// <param name="uri">The collection URI, or null for the default collection.</param>
    /// <param name="policy">The resource policy.</param>
    /// <returns>The collection sequence.</returns>
    XdmSequence ResolveXPathCollection(string? uri, XmlResolverPolicy policy);

    /// <summary>
    /// Resolves the URI members of a collection.
    /// </summary>
    /// <param name="uri">The collection URI, or null for the default collection.</param>
    /// <param name="policy">The resource policy.</param>
    /// <returns>The collection member URIs.</returns>
    string[] ResolveXPathUriCollection(string? uri, XmlResolverPolicy policy);
}
