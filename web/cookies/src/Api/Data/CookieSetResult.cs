namespace cookies;

/// <summary>
/// Describes one cookie-jar storage mutation outcome.
/// </summary>
public class CookieSetResult
{
    /// <summary>
    /// The storage disposition.
    /// </summary>
    public CookieSetStatus Status;

    /// <summary>
    /// The primary stable reason for the outcome.
    /// </summary>
    public CookieErrorCode ErrorCode;

    /// <summary>
    /// The stored cookie after an accepted or replaced mutation, when available.
    /// </summary>
    public StoredCookie? Cookie;

    /// <summary>
    /// The raw-header parse result when the mutation originated from text.
    /// </summary>
    public SetCookieParseResult? ParseResult;

    /// <summary>
    /// The ordered diagnostics produced by parsing, policy, storage, and eviction.
    /// </summary>
    public CookieIssue[] Issues = [];
}
