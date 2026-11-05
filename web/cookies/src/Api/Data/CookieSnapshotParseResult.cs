namespace cookies;

/// <summary>
/// Describes the exception-less result of decoding a lossless jar snapshot.
/// </summary>
public class CookieSnapshotParseResult
{
    /// <summary>
    /// Indicates whether a validated snapshot was produced.
    /// </summary>
    public bool Success;

    /// <summary>
    /// The validated snapshot when <see cref="Success"/> is true.
    /// </summary>
    public CookieJarSnapshot? Snapshot;

    /// <summary>
    /// The primary stable outcome code.
    /// </summary>
    public CookieErrorCode ErrorCode;

    /// <summary>
    /// The ordered schema and entry diagnostics.
    /// </summary>
    public CookieIssue[] Issues = [];
}
