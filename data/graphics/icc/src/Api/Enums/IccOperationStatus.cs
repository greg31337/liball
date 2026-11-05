namespace icc;

/// <summary>
/// Describes the outcome of an operation that reports expected failures without throwing.
/// </summary>
public enum IccOperationStatus
{
    /// <summary>
    /// Indicates that no operation outcome has been assigned.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// Indicates that the operation completed successfully.
    /// </summary>
    Success = 1,

    /// <summary>
    /// Indicates that input data was invalid.
    /// </summary>
    InvalidData = 2,

    /// <summary>
    /// Indicates that a configured resource limit was exceeded.
    /// </summary>
    ResourceLimitExceeded = 3,

    /// <summary>
    /// Indicates that the requested feature or representation is unsupported.
    /// </summary>
    UnsupportedFeature = 4,

    /// <summary>
    /// Indicates that the requested profile or item was not found.
    /// </summary>
    NotFound = 5,

    /// <summary>
    /// Indicates that conflicting data prevented the operation.
    /// </summary>
    Conflict = 6,

    /// <summary>
    /// Indicates that the operation failed for another expected reason.
    /// </summary>
    Failed = 7
}
