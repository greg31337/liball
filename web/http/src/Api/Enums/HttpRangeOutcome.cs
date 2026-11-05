namespace http;

/// <summary>
/// Identifies the result of parsing or evaluating an HTTP range request.
/// </summary>
public enum HttpRangeOutcome
{
    /// <summary>
    /// Range processing is not applicable or was intentionally ignored.
    /// </summary>
    Ignored = 0,

    /// <summary>
    /// At least one requested range is satisfiable.
    /// </summary>
    Satisfiable = 1,

    /// <summary>
    /// None of the requested ranges is satisfiable.
    /// </summary>
    Unsatisfiable = 2,

    /// <summary>
    /// The range syntax is malformed.
    /// </summary>
    Malformed = 3,

    /// <summary>
    /// The requested range unit is unsupported.
    /// </summary>
    UnsupportedUnit = 4
}
