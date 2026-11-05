namespace cookies;

/// <summary>
/// Describes a deterministic merge or replacement import operation.
/// </summary>
public class CookieImportResult
{
    /// <summary>
    /// Indicates whether the import completed without a fatal error.
    /// </summary>
    public bool Success;

    /// <summary>
    /// The merge or replacement behavior used for the import.
    /// </summary>
    public CookieImportMode Mode;

    /// <summary>
    /// The primary stable outcome code.
    /// </summary>
    public CookieErrorCode ErrorCode;

    /// <summary>
    /// The per-entry storage outcomes in imported order.
    /// </summary>
    public CookieSetResult[] Results = [];

    /// <summary>
    /// The number of newly accepted entries.
    /// </summary>
    public int AcceptedCount;

    /// <summary>
    /// The number of entries that replaced an existing identity.
    /// </summary>
    public int ReplacedCount;

    /// <summary>
    /// The number of identities deleted by imported expiry state.
    /// </summary>
    public int DeletedCount;

    /// <summary>
    /// The number of ignored entries.
    /// </summary>
    public int IgnoredCount;

    /// <summary>
    /// The number of rejected entries.
    /// </summary>
    public int RejectedCount;

    /// <summary>
    /// The number of preexisting entries removed by replacement import.
    /// </summary>
    public int RemovedCount;

    /// <summary>
    /// The ordered import and loss diagnostics.
    /// </summary>
    public CookieIssue[] Issues = [];
}
