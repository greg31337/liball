namespace icc;

/// <summary>
/// Identifies the severity of a profile validation issue.
/// </summary>
public enum IccValidationSeverity
{
    /// <summary>
    /// Identifies informational validation output.
    /// </summary>
    Information = 0,

    /// <summary>
    /// Identifies a warning that does not necessarily invalidate a profile.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Identifies a profile conformance error.
    /// </summary>
    Error = 2,

    /// <summary>
    /// Identifies an error that prevents safe continued processing.
    /// </summary>
    Fatal = 3
}
