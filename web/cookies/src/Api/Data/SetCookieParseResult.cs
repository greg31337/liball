namespace cookies;

/// <summary>
/// Describes the exception-less result of parsing one Set-Cookie field value.
/// </summary>
public class SetCookieParseResult
{
    /// <summary>
    /// Indicates whether a parsed cookie was produced.
    /// </summary>
    public bool Success;

    /// <summary>
    /// The parsed cookie when <see cref="Success"/> is true.
    /// </summary>
    public SetCookie? Cookie;

    /// <summary>
    /// The primary stable outcome code.
    /// </summary>
    public CookieErrorCode ErrorCode;

    /// <summary>
    /// The ordered diagnostics produced while parsing.
    /// </summary>
    public CookieIssue[] Issues = [];
}
