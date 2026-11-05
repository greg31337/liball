namespace cookies;

/// <summary>
/// Describes the exception-less result of parsing one or more Cookie request fields.
/// </summary>
public class CookieHeaderParseResult
{
    /// <summary>
    /// Indicates whether parsing completed without a fatal error.
    /// </summary>
    public bool Success;

    /// <summary>
    /// The parsed name and value pairs in field and segment order.
    /// </summary>
    public CookiePair[] Pairs = [];

    /// <summary>
    /// The primary stable outcome code.
    /// </summary>
    public CookieErrorCode ErrorCode;

    /// <summary>
    /// The ordered diagnostics produced while parsing.
    /// </summary>
    public CookieIssue[] Issues = [];
}
