namespace http;

/// <summary>
/// Identifies how an HTTP/2 connection Origin Set affects an origin.
/// </summary>
public enum Http2OriginOutcome
{
    /// <summary>
    /// No ORIGIN frame has initialized the set, so the caller applies the usual authority rules.
    /// </summary>
    DefaultAuthorityRules = 0,

    /// <summary>
    /// The initialized Origin Set permits the caller to consider the origin.
    /// </summary>
    OriginSetAllows = 1,

    /// <summary>
    /// The initialized Origin Set forbids treating the connection as authoritative for the origin.
    /// </summary>
    OriginSetRejects = 2,

    /// <summary>
    /// Origin Set advice is unavailable for this operation.
    /// </summary>
    NotApplicable = 3
}
