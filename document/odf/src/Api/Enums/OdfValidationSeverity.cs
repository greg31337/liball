namespace odf;

/// <summary>
/// Identifies the severity of a validation diagnostic.
/// </summary>
public enum OdfValidationSeverity
{
    /// <summary>
    /// Informational output.
    /// </summary>
    Information = 0,
    /// <summary>
    /// A non-fatal compatibility or quality warning.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// A conformance error.
    /// </summary>
    Error = 2,
    /// <summary>
    /// An error that prevents continued processing.
    /// </summary>
    Fatal = 3
}
