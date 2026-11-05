namespace http;

/// <summary>
/// Identifies the outcome of a bounded or incremental HTTP operation.
/// </summary>
public enum HttpOperationStatus
{
    /// <summary>
    /// The operation completed successfully.
    /// </summary>
    Done = 0,

    /// <summary>
    /// The operation needs more input before it can continue.
    /// </summary>
    NeedInput = 1,

    /// <summary>
    /// The operation needs more output capacity before it can continue.
    /// </summary>
    NeedOutput = 2,

    /// <summary>
    /// The input or requested operation was rejected.
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// The requested feature or extension is unsupported.
    /// </summary>
    Unsupported = 4,

    /// <summary>
    /// The operation reached a terminal protocol state.
    /// </summary>
    Terminal = 5
}
