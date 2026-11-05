namespace uri;

/// <summary>
/// Resolves URI references against absolute base URIs.
/// </summary>
public interface IUriResolver
{
    /// <summary>Resolves a URI reference or throws when resolution fails.</summary>
    /// <param name="baseUri">The absolute base URI without a fragment.</param>
    /// <param name="reference">The URI reference to resolve.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The resolved absolute URI text.</returns>
    /// <exception cref="UriResolutionException">Validation, resolution, or a configured limit fails.</exception>
    string ResolveReference(
        string baseUri,
        string reference,
        UriResolutionOptions options);

    /// <summary>Resolves a URI reference without throwing for expected failure.</summary>
    /// <param name="baseUri">The absolute base URI without a fragment.</param>
    /// <param name="reference">The URI reference to resolve.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The resolution outcome.</returns>
    UriTextResult TryResolveReference(
        string baseUri,
        string reference,
        UriResolutionOptions options);
}
