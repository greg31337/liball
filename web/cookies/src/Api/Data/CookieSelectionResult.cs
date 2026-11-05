namespace cookies;

/// <summary>
/// Describes ordered cookies and request fields selected for one request.
/// </summary>
public class CookieSelectionResult
{
    /// <summary>
    /// Indicates whether selection completed without a fatal error.
    /// </summary>
    public bool Success;

    /// <summary>
    /// The primary stable outcome code.
    /// </summary>
    public CookieErrorCode ErrorCode;

    /// <summary>
    /// The included stored cookies in request serialization order.
    /// </summary>
    public StoredCookie[] Cookies = [];

    /// <summary>
    /// The included request pairs in serialization order.
    /// </summary>
    public CookiePair[] Pairs = [];

    /// <summary>
    /// The serialized Cookie field values in output order.
    /// </summary>
    public string[] FieldValues = [];

    /// <summary>
    /// The per-candidate match results in evaluation order.
    /// </summary>
    public CookieMatchResult[] Matches = [];

    /// <summary>
    /// The ordered selection and serialization diagnostics.
    /// </summary>
    public CookieIssue[] Issues = [];
}
