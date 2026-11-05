namespace markdown;

/// <summary>
/// Indicates how seriously a Markdown diagnostic affects processing.
/// </summary>
public enum MarkdownDiagnosticSeverity
{
    /// <summary>
    /// Marks a diagnostic as info.
    /// </summary>
    Info = 0,
    /// <summary>
    /// Marks a diagnostic as warning.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// Marks a diagnostic as error.
    /// </summary>
    Error = 2
}
