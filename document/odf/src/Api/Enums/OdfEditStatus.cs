namespace odf;

/// <summary>
/// Identifies the outcome of an editing operation.
/// </summary>
public enum OdfEditStatus
{
    /// <summary>
    /// The requested edit was applied.
    /// </summary>
    Success = 0,
    /// <summary>
    /// The request was valid but made no change.
    /// </summary>
    NoChange = 1,
    /// <summary>
    /// An edit precondition failed.
    /// </summary>
    Conflict = 2,
    /// <summary>
    /// The edit was rejected by validation.
    /// </summary>
    ValidationFailed = 3,
    /// <summary>
    /// The edit was cancelled before completion.
    /// </summary>
    Cancelled = 4,
    /// <summary>
    /// The edit failed.
    /// </summary>
    Failed = 5
}
