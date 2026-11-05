namespace http;

/// <summary>
/// Identifies the result of evaluating HTTP request preconditions.
/// </summary>
public enum HttpPreconditionOutcome
{
    /// <summary>
    /// Request processing can proceed.
    /// </summary>
    Proceed = 0,

    /// <summary>
    /// The recipient should respond with a not-modified response.
    /// </summary>
    NotModified = 1,

    /// <summary>
    /// A request precondition failed.
    /// </summary>
    PreconditionFailed = 2,

    /// <summary>
    /// The Range field should be ignored while processing continues.
    /// </summary>
    IgnoreRange = 3,

    /// <summary>
    /// The preconditions could not be evaluated safely.
    /// </summary>
    Rejected = 4
}
