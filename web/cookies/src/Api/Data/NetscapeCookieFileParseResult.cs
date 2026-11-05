namespace cookies;

/// <summary>
/// Describes the exception-less result of parsing Netscape cookie-file text.
/// </summary>
public class NetscapeCookieFileParseResult
{
    /// <summary>
    /// Indicates whether parsing completed without a fatal error.
    /// </summary>
    public bool Success;

    /// <summary>
    /// The parsed cookie entries in file order.
    /// </summary>
    public StoredCookie[] Cookies = [];

    /// <summary>
    /// The primary stable outcome code.
    /// </summary>
    public CookieErrorCode ErrorCode;

    /// <summary>
    /// The ordered malformed-line and lossy-conversion diagnostics.
    /// </summary>
    public CookieIssue[] Issues = [];
}
