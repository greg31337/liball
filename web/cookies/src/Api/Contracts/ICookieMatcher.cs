namespace cookies;

/// <summary>
/// Provides canonical host, domain, path, site, expiry, and request matching operations.
/// </summary>
public interface ICookieMatcher
{
    /// <summary>
    /// Extracts and canonicalizes the host of an absolute origin or request URI.
    /// </summary>
    /// <param name="uri">The absolute URI.</param>
    /// <returns>The canonical ASCII host.</returns>
    string CanonicalizeHost(global::System.Uri uri);

    /// <summary>
    /// Determines whether a canonical request host domain-matches a canonical cookie domain.
    /// </summary>
    /// <param name="requestHost">The canonical request host.</param>
    /// <param name="cookieDomain">The canonical cookie domain.</param>
    /// <returns>True when the domain-match rules are satisfied.</returns>
    bool DomainMatches(string requestHost, string cookieDomain);

    /// <summary>
    /// Computes the RFC default cookie path from an origin URI.
    /// </summary>
    /// <param name="origin">The absolute origin URI.</param>
    /// <returns>The canonical default path.</returns>
    string ComputeDefaultPath(global::System.Uri origin);

    /// <summary>
    /// Determines whether a request path path-matches a cookie path.
    /// </summary>
    /// <param name="requestPath">The escaped request path.</param>
    /// <param name="cookiePath">The canonical cookie path.</param>
    /// <returns>True when the path-match rules are satisfied.</returns>
    bool PathMatches(string requestPath, string cookiePath);

    /// <summary>
    /// Computes the schemeful site for an absolute URI.
    /// </summary>
    /// <param name="uri">The absolute URI.</param>
    /// <returns>The canonical schemeful site.</returns>
    string ComputeSchemefulSite(global::System.Uri uri);

    /// <summary>
    /// Evaluates whether a stored cookie is eligible for a request.
    /// </summary>
    /// <param name="cookie">The stored cookie.</param>
    /// <param name="context">The request context.</param>
    /// <returns>The include or exclude result.</returns>
    CookieMatchResult EvaluateCookieMatch(StoredCookie cookie, CookieRequestContext context);

    /// <summary>
    /// Determines whether a stored cookie is expired at a supplied instant.
    /// </summary>
    /// <param name="cookie">The stored cookie.</param>
    /// <param name="evaluationTime">The explicit UTC evaluation instant.</param>
    /// <returns>True when the cookie is expired.</returns>
    bool IsCookieExpired(StoredCookie cookie, global::System.DateTimeOffset evaluationTime);
}
