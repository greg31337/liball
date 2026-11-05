namespace pptx;

/// <summary>
/// Specifies the severity assigned to a package diagnostic.
/// </summary>
public enum PptxDiagnosticSeverity
{
    /// <summary>
    /// Reports context about an operation without making the result fail.
    /// </summary>
    Info = 0,
    /// <summary>
    /// Reports a suspicious or unsupported condition without making the result fail.
    /// </summary>
    Warning = 1,
    /// <summary>
    /// Reports a failure that causes <see cref="PptxResult.Failed"/> to return <c>true</c>.
    /// </summary>
    Error = 2
}

