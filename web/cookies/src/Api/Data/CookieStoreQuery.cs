namespace cookies;

/// <summary>
/// Describes candidate lookup and management filters for stored cookies.
/// </summary>
public class CookieStoreQuery
{
    /// <summary>
    /// Indicates whether an exact cookie identity filter is active.
    /// </summary>
    public bool HasIdentity;

    /// <summary>
    /// The exact identity when <see cref="HasIdentity"/> is true.
    /// </summary>
    public CookieIdentity Identity = new CookieIdentity();

    /// <summary>
    /// Indicates whether a canonical domain filter is active.
    /// </summary>
    public bool HasDomain;

    /// <summary>
    /// The canonical domain when <see cref="HasDomain"/> is true.
    /// </summary>
    public string Domain = "";

    /// <summary>
    /// Includes cookies from subdomains of <see cref="Domain"/>.
    /// </summary>
    public bool IncludeSubdomains;

    /// <summary>
    /// Indicates whether a canonical path filter is active.
    /// </summary>
    public bool HasPath;

    /// <summary>
    /// The canonical path when <see cref="HasPath"/> is true.
    /// </summary>
    public string Path = "";

    /// <summary>
    /// Indicates whether a cookie name filter is active.
    /// </summary>
    public bool HasName;

    /// <summary>
    /// The cookie name when <see cref="HasName"/> is true.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Indicates whether partition state is used as a filter.
    /// </summary>
    public bool FilterByPartition;

    /// <summary>
    /// Indicates whether the partition filter selects a keyed partition.
    /// </summary>
    public bool HasPartitionKey;

    /// <summary>
    /// The canonical partition key when <see cref="HasPartitionKey"/> is true.
    /// </summary>
    public string PartitionKey = "";

    /// <summary>
    /// Selects only session cookies.
    /// </summary>
    public bool SessionOnly;

    /// <summary>
    /// Selects only cookies expired at the evaluation instant.
    /// </summary>
    public bool ExpiredOnly;

    /// <summary>
    /// Indicates whether an expiry evaluation instant was supplied.
    /// </summary>
    public bool HasEvaluationTime;

    /// <summary>
    /// The expiry evaluation instant when <see cref="HasEvaluationTime"/> is true.
    /// </summary>
    public global::System.DateTimeOffset EvaluationTime;
}
