namespace cookies;

/// <summary>
/// Describes ordered outcomes for independently processed Set-Cookie fields.
/// </summary>
public class CookieBatchSetResult
{
    /// <summary>
    /// The per-field outcomes in input order.
    /// </summary>
    public CookieSetResult[] Results = [];

    /// <summary>
    /// The number of newly accepted cookies.
    /// </summary>
    public int AcceptedCount;

    /// <summary>
    /// The number of replaced cookies.
    /// </summary>
    public int ReplacedCount;

    /// <summary>
    /// The number of deleted cookie identities.
    /// </summary>
    public int DeletedCount;

    /// <summary>
    /// The number of ignored inputs.
    /// </summary>
    public int IgnoredCount;

    /// <summary>
    /// The number of rejected inputs.
    /// </summary>
    public int RejectedCount;

    /// <summary>
    /// The ordered batch-level diagnostics.
    /// </summary>
    public CookieIssue[] Issues = [];
}
