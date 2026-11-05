namespace docx;

/// <summary>
/// Specifies the error severity used by DOCX operations.
/// </summary>
public enum DocxErrorSeverity
{
    /// <summary>
    /// Describes a repair or observation that does not make the package invalid.
    /// </summary>
    Information = 0,
    /// <summary>
    /// Describes a suspicious condition that may affect compatibility but does not prevent processing.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// Describes an invalid condition that should be corrected before relying on the document.
    /// </summary>
    Error = 2
}
