namespace cookies;

/// <summary>
/// Evaluates cookie acceptance and request-return policy.
/// </summary>
public interface ICookiePolicy
{
    /// <summary>
    /// Evaluates whether a supplied cookie can be accepted.
    /// </summary>
    /// <param name="cookie">The cookie proposed for storage.</param>
    /// <param name="context">The facts governing the cookie-setting operation.</param>
    /// <returns>The acceptance decision and normalized candidate.</returns>
    /// <exception cref="CookiePolicyException">Policy evaluation cannot be completed.</exception>
    CookieSetDecision EvaluateCookieAcceptance(SetCookie cookie, CookieSetContext context);

    /// <summary>
    /// Evaluates whether a stored cookie can be returned for a request.
    /// </summary>
    /// <param name="cookie">The stored cookie being considered.</param>
    /// <param name="context">The facts governing the request.</param>
    /// <returns>The cookie match decision.</returns>
    /// <exception cref="CookiePolicyException">Policy evaluation cannot be completed.</exception>
    CookieMatchResult EvaluateCookieReturn(StoredCookie cookie, CookieRequestContext context);
}
