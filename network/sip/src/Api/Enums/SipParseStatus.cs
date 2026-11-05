namespace sip;

/// <summary>
/// Identifies the outcome of a bounded parse operation.
/// </summary>
public enum SipParseStatus
{
    /// <summary>
    /// No parse outcome is available.
    /// </summary>
    None = 0,

    /// <summary>
    /// A complete value or message was parsed.
    /// </summary>
    Complete = 1,

    /// <summary>
    /// More input is required to complete parsing.
    /// </summary>
    Incomplete = 2,

    /// <summary>
    /// The input is invalid.
    /// </summary>
    Error = 3
}
