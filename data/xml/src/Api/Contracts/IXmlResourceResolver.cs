namespace xml;

/// <summary>
/// Resolves an external resource under an explicit access policy.
/// </summary>
public interface IXmlResourceResolver
{
    /// <summary>
    /// Resolves one external resource.
    /// </summary>
    /// <param name="kind">The purpose of the resource.</param>
    /// <param name="publicIdentifier">The public identifier, or null when absent.</param>
    /// <param name="systemIdentifier">The system identifier or URI reference.</param>
    /// <param name="baseUri">The base URI used to resolve a relative reference.</param>
    /// <param name="policy">The access policy and resolution limits.</param>
    /// <returns>The resolved resource.</returns>
    XmlResolvedResource ResolveXmlResource(XmlResourceKind kind, string? publicIdentifier, string? systemIdentifier, string? baseUri, XmlResolverPolicy policy);
}
