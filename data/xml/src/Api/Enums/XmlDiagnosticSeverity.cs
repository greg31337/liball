namespace xml;

/// <summary>
/// Specifies the severity of an XML diagnostic.
/// </summary>
public enum XmlDiagnosticSeverity
{
    /// <summary>
    /// Reports informational detail.
    /// </summary>
    Information = 0,
    /// <summary>
    /// Reports a recoverable warning.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// Reports an error.
    /// </summary>
    Error = 2,
    /// <summary>
    /// Reports an error that stops processing.
    /// </summary>
    Fatal = 3
}
