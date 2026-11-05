namespace uri;

/// <summary>
/// Creates verified URI references from absolute base and target URIs.
/// </summary>
public interface IUriRelativizer
{
    /// <summary>Relativizes an absolute target URI or throws when no verified reference can be created.</summary>
    /// <param name="baseUri">The absolute base URI without a fragment.</param>
    /// <param name="targetUri">The absolute target URI.</param>
    /// <param name="options">The relativization options.</param>
    /// <returns>The selected URI reference text.</returns>
    /// <exception cref="UriResolutionException">Validation, relativization, or a configured limit fails.</exception>
    string RelativizeReference(
        string baseUri,
        string targetUri,
        UriRelativizationOptions options);

    /// <summary>Relativizes an absolute target URI without throwing for expected failure.</summary>
    /// <param name="baseUri">The absolute base URI without a fragment.</param>
    /// <param name="targetUri">The absolute target URI.</param>
    /// <param name="options">The relativization options.</param>
    /// <returns>The relativization outcome.</returns>
    UriTextResult TryRelativizeReference(
        string baseUri,
        string targetUri,
        UriRelativizationOptions options);
}
