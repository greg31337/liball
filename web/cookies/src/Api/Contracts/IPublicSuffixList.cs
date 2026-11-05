namespace cookies;

/// <summary>
/// Provides concurrency-safe public-suffix and registrable-domain lookups.
/// </summary>
public interface IPublicSuffixList
{
    /// <summary>
    /// Returns the public suffix for a canonical ASCII domain.
    /// </summary>
    /// <param name="canonicalDomain">The canonical ASCII domain.</param>
    /// <returns>The canonical public suffix, or null when no rule applies.</returns>
    string? GetPublicSuffix(string canonicalDomain);

    /// <summary>
    /// Returns the registrable domain for a canonical ASCII domain.
    /// </summary>
    /// <param name="canonicalDomain">The canonical ASCII domain.</param>
    /// <returns>The canonical registrable domain, or null when none exists.</returns>
    string? GetRegistrableDomain(string canonicalDomain);

    /// <summary>
    /// Determines whether a canonical ASCII domain is a public suffix.
    /// </summary>
    /// <param name="canonicalDomain">The canonical ASCII domain.</param>
    /// <returns>True when the domain is a public suffix.</returns>
    bool IsPublicSuffix(string canonicalDomain);
}
