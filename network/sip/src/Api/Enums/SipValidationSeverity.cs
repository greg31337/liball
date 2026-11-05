namespace sip;

/// <summary>
/// Identifies the severity of a SIP validation issue.
/// </summary>
public enum SipValidationSeverity
{
    /// <summary>
    /// The severity has not been assigned.
    /// </summary>
    None = 0,

    /// <summary>
    /// The input is usable but may be non-conformant or risky.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// The input cannot be accepted under the selected validation policy.
    /// </summary>
    Error = 2
}
