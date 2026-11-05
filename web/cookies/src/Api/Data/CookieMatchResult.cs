namespace cookies;

/// <summary>
/// Describes whether a stored cookie is eligible for a request.
/// </summary>
public class CookieMatchResult
{
    /// <summary>
    /// The stored cookie evaluated by the match operation, when available.
    /// </summary>
    public StoredCookie? Cookie;

    /// <summary>
    /// The request pair produced for an included cookie, when available.
    /// </summary>
    public CookiePair? Pair;

    /// <summary>
    /// The request-match disposition.
    /// </summary>
    public CookieMatchStatus Status;

    /// <summary>
    /// The primary stable reason for the match disposition.
    /// </summary>
    public CookieErrorCode ErrorCode;

    /// <summary>
    /// The diagnostics produced while evaluating the match.
    /// </summary>
    public CookieIssue[] Issues = [];
}
