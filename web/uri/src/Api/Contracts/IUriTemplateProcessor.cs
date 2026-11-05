namespace uri;

/// <summary>
/// Expands URI Templates into parsed, resolved, or normalized URI references.
/// </summary>
public interface IUriTemplateProcessor
{
    /// <summary>Expands and parses a URI-reference.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="expansionOptions">The expansion options.</param>
    /// <param name="parseOptions">The URI parsing options.</param>
    /// <returns>The parsed expanded URI reference.</returns>
    UriReference ExpandToUri(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions expansionOptions,
        UriParseOptions parseOptions);

    /// <summary>Expands and resolves a URI reference against an absolute base URI.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="baseUri">The absolute base URI without a fragment.</param>
    /// <param name="expansionOptions">The expansion options.</param>
    /// <param name="resolutionOptions">The URI resolution options.</param>
    /// <returns>The resolved absolute URI text.</returns>
    string ExpandAndResolve(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        string baseUri,
        UriTemplateExpansionOptions expansionOptions,
        UriResolutionOptions resolutionOptions);

    /// <summary>Expands and syntax-normalizes a URI reference.</summary>
    /// <param name="template">The reusable template model.</param>
    /// <param name="bindings">The case-sensitive variable bindings.</param>
    /// <param name="expansionOptions">The expansion options.</param>
    /// <param name="normalizationOptions">The URI normalization options and optional profile.</param>
    /// <returns>The normalized URI-reference text.</returns>
    string ExpandAndNormalize(
        UriTemplate template,
        UriTemplateBinding[] bindings,
        UriTemplateExpansionOptions expansionOptions,
        UriNormalizationOptions normalizationOptions);
}
